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
| closures     | 392         | 929        | **47**  | 93     | 52     | yes         |
| collatz      | 358         | 890        | **46**  | 93     | 59     | yes         |
| du_eval      | 385         | 993        | 132     | 176    | **97** | yes         |
| fib30        | 341         | 766        | **43**  | 102    | 68     | yes         |
| float_math   | 347         | 824        | **36**  | 82     | 42     | yes         |
| loop_count   | 320         | 785        | **33**  | 89     | 56     | yes         |
| prime_count  | 327         | 774        | **37**  | 74     | 44     | yes         |
| records      | 343         | 907        | 71      | 86     | **57** | yes         |
| sort_list    | 458         | 823        | **50**  | 154    | 75     | yes         |
| str_concat   | 349         | 819        | 69      | 93     | **59** | yes         |
| string_build | 368         | 834        | **35**  | 75     | 42     | yes         |
| sum_list     | 374         | 799        | **36**  | 74     | 41     | yes         |
| tree_sum     | 355         | 869        | **32**  | 70     | 39     | yes         |

fss wins 10 of 13 against F# NativeAOT and all 13 against F# JIT. The three
remaining losses are the allocation-heavy programs (`du_eval`, `records`,
`str_concat`), and the gap there is now 1.2-1.4× rather than 5×.

## AOT comparison

`bench/aot_bench.sh` additionally compares **C# NativeAOT**:

```bash
bash bench/aot_bench.sh
```

## Takeaways

- **Compile time**: fss is ~2.4× faster (~350 ms vs ~850 ms). fss runs a
  single pass of lex → parse → type-check → monomorphize → C codegen plus one
  `gcc -O2` invocation; F# pays `fsc` + .NET toolchain startup.
- **Run time, compute-bound**: fss native code beats F# NativeAOT by 1.2-1.8×
  (loops, recursion, prime counting, trees, closures, float math) because gcc
  optimises the emitted C directly. It beats F# JIT by ~2× on most programs.
- **Run time, allocation-bound**: this was a 5× loss and is now 1.2-1.4×.
  The runtime bump-allocates inside 8 MB chunks, recycles freed objects
  through 16-byte size classes (8-byte block headers) and keeps a per-chunk,
  append-only index of object starts — so a collection no longer sorts the
  heap, it walks only what was allocated since the last collection.
  `du_eval` (25M short-lived union nodes) went 629 ms → 132 ms,
  `records` 98 → 71 ms, `str_concat` 105 → 69 ms, `sort_list` 170 → 50 ms.
  .NET still wins these because it moves objects and collections are
  generational; fss's conservative collector can't move objects (its roots are
  ambiguous stack words), so there is no nursery yet.
- **Startup floor**: fss binaries start in ~2 ms; a .NET JIT run pays a ~90 ms
  runtime/JIT startup, which is why `fib30` (43 vs 102 ms) is not really about
  steady-state speed. F# NativeAOT removes that floor, and its startup is
  comparable to fss.
