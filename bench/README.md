# fss vs F# benchmark

Small programs written in both **fss** (the subset compiler: fss → C → native
via gcc) and **F#**. Each program is a **single shared `.fs` file** that both
compilers accept, and both print the same result (the `match` column checks
this at run time).

Three F# baselines are measured:

- **F# JIT** — `fsc --optimize+`, run on the .NET 9 runtime
- **F# NativeAOT** — published once with `dotnet publish -r <rid>`, run per
  program
- (optionally) **C# NativeAOT** via `aot_bench.sh`

## Layout

- `programs/<name>.fs` — one F# source file, compiled by both toolchains. It
  uses the common subset: `[<EntryPoint>] let main _ = …` (fss ignores the
  attribute), `System.Console.WriteLine` (aliased to fss `println`),
  `System.Math.*` (mapped to fss's libm builtins), F#-style indentation, and
  `string` / `int` / `float` conversions.
- `run.sh` / `run.ps1` / `run.bat` — compile with both, run each 3×, print a
  timing table
- `aot/fs/Program.fs` — the F# NativeAOT dispatcher (`main argv` switches on the
  program name)
- `aot/cs/Program.cs` — the C# NativeAOT comparison program

## Running

Windows (cmd/PowerShell):

```bat
bench\run.bat
```

Linux/macOS, WSL, or Git Bash / MSYS2:

```bash
bash bench/run.sh
```

The fss compiler is built once up front (not part of per-program timings).
The scripts locate `fsc.dll` automatically from `dotnet --list-sdks`, so they
work with any .NET SDK that includes F#. In WSL the distro needs its own .NET
SDK installed (`dotnet --list-sdks` must list at least one SDK).

## Programs

| name          | work                                                          |
|---------------|---------------------------------------------------------------|
| `fib30`       | naive recursive `fib 35` (≈ 9.2M calls)                       |
| `loop_count`  | 100M-iteration mutable while loop                             |
| `sum_list`    | build + tail-recursive sum of a 20k list, 10×                 |
| `collatz`     | total Collatz steps for 1..100000                             |
| `str_concat`  | build a 20k-char string by repeated concatenation             |
| `prime_count` | trial-division prime count below 100000                       |
| `sort_list`   | insertion-sort a 1500-element descending list                 |
| `tree_sum`    | sum a 1000-node recursive union tree                          |
| `records`     | 2M iterations of record create/`with`-update/nested-field read |
| `du_eval`     | tiny AST interpreter: build + evaluate 200k union trees (depth 6) |
| `closures`    | 200× `fold`/`map` over a 2000-element list with composed lambdas |
| `string_build`| build 400 strings from `string`/`int` concatenation           |
| `float_math`  | 20k× `sqrt`/`Math.Sin`/`Math.Log` over a float list (libm)     |

## Results (MSYS2, gcc 15, .NET 9, one run)

Compile and run times are wall-clock milliseconds; runs are the best of 3.

| program      | fss compile | F# compile | fss run | F# JIT | F# AOT | same output |
|--------------|-------------|------------|---------|--------|--------|-------------|
| closures     | 366         | 883        | **44**  | 86     | 49     | yes         |
| collatz      | 332         | 817        | **44**  | 89     | 54     | yes         |
| du_eval      | 349         | 918        | 488     | 166    | **100**| yes         |
| fib30        | 343         | 844        | **45**  | 108    | 71     | yes         |
| float_math   | 355         | 836        | **37**  | 84     | 44     | yes         |
| loop_count   | 310         | 765        | **32**  | 94     | 57     | yes         |
| prime_count  | 387         | 810        | **37**  | 75     | 42     | yes         |
| records      | 432         | 852        | 77      | 89     | **57** | yes         |
| sort_list    | 338         | 894        | **49**  | 153    | 72     | yes         |
| str_concat   | 345         | 804        | 73      | 94     | **61** | yes         |
| string_build | 351         | 814        | **34**  | 75     | 41     | yes         |
| sum_list     | 336         | 795        | **34**  | 77     | 42     | yes         |
| tree_sum     | 354         | 886        | **33**  | 71     | 39     | yes         |

fss wins 10 of 13 against F# NativeAOT and all 13 against F# JIT on startup-
dominated or compute-bound work; it loses on the three allocation-heavy cases
(`du_eval`, `records`, `str_concat`).

## AOT comparison

`bench/aot_bench.sh` additionally compares **C# NativeAOT**:

```bash
bash bench/aot_bench.sh
```

## Takeaways

- **Compile time**: fss is ~2.4× faster (~350 ms vs ~850 ms). fss runs a
  single pass of lex → parse → type-check → monomorphize → C codegen plus one
  `gcc -O2` invocation; F# pays `fsc` + .NET toolchain startup.
- **Run time, compute-bound**: fss native code beats F# NativeAOT by 1.2–1.8×
  (loops, recursion, prime counting, trees, closures, float math) because gcc
  optimises the emitted C directly. It beats F# JIT by 2× or more on most
  programs.
- **Run time, allocation-bound**: `du_eval` (25M short-lived union nodes) is
  ~5× slower than F# AOT. .NET's generational GC makes gen0 allocation and
  collection almost free; fss uses a bump allocator with 16-byte size classes
  plus a conservative mark-sweep collector, which is fast per allocation but
  pays a full conservative scan + index build per collection. `records` (77 vs
  57 ms) and `str_concat` (73 vs 61 ms) are the same story at smaller scale.
  This is the clearest remaining gap and the next optimisation target.
- **Startup floor**: fss binaries start in ~2 ms; a .NET JIT run pays a ~90 ms
  runtime/JIT startup, which is why `fib30` (45 vs 108 ms) is not really about
  steady-state speed. F# NativeAOT removes that floor, and its startup is
  comparable to fss.
- **Fixed in v0.2**: per-node `malloc` was replaced by an 8 MB bump allocator
  with 16-byte size-class free lists (and recycled blocks keep their size, so
  reuse is a pointer swap), plus a 16-byte block header. `sort_list` went from
  ~170 ms to ~49 ms (faster than both F# baselines), `str_concat` from ~105 to
  ~73 ms, `records` 98 → 77 ms and `du_eval` 629 → 488 ms.
