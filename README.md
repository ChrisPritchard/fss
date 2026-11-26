# fss — an F# subset compiled to C

A small compiler that lowers an F#-like source language to C, then uses the
system C compiler to produce a native executable (libraries come later).

![](./bench.webp)

Read `DESIGN.md` for the full design: what is kept, converted, reimplemented,
and dropped, plus the value-representation and memory-model decisions.

> **Work in progress.** This is an early, incomplete implementation (v0.2).
> The language surface, runtime, and self-hosting bootstrap are still
> evolving, so expect rough edges and breaking changes.

## Status

Implemented so far (v0.2):

- lexer, parser, type checker (Hindley-Milner-style unification), C codegen, gcc driver
- `int` (32-bit), `bool`, `unit`, function types `t -> u`
- arithmetic `+ - * / %`, comparison `= <> < <= > >=`, logic `&& || not`, negation `-`
- `if / then / else` (statement and expression), `if / then` (unit if), `while / do`
- `let ... in ...`, `let mutable ... in ...`, `<-`
- functions with a `unit` parameter: `let f () = ...`, `fun () -> ...`
- top-level functions as values (`let g = id`, `twice f`), eta-expanded
- functions: `let f x y = ...`, `let rec f x = ...`, lambdas `fun x -> ...`, application `f x`
- closures (capture by value; mutable captures by reference), partial application
- direct-call optimization for fully applied top-level functions; tail-position
  `return`s so gcc performs sibling-call optimization on self-recursion
- records (`type R = { X: int; Y: int }`, `r.X`, `{ r with X = 1 }`)
- discriminated unions (`type T = A of int | B of int * int | C`), constructors, matching
- options (`None`, `Some x`), lists (`[]`, `x :: xs`, `[1; 2]`), tuples `(1, 2)`
- `match ... with | pat -> ... | ...` with wildcard/variable/literal/tuple/union/list patterns
- structural equality (`=`, `<>`) for all data types (including strings)
- UTF-8 strings: literals, `+` concatenation, `=`, `str_len`, `print_string`
- formatting helpers: `int_to_string`, `bool_to_string` (build strings with `+`)
- I/O: `read_line ()` (stdin), `read_file path`, `write_file path content`
- exceptions: `failwith "msg"`, `try e with | pat -> ...` (`_`, variable, or string literal)
- `let main argv = ...` gives `argv : string list` (also `let main () = ...`)
- `;` sequencing, scope-correct shadowing
- **significant whitespace**: newline + indentation is accepted as an
  alternative to `;`/`in`/`( )`, so F#-style layout just works
- F# attributes (`[<EntryPoint>]`, etc.) are read and ignored
- `open System` and `.NET`-style names (`System.Console.WriteLine`,
  `System.String`, `Console.WriteLine`, ...) are aliased to fss builtins/types
- `fss scan file.fs` reports F# features fss doesn't support (instead of a
  hard compile failure)
- builtins: `print_int`, `print_bool`, `print_newline`, `print_string`, `str_len`,
  `int_to_string`, `bool_to_string`, `read_line`, `read_file`, `write_file`,
  `print_double`, `double_to_string`, `int_to_double`, `double_to_int`,
  `cstring_of_string`, `string_of_cstring`, `cstring_null`,
  `cstring_is_null`, `cstring_len`
- **C interop**: `double`, `cstring` (`char*`), `extern` declarations,
  `#include "h"` / `#link "lib"` (or `[<Include("h")>]` / `[<Link("lib")>]`),
  so any C library is callable; `[<Export>]` + `--lib` builds a `.so`/`.dll`
  with a generated header, so C can call fss

Not yet: arrays, type annotations, local `let rec` and `and`, full `sprintf`,
`try/finally`, `raise`. These are the next milestones in `DESIGN.md`.

Top-level mutual recursion is supported: `let rec even n = … and odd n = …`.

### v0.2 limitations

- Top-level bindings are polymorphic (Hindley-Milner let-polymorphism) and
  monomorphized per use. Local `let` bindings stay monomorphic — hoist them to
  top level if you need polymorphism.
- Local `let rec` is rejected; define recursive functions at top level.
- Top-level functions work as values (`let g = id`, `twice greet`): they are
  eta-expanded into closures. Partially applied forms work too (`add 10`).
- `let f () = ...` and `fun () -> ...` take a real `unit` parameter.
- User-defined record/union types are **heap-allocated** (pointer-boxed), so
  recursive types like `type Tree = Leaf of int | Node of Tree * Tree` work.
- Strings are UTF-8 with a length prefix; `str_len` returns the byte length.
- Heap memory uses a bump allocator (8 MB chunks) with 64 size-class free
  lists, backed by a conservative mark-sweep collector (stack + registered
  global roots) triggered at a 64 MB threshold.
- C interop covers `int`, `bool`, `double`, `cstring` (`char*`) and `unit`.
  Arbitrary struct pointers, callbacks (C function pointers) and arrays are
  not exposed yet.

## Build the compiler

```sh
dotnet build src/Fss.fsproj -c Release
```

### Native (AOT) compiler — recommended

A one-time publish turns the compiler itself into a native binary, removing
.NET startup from every compilation (minimal compile time):

```sh
bash scripts/build-native.sh     # produces build/aot/fss.exe
./build/aot/fss.exe build prog.fs -o prog
```

The managed build (`dotnet run ...` or `dotnet src/bin/Release/net9.0/fss.dll`)
still works and is what the bootstrap uses.

## Use

```sh
fss build prog.fs -o prog          # compiles to prog.exe (Windows) or prog
fss build a.fs b.fs -o app         # multiple files are merged in order
fss scan prog.fs                   # list unsupported F# features
./prog
```

Flags:

- `-o <output>` — output base name
- `--keep-c` — keep the generated `prog.c`
- `-O0` / `-O1` / `-O2` / `-O3` / `-Os` / `-Ofast` — C optimization (default `-O2`)
- `--cc <name>` — C compiler (default `cc`)
- `--march-native` — add `-march=native` for maximum local performance

`-fwrapv` is always passed: F# `int` arithmetic is unchecked and wraps, while C
signed overflow would otherwise be undefined behaviour.

## Syntax notes (small deviations from F#, for now)

Both the verbose form (`;`, `in`, `( ... )`) and F#-style layout work:

```fsharp
// verbose
print_int 1; print_newline ()
let x = 1 in print_int x

// significant whitespace
print_int 1
print_newline ()
let x = 1
print_int x
```

- A `while`/`if`/`let`/function body is a *single statement*; in the verbose
  form use `( ... )` for several, or indent a block in the layout form
- Functions: `let add x y = x + y`, `let rec fact n = if n = 0 then 1 else n * fact (n - 1)`
- Lambdas: `fun x -> x + 1`; application: `f x y`

## C interop

fss emits C, so calling into C is direct: no P/Invoke, no marshalling.
Declare foreign functions with `extern`, and pull in headers/libraries with
`#include`/`#link` (or the F#-compatible attribute spellings
`[<Include("...")>]`/`[<Link("...")>]`):

```fsharp
#include "math.h"
#link "m"

extern sqrt(x: double) : double
extern pow(x: double, y: double) : double
extern abs(x: int) : int

let main () =
    println (sqrt 144.0)          // 12
    println (pow 2.0 10.0)        // 1024
    println (abs (-42))           // 42
    0
```

Each `extern` compiles to a static wrapper (`static double fs_ext_sqrt(double)
{ return sqrt(a0); }`) plus `-lm` at link time, so the call is a plain C
function call. fss deliberately does *not* re-declare the foreign prototype:
the included header already declares it, and a duplicate declaration with
slightly different types (say `char*` vs `const char*` for `strcmp`) would be
a conflicting-types error. The wrapper lets C apply the usual implicit
conversions.

Interop types: `int` → `int32_t`, `bool` → `bool`, `double` → `double`,
`cstring` → `char*`, `string` → `fsstring_t` (UTF-8, length-prefixed), `unit` →
`void` for a return type.

`cstring` is fss's raw C-string type:

```fsharp
#include <string.h>
extern strlen(s: cstring) : int
extern strchr(s: cstring, c: int) : cstring

let s = cstring_of_string "hello"   // NUL-terminated copy (GC-managed)
println (strlen s)                  // 5
println (string_of_cstring s)       // hello
println (s = cstring_of_string "hello")   // true (strcmp, not pointer equality)
cstring_null ()                     // NULL
cstring_is_null p                   // p == NULL
cstring_len p                       // strlen p
```

Convert with `cstring_of_string` / `string_of_cstring`; `cstring` values
compare by content.

### Calling fss from C

Mark top-level functions `[<Export>]` (optionally `[<Export("c_name")>]`) and
build a shared library — no `main` needed:

```fsharp
[<Export>]
let add x y = x + y

[<Export("greet")>]
let greeting (name: cstring) =
    cstring_of_string ("hello, " + string_of_cstring name + "!")
```

```sh
fss build mylib.fs -o mylib --lib     # -> mylib.so (Linux) / mylib.dll (Windows)
```

That also writes `mylib.h`:

```c
void fss_init(void);
int32_t add(int32_t, int32_t);
char* greet(char*);
```

Link and call it like any C library. An initializer runs automatically
(a `__attribute__((constructor))` on GCC/Clang); on other toolchains call
`fss_init()` yourself first:

```c
#include "mylib.h"
int main(void) {
    printf("%d
", add(2, 3));
    printf("%s
", greet("world"));
    return 0;
}
```

Only C-compatible types can cross an `[<Export>]` boundary: `int`, `bool`,
`double`, `cstring`, `unit`. Exposing `string`/lists/records is a compile
error with a clear message (use `cstring` instead).

### C callbacks (passing fss functions to C)

An `extern` parameter with a function type is a C function pointer, and a
top-level fss function can be passed directly:

```fsharp
#include "callbacks.h"
#link "callbacks.c"          // a .c/.o/.a path is passed straight to the compiler

extern apply(f: (int -> int), x: int) : int
extern each(f: (int -> unit), n: int) : unit

let doubleIt x = x * 2
let show i = print_int i

let main () =
    println (apply doubleIt 21)   // 42
    each show 4                   // 0123
    0
```

Curried fss functions flatten to C signatures (`int -> int -> int` becomes
`int32_t (*)(int32_t, int32_t)`). Only *named* top-level functions can be
passed as callbacks — a capturing lambda cannot be turned into a bare C
function pointer, and that is a compile error with an explanation.

`double` is a first-class type in fss (`1.5`, `sqrt`, `/`), with
`int_to_double`, `double_to_int`, `double_to_string`, and `print_double` /
`println` support. `%` is rejected on `double` (use `fmod` via `extern`).

This is the one place fss diverges from drop-in F# compatibility: `extern`,
`#include`, and `#link` are fss-only syntax, so a file using them is not
F#-compilable. Keep interop in its own module if you want the rest of the
program to stay dual-compilable.

## Test

```sh
bash tests/run.sh
```

Runs positive cases under `tests/cases/` (compile, run, diff expected output)
and negative cases under `tests/errors/` (must fail with a type error), plus
bootstrap and self-hosting round-trip checks (300+ cases total).

## Benchmark vs F#

`bench/` contains the same small programs written in both fss and F#, with a
script that times compilation and execution of each.

```sh
bash bench/run.sh
```

See `bench/README.md` for the current numbers and the takeaways (compile time,
run time, and where allocation-heavy code behaves differently).

### Why fss over F# NativeAOT?

From those benchmarks, fss's advantages relative to F# NativeAOT are:

- **Compile time** — fss compiles a file in ~280 ms (lex + type-check +
  monomorphize + C codegen + `gcc -O2`). F# NativeAOT is a whole-program
  `dotnet publish` taking seconds (~2-6 s for these programs), so fss is
  ~10-20× faster to iterate on during development.
- **Run speed on compute-bound code** — fss native code is often faster than
  F# NativeAOT for loops, integer arithmetic, and recursion (fib ~2.8×, a
  100M-iteration loop ~7×, prime counting ~2× in `bench/`), because gcc
  optimizes the emitted C directly (constant folding, strength reduction,
  tail-call elimination).
- **No NativeAOT restrictions** — F# NativeAOT forbids reflection and several
  `printf`/format paths (the benchmark had to switch to
  `System.Console.WriteLine`). fss has no such constraints: everything lowers
  to plain C.
- **Transparent, tunable C** — `--keep-c` shows the generated C, and `-O3`,
  `--march-native`, and `--cc <compiler>` are one flag away; F# AOT's codegen
  is opaque by comparison.
- **Direct C interop** — fss *is* C output, so `#include`/`#link` + `extern`
  give zero-cost calls into libc, libm, OpenGL, zlib, or any C library with no
  P/Invoke boundary or marshalling layer (see [C interop](#c-interop)). It also
  means fss inherits whatever targets the C compiler supports (wasm,
  microcontrollers, static musl builds) by swapping `--cc`.

Allocation-heavy code was the historical weak spot; a bump allocator with
size-class free lists (added in v0.2) brought `sort_list` from ~170 ms to
~50 ms (now faster than F# JIT and F# AOT) and `str_concat` to ~90 ms (on par
with F# JIT). The remaining cost for strings is the inherent O(n²) copying of
immutable concatenation, which .NET pays too.

## Self-hosting goal

The .NET compiler is a bootstrap seed. The end goal is to rewrite the compiler
in the fss subset and compile it with itself, removing .NET from the toolchain
entirely. The compiler core is kept free of reflection and `%A`/`%d` printf for
that reason.

Bootstrap stages done, all written in fss, compiled by fss, and exercised by
the test suite:

- `bootstrap/lexer.fs` — tokenizer
- `bootstrap/parser.fs` — precedence-climbing parser (recursive `Expr` AST)
- `bootstrap/typer.fs` — unification-based type checker
- `bootstrap/compiler.fs` — emits a complete C program from a typed AST,
  with top-level `let`/`let rec` (incl. `and` groups), Hindley-Milner
  let-polymorphism + monomorphization, functions/closures, options, mutable
  globals + assignment, n-ary tuples, lists + pattern matching, union/record
  types, structural equality for `=`/`<>` on all data types, and the builtin
  runtime (string ops, I/O, `failwith`) (fss → C → gcc → binary). It now
  compiles `bootstrap/lexer.fs`, `bootstrap/parser.fs`, and
  `bootstrap/typer.fs` to native binaries whose output matches the .NET-built
  versions byte-for-byte, and it compiles **itself**
  (`bootstrap/compiler.fs`) to a working native binary (self-hosting
  milestone).

To support them, `elif`, `str_get`, `str_sub`, `int_of_string`, recursive
(pointer-boxed) user types, and top-level `let mutable` were added. The
self-hosted compiler also handles F# attributes (`[<EntryPoint>]`, ignored),
`open System` + dotted `.NET` names (`Console.WriteLine`, `System.String`,
`System.IO.File.ReadAllText`, …), type annotations (`(name: T)`, ignored), and
`let main () = …`. Significant whitespace in the self-hosted compiler is the
remaining gap (it still wants `in` for local lets).

Known limitation: the .NET NativeAOT compiler binary hits a NativeAOT edge case
on a few code shapes (recursive function + option + `if`), so it may not compile
`bootstrap/parser.fs`; the managed compiler (`dotnet fss.dll`) handles it, and
the test suite uses the managed compiler. The AOT binary is a bootstrap-only
optimization and will be retired once self-hosting is complete.

## Acknowledgements

Developed with [Pi](https://pi.dev), an extensible open-source coding-agent
harness, running on the [DeepSeek Platform](https://platform.deepseek.com).

## License

MIT — see `LICENSE`.
