# MiniF# — an F#-subset compiler that lowers to C

Goal: a small compiler (living in this directory) that takes an F#-like source
language, type-checks it, emits C, then invokes the system C compiler to produce
either an executable or a static/shared library. No .NET runtime, no GC, nothing
to install on the target machine.

```
mysrc.fs  ->  [parser]  ->  AST  ->  [type checker / inference]  ->  typed AST
          ->  [lowering]  ->  C source  ->  [gcc/cc]  ->  ./program | libfoo.a | libfoo.so
```

Toolchain available here: `dotnet` 9 (for the compiler itself), `gcc`/`cc` 15 (backend).

---

## 1. What we take from C# Zero

C# Zero is a bflat fork using NativeAOT to go **C# -> native machine code**
directly. Its lesson is the *split*, not the mechanism:

| C# Zero keeps | C# Zero drops |
|---|---|
| C# 15 syntax, type checking, generics | Garbage collector |
| Real (specialized) generics | .NET runtime |
| Interfaces, virtual methods, records, lambdas | Catching exceptions (`throw` ends the program) |
| Threads (`Parallel.For`) | Reflection, async |

It replaces the runtime with: **arena allocation + a small library** (strings,
collections, I/O) that works on those terms.

Our project makes the same split for F#, but adds an explicit C emission stage
(instead of going straight to machine code). Everything that C# Zero says about
memory and runtime holds for us; the difference is we also get to *choose* our
memory model because we emit C rather than relying on NativeAOT.

---

## 2. The feature ledger

Four buckets, used throughout the rest of the document:

- **Keep** — same syntax and semantics; the compiler lowers it mechanically. Nothing to reimplement.
- **Convert** — same semantics, but the compiler must do real transformation work to express it in C.
- **Reimplement** — a new C library/runtime must be written because .NET's is gone.
- **Drop** — out of scope (v1) or out of scope forever.

### 2.1 Core values and bindings

| Feature | Bucket | Notes |
|---|---|---|
| `let x = ...` | Keep | C local variable / constant |
| `let mutable` / `<-` | Keep | C variable assignment |
| `let rec`, `and` (mutual recursion) | Keep/Convert | Forward declarations; tail-call handling (see §4) |
| Nested functions | Convert | Closure or lambda-lift |
| `fun x -> ...` lambdas | Convert | Closure record + function pointer |
| Partial application / currying | Convert | Closure thunk or ABI choice (§4) |
| Type annotations | Keep | Used by inference |
| `unit`, `int`, `bool`, `float`, `char`, `int64`, `byte` | Keep | Map to C types |

### 2.2 Data types

| Feature | Bucket | Notes |
|---|---|---|
| Tuples `int * string` | Keep | C struct or heap tuple |
| Records `type R = { A: int; B: string }` | Convert | Struct + generated structural equality/hash |
| Anonymous records `{| X = 1 |}` | Convert | Like records, unnamed type |
| Discriminated unions `type T = A \| B of int` | Convert | Tagged struct (tag + payload union) |
| Options `'a option` | Reimplement | Built-in DU, in the C library |
| Lists `'a list` | Reimplement | Persistent linked list in C library |
| Arrays `'a []`, `[,]`, `[,,]` | Reimplement | Heap buffers; bounds checks |
| Sequences `seq<'a>` | Reimplement/Drop | Defer; `[ for ... ]`/`[| ... |]` comprehensions can skip `seq` |
| Map / Set | Reimplement/Drop | Defer; simple binary tree or hash map later |
| Strings | Reimplement | §5 (representation decision) |
| Function types `int -> int` | Convert | §4 calling convention |
| Generics `'a` | Convert | Monomorphize (specialize) or box (§4) |
| Struct records/unions | Convert/Drop | Defer v1; `[<Struct>]` needs value passing care |
| Units of measure | Keep | Compile-time only, erased; zero runtime cost |
| Classes, interfaces, inheritance, object expressions | Drop | The OO half of F#; defer or never |
| Type abbreviations | Keep | Purely syntactic |

### 2.3 Pattern matching

| Feature | Bucket | Notes |
|---|---|---|
| `match` / `function` | Convert | Lower to switch on tag + field projections |
| Literal, variable, wildcard, `as`, `or`, `and` patterns | Convert | Standard decision tree |
| Tuple/record/DU patterns | Convert | Projections |
| List patterns `[]` / `x::xs` | Convert | Head/tail checks |
| Guards `when` | Keep | `if` inside arm |
| Exhaustiveness checking | Keep | In the type checker, same as F# |
| Active patterns | Drop | Defer |

### 2.4 Control flow and effects

| Feature | Bucket | Notes |
|---|---|---|
| `if/then/else` | Keep | C `if` |
| `for` / `while` loops | Keep | C loops |
| Sequencing / `;` | Keep | C statement sequence |
| `use` / `try/finally` | Convert/Drop | `use` needs cleanup discipline; defer try/finally |
| `try/with`, `raise`, `failwith` | Drop/Convert | v1: `failwith` aborts the process (C# Zero style); return `Result` instead. Later: `setjmp`/`longjmp` or error-return codegen |
| `printf` / `sprintf` | Reimplement | Formatted I/O in the C library |
| Async / task computation expressions | Drop | Needs a scheduler/runtime; large. Defer |
| General computation expressions | Drop | Except simple `seq`/list comprehension sugar if cheap |
| Mutually recursive modules / `module rec` | Keep/Convert | Name resolution + forward decls |

### 2.5 Modules and compilation model

| Feature | Bucket | Notes |
|---|---|---|
| `module` / `namespace` / `open` | Keep | File -> C translation unit; names -> mangled C identifiers |
| `[<EntryPoint>] let main argv` | Convert | Emits `int main(int argc, char** argv)` |
| Multiple files | Keep | Each becomes a `.c`; link them |
| `.NET BCL` / NuGet packages | Drop | Replaced by our C library |
| P/Invoke | Drop | Not needed — everything is already native; C FFI is natural |
| Attributes (except `EntryPoint`) | Drop | Compile-time only where useful |
| Reflection / `typeof` / quotations / type providers | Drop | No runtime metadata |

---

## 3. What the C runtime library must provide

A `runtime/` directory of C that ships with every compiled program (or is linked
statically). Minimal v1 surface:

```
runtime/
  fsval.h      value representation, tags, allocation
  memory.c     allocator (arena / refcount / GC — see §6.3)
  string.c     utf8 strings, concat, split, substring, compare, format
  list.c       persistent cons list: map, filter, fold, append, rev
  array.c      arrays + bounds-checked get/set, map, fold
  option.c     (mostly a DU; helpers: map, bind, defaultArg)
  result.c     Ok/Error DU
  io.c         print, read line, read file, write file (subset)
  math.c       abs, min/max, pow, sqrt, trig (call libc)
  random.c     PRNG
```

Everything F# programmers reach for first — `List.map`, `Array.map`,
`Option.map`, `String.concat`, `printf` — lives here.

---

## 4. Compiler transformations (the "Convert" work)

### 4.1 Type inference
We do **not** inherit .NET's compiler. We reimplement Hindley–Milner (with
let-polymorphism) plus the extras F# uses: records, DUs, tuples, and the usual
operator overloading baked in. This is well-understood and is the same algorithm
F# uses — conceptually "kept", practically written by us.

### 4.2 Value representation (biggest single decision)

The problem: F# values have many types (`int`, `float`, tuples, records, DUs,
functions, strings, lists…), but C demands a concrete type for every variable
and parameter *at compile time*. We must decide:

> What is the C type of "an F# value"?

There are two fundamentally different answers.

#### Option A — Uniform boxed `fsval_t`

Invent **one** C type that can hold any F# value and use it everywhere:

```c
typedef enum {
  TAG_INT, TAG_FLOAT, TAG_BOOL, TAG_CHAR,
  TAG_STRING, TAG_OBJ, TAG_UNIT
} tag_t;

typedef struct {
  tag_t tag;
  union {
    int32_t i;      // F# `int` is 32-bit (System.Int32)
    double  f;      // F# `float` is a double
    bool    b;
    void*   p;      // pointer to a heap object
  } u;
} fsval_t;
```

Rules:
- **Scalars** (int, float, bool, char) are stored *inline*, distinguished by
  `tag`.
- **Compound values** (records, DUs, tuples, lists, strings, closures) live on
  the heap and `u.p` points at them.

Every F# function then has the same C shape:

```fsharp
let add (x: int) (y: int) = x + y
```
```c
fsval_t add(fsval_t x, fsval_t y) {
    // caller guarantees x.tag == TAG_INT && y.tag == TAG_INT
    fsval_t r = { .tag = TAG_INT, .u.i = x.u.i + y.u.i };
    return r;
}
```

A DU value is a heap object with a type/case tag in its header:

```c
typedef struct {
    obj_header_t hdr;    // type id + case index
    union {
        struct { double r; } circle;
        struct { double w, h; } rect;
    } data;
} shape_t;
// Circle 2.0 is fsval_t { .tag = TAG_OBJ, .u.p = &shape }
```

Pros: generics become free (`let id x = x` is one C function — *type erasure*,
the Java approach); codegen is trivial; pattern matching is just a switch on
the tag. Perfect for v0.

Cons: boxing everywhere; heap allocation for all compound values; the C
compiler can't optimize through the tagged union. On Windows x64 a 16-byte
struct is passed by pointer (extra indirection); SysV x64 passes it in two
registers.

#### Option B — Specialized / monomorphized

Translate each F# type to its **natural C type**:

| F# | C |
|---|---|
| `int` | `int32_t` |
| `int64` | `int64_t` |
| `float` | `double` |
| `bool` | `bool` |
| `string` | `fsstring_t*` |
| `int * string` | `struct { int32_t a; fsstring_t* b; }` |
| `Shape` (DU) | `struct { uint8_t tag; union { … } u; }` |

```fsharp
let add (x: int) (y: int) = x + y
```
```c
int32_t add(int32_t x, int32_t y) { return x + y; }
```

Zero overhead; gcc optimizes it like ordinary C. The cost is generics:
`let id x = x` has no single C type, so we emit one copy per instantiation
(Rust/C++ style):

```c
int32_t     id_int32(int32_t x)      { return x; }
fsstring_t* id_string(fsstring_t* x) { return x; }
```

This is what C# Zero means by **"real generics"** — NativeAOT specializes per
type instead of boxing into `object`. Best performance, most codegen work
(instantiation discovery, name mangling, code bloat). Target for v1+.

#### Option C — Hybrid

Specialize monomorphic functions to native C types; for genuinely polymorphic
code either specialize (Rust-style) or box just those values into `fsval_t`
(Java-style). This is where we should end up.

#### Middle grounds (uniform but fast)

Real language runtimes compress the uniform representation to 8 bytes:

- **NaN-boxing** (JS engines, some Schemes): pack ints/floats/pointers into a
  64-bit `double`'s NaN payload space. Every value is exactly 8 bytes — uniform
  *and* register-friendly, but fiddly to keep portable C.
- **Tagged pointers**: put the type tag in the low bits of an aligned heap
  pointer; scalars are immediates. Same 8-byte goal.

These can compress `fsval_t` from 16 to 8 bytes later if we start with A.

#### Decision

**v0 = Option A everywhere** (correct first, trivial codegen), then add
specialization as an optimization pass for monomorphic functions and hot loops.
The one C type in v0 is `fsval_t`; do not bake in 16-byte assumptions so we can
switch to NaN-boxing/tagged-pointers later.

### 4.3 Calling convention / currying

F# functions are curried. Two ways to lower:

- **Flat ABI + partial-application closures** — `let add x y = ...` becomes
  `fsval_t add(fsval_t x, fsval_t y)`. A partially applied `add 1` creates a
  closure `{ fn = add_1_thunk, env = {x=1} }`. Fast for full applications
  (the common case), a bit of work for partials.

- **Single-argument curried ABI** — every function takes one `fsval_t` and
  returns either a value or another closure. Uniform, matches currying exactly,
  slower for multi-arg calls.

Recommendation: flat ABI for known-arity calls, synthesize a closure when the
compiler can't prove full application.

### 4.4 Closures

`fun y -> x + y` (capturing `x`) lowers to:

```c
typedef struct { fsval_t x; } env_12_t;
fsval_t lambda_12(env_12_t* env, fsval_t y) { return int_add(env->x, y); }
```

Captured variables that are mutated (`let mutable`) get promoted to a heap cell.

### 4.5 Tail calls

F# idiomatically relies on tail recursion. C gives no guarantee.

- **Self tail calls** (`let rec f x = ... f (x-1)`) → rewrite as a `while` loop.
  Cheap, huge win, do this first.
- **Mutual tail calls** → harder. Options: trampoline mode, or rely on
  `gcc -O2` plus `__attribute__((musttail))` on clang. Defer; document.

### 4.6 Generics

- v0 with uniform `fsval_t`: generics are *type-erased* (everything is a value),
  no specialization needed — simple but no unboxed primitives.
- v1+: monomorphize like Rust. Each closed instantiation gets its own C function
  and its own struct definitions. Requires whole-program or per-module analysis
  to find instantiations.

### 4.7 Structural equality, comparison, hashing

F# `=` on records/DUs/lists is structural and deep. The compiler generates per
type:

```c
bool eq_R(R* a, R* b);        // ==
int  cmp_R(R* a, R* b);       // compare, for ordering
uint32_t hash_R(R* a);        // hash, for Map/Set later
```

Lists/arrays are handled by the runtime library, which calls back through these
generated functions where needed.

### 4.8 Pattern-matching codegen

```fsharp
match x with
| Circle r -> ...
| Rect(w, h) -> ...
```
becomes a `switch (x->tag)` on the DU's case index, with each arm binding the
payload fields to C locals.

---

## 5. Strings

.NET strings are immutable UTF-16; C strings are NUL-terminated UTF-8. Decision
needed, because `s.[i]` semantics differ.

- **UTF-8 + length prefix** (recommended): `typedef struct { int64_t len; char data[]; } fsstring_t;`
  Natural C interop, smaller, but `s.[i]` indexes UTF-8 *bytes* unless we
  implement a codepoint indexer (doable, `O(n)`).
- **UTF-16 to match .NET**: more faithful to F# semantics, annoying in C.

Recommendation: UTF-8 with a length; provide `String.length` (codepoints),
`String.get` (codepoints, `O(n)`), and document the divergence from .NET's
UTF-16 code-unit indexing. For a self-contained compiler this is an acceptable,
well-documented simplification.

---

## 6. Memory management (the C# Zero fork in the road)

C# Zero answers this with **arenas**: allocation is bump-pointer, freeing is
all-at-once per lifetime, no GC. That works for C# Zero's style (`Span`,
`stackalloc`, keep-only-what-must-last).

F# is different: idiomatic functional code allocates **constantly** — every
`List.map`, every record update, every closure. An arena-only model would leak
without bound on ordinary F#. So the realistic choices are:

| Model | Effort | Fit for F# |
|---|---|---|
| Arena only (C# Zero) | Trivial | Poor — persistent data structures leak |
| Reference counting (`Rc`-style) | Moderate | Good for immutable data; cycles rare but possible via closures/mutable refs |
| Small precise mark-sweep GC | Higher (~300–500 lines) | Best fit; F# is GC-shaped |
| Boehm conservative GC (libgc) | Trivial to integrate | Pragmatic stopgap, not "small" in spirit |

Recommendation: **v0 = arena + reference counting** (arena for compiler
internals, refcount for user values, documented no-cycle guarantee), then a
**precise mark-sweep GC** when the language grows closures-in-mutable-refs and
general `let rec` data. The compiler already knows every type layout, which is
what makes a precise GC feasible.

### What actually shipped

A **conservative mark-sweep collector with a chunked bump allocator**
(`src/Codegen.fs` preamble):

- objects are bump-allocated inside 8 MB chunks; freed objects are recycled
  through 512 fixed-size classes at 16-byte granularity;
- the block header is 8 bytes (`uint32 total`, `uint32 state`), and a free
  block's payload holds the free-list link, so overhead is 8 bytes/object;
- each chunk lazily keeps the **start addresses of its objects in increasing
  order**, extended at collection time by walking only the part of the chunk
  allocated since the last collection (bump allocation only appends, so this
  is a sequential scan — never a sort);
- pointer→object lookup is a chunk range check plus a binary search inside one
  chunk, which gives interior-pointer support for conservative scanning;
- roots are the stack (via `__builtin_unwind_init()`), registered globals and
  the export initialiser.

Two redesigns drove the allocation-heavy benchmark from ~5× slower than F#
NativeAOT to near parity: replacing per-object `malloc` with bump + size-class
recycling, and replacing the per-collection `qsort` of the whole heap with the
chunk-walk index above (`du_eval`: 629 ms → 132 ms, vs 97 ms for F# AOT).

Known trade-off: the collector never moves objects (conservative stack roots
make copying unsafe), so there is no generational/nursery promotion yet; that
is the next step if allocation-heavy code needs more.

---

## 7. Project layout

```
fss/
  DESIGN.md            <- this document
  src/                 <- the compiler itself (F#, built with dotnet)
    Fss.fsproj
    Lexer.fs
    Parser.fs
    Ast.fs
    Typer.fs           <- inference + exhaustiveness
    Lower.fs           <- typed AST -> C
    Emit.fs            <- C pretty-printer
    Driver.fs          <- CLI: fss build ./src.fs [-o out | -lib]
  runtime/             <- C library linked into every output
    fsval.h  memory.c  string.c  list.c  array.c  option.c  result.c  io.c  math.c
  tests/
    *.fs               <- one per feature, compiled + run by a test script
  examples/
```

Compiler CLI sketch:

```
fss build main.fs -o hello        # -> hello.c -> cc -> ./hello
fss build lib.fs --lib -o libfoo  # -> libfoo.c -> cc -shared -> libfoo.so / .a
fss build src/*.fs -o app         # multiple files, link them
```

---

## 8. Milestones

1. ~~**M0 — walk before running**: lexer + parser + AST + pretty-print for the
   core expression grammar (`let`, `if`, arithmetic, tuples, `match` on DUs).~~
   **done** (plus codegen — see M1).
2. ~~**M1 — integers & control flow**: type check `int`/`bool`, emit C for
   `let`, `if`, `while`, arithmetic. Produce a runnable binary via gcc.~~
   **done**, with a 13-case test suite and a native (AOT) compiler binary.
3. ~~**M2 — functions**: lambdas, recursion, closures, self-tail-call → loop.~~
   **done** — monomorphic functions, `let rec`, lambdas, closures, partial
   application, direct-call optimization, tail-position returns for gcc sibling
   calls. Generics (polymorphism) deferred to M5; local `let rec` and passing a
   function as a bare value are rejected for now.
4. ~~**M3 — data**: records, DUs, options, lists, pattern matching, structural
   equality.~~ **done** (runtime codegen inlined rather than a separate
   `runtime/`; types are monomorphic and non-recursive user types for now).
5. ~~**M4 — strings & I/O**: UTF-8 strings, `printf`, file read/write, `argv`.~~
   **done** — string literals, `+`/`=`, `str_len`, `print_string`,
   `int_to_string`/`bool_to_string`, `read_line`, `read_file`, `write_file`,
   and `let main argv = ...`. Full `sprintf` (format-string parsing) is
   deferred; the `*_to_string` helpers + `+` cover the common cases.
6. ~~**M5 — generics & modules**: `module`/`open`, multi-file, monomorphization or
   boxed generics, library output mode.~~ **done** — let-polymorphism with
   per-use monomorphization, and multi-file programs (`fss build a.fs b.fs`).
   `module`/`open` namespacing and library output are deferred.
7. ~~**M6 — memory model hardening**: replace refcount with precise GC if needed,
   exceptions (`setjmp`) or `Result` codegen, `try/finally`.~~
   **done** — conservative mark-sweep GC (stack + registered-global roots,
   sorted-index pointer lookup), and `failwith`/`try … with` exceptions via
   `setjmp`/`longjmp`. `raise`/`try/finally` remain.
8. **M7 — self-hosting**: rewrite the compiler in fss itself, compile it with
   the bootstrap compiler, then have it compile itself. The .NET/AOT build is a
   bootstrap seed only. In progress — `bootstrap/lexer.fs` (tokenizer) and
   `bootstrap/parser.fs` (S-expression parser) are written in fss and tested;
   `elif`, `str_get`/`str_sub`/`int_of_string`, top-level `let mutable`, and
   **recursive user-defined types** (pointer-boxed records/unions) were added.
   The self-hosted lexer, parser (recursive `Expr` AST), type checker, and a
   C code generator (`bootstrap/compiler.fs`) are written in fss and tested
   end-to-end (fss → C → gcc → binary). The self-hosted compiler now also
   implements Hindley-Milner let-polymorphism with per-use monomorphization,
   options, mutable globals + assignment, n-ary tuples, structural equality
   (`=`/`<>` on lists/options/tuples/unions/records via generated `eq_*`
   functions), and a builtin runtime (string ops, I/O, `failwith`). It
   compiles `bootstrap/lexer.fs`, `bootstrap/parser.fs`, and
   `bootstrap/typer.fs` to native binaries matching the .NET-built output.
   `bootstrap/compiler.fs` now **self-hosts**: the self-hosted compiler
   compiles itself to C, which compiles to a working native binary that
   reproduces the .NET-built compiler's output on the test programs
   (`compiler_input.fs`, `lexer.fs`). Remaining for full parity: the
   self-compiled binary still mis-specializes a polymorphic helper
   (`reverse`) in one multi-instantiation edge case, and the emitted C
   runtime lacks the .NET compiler's conservative GC and `try`/`with`
   exceptions (it uses `malloc`/`exit` instead).
9. ~~**Language surface for real F# files**~~ — **done (v0.3)**: `|>`/`<|`
   pipes, `@` list append, `for i = a to b` / `downto` / `for x in xs do`
   (all lowered to existing constructs), local `let rec` and mutual
   `let rec … and …` (closures held in heap cells so they can reference each
   other), and layout-style local bindings with indented bodies.
10. ~~**Standard library**~~ — **done (v0.3)**: `src/Prelude.fs` is a small
   `List`/`Option`/`String` library written in fss, parsed and prepended to
   every program by the driver. The parser rewrites `List.map` &c. to mangled
   prelude names (`__list_map`), so the library needs no special typing or
   codegen and is monomorphised per use like user code.
11. ~~**C interop**~~ — **done**. `double` is a first-class type;
   `extern name(p: t, ...) : ret` declares foreign C functions;
   `#include "h"` / `#include <h>` and `#link "lib"` (or the
   F#-compatible `[<Include("...")>]` / `[<Link("...")>]` attributes) pull in
   headers and linker flags. Each extern becomes a small static wrapper over
   the real C symbol, so headers with different const-ness do not clash;
   `cstring` is a raw `char*`, and C function-pointer callbacks are supported.
   `[<Export>]` + `fss build --lib` produces a `.so`/`.dll` plus a generated
   header so C can call into fss.
12. **Later**: arrays beyond 1-D, `seq`, Map/Set, computation expressions, the
   OO subset, wasm target, zero-copy record structs / opaque `ptr` types and
   C-function-pointer callbacks for interop.

---

## 9. Open questions to settle before coding

1. Compiler implementation language — **F# for now, fss later (self-hosting)**.
   The bootstrap compiler is F# on .NET (published as a native AOT binary so
   compile time stays minimal). The end goal is to rewrite it in the fss
   subset and compile it with itself, removing the .NET dependency entirely.
   To keep that cheap, the compiler core stays free of reflection, `%A`/`%d`
   printf, and other .NET-specific constructs; it leans on lists/maps/strings
   that the runtime library will provide.
2. Value representation start point — uniform `fsval_t` (v0) vs direct
   specialization (later). Recommended: uniform first.
3. Memory model v0 — arena + refcount vs tiny GC. Recommended: arena + refcount.
4. String encoding — UTF-8 vs UTF-16. Recommended: UTF-8 + length.
5. Exceptions — `failwith` = abort (C# Zero style) in v1, add real exceptions or
   `Result`-return codegen later. Recommended: abort first, `Result` everywhere.
6. How much of the OO subset — recommend **none** in v1; F#'s value is in the
   functional core anyway.

---

## 10. Bottom line

- **Keep**: the whole functional surface — `let`, functions, tuples, records,
  DUs, pattern matching, modules, type inference, units of measure (erased).
- **Convert**: closures, currying, tail calls, generics, structural equality,
  name mangling, pattern-match lowering.
- **Reimplement**: a small C runtime — strings, lists, arrays, option/result,
  printf, I/O, and the memory allocator.
- **Drop (v1)**: OO (classes/interfaces), async/tasks, reflection, quotations,
  type providers, .NET BCL/NuGet interop, catching exceptions.
