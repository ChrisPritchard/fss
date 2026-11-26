# fss vs F# benchmark

Small programs written in both **fss** (the subset compiler: fss → C → native via
gcc) and **F#** (compiled with the .NET F# compiler, running on the .NET 9 JIT).
Each program performs the same work in both languages and prints the same
result.

## Layout

- `programs/<name>.fs` — a **single** F# source file, compiled by both fss and
  F#. It uses the common subset: `[<EntryPoint>] let main _ = …` (fss ignores
  the attribute), `System.Console.WriteLine` (aliased to fss `println`), and
  F#-style indentation (fss's layout pass).
- `run.sh` / `run.bat` — compile with both, run each 3×, print a timing table
- `aot_bench.sh` — adds C# NativeAOT and F# NativeAOT comparisons

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

| name        | work                                                        |
|-------------|-------------------------------------------------------------|
| `fib30`     | naive recursive `fib 35` (≈ 9.2M calls)                     |
| `loop_count`| 100M-iteration mutable while loop                           |
| `sum_list`  | build + tail-recursive sum of a 20k list, 10×               |
| `collatz`   | total Collatz steps for 1..100000                           |
| `str_concat`| build a 20k-char string by repeated concatenation           |
| `prime_count`| trial-division prime count below 100000                     |
| `sort_list` | insertion-sort a 1500-element descending list               |
| `tree_sum`  | sum a 1000-node recursive union tree                        |

## Results (MSYS2, gcc 15, .NET 9, one run)

| program       | fss compile (ms) | F# compile (ms) | fss run (ms) | F# run (ms) |
|---------------|------------------|-----------------|--------------|-------------|
| collatz       | 245              | 683             | 45           | 102         |
| fib30         | 258              | 694             | 45           | 114         |
| loop_count    | 232              | 667             | 36           | 106         |
| prime_count   | 244              | 693             | 39           | 91          |
| sort_list     | 272              | 700             | 165          | 158         |
| str_concat    | 252              | 671             | 104          | 107         |
| sum_list      | 286              | 717             | 43           | 88          |
| tree_sum      | 320              | 739             | 36           | 86          |

Timings are wall-clock milliseconds; runs are the best of 3.

## AOT comparison

**F# NativeAOT** is now part of the main `run.sh`/`run.bat` table (published once,
run per program). `bench/aot_bench.sh` additionally compares **C# NativeAOT**:

```bash
bash bench/aot_bench.sh
```

Sample (MSYS2, gcc 15, .NET 9, win-x64):

| program      | fss ms | F# AOT ms | C# AOT ms | F# JIT ms |
|--------------|--------|-----------|-----------|-----------|
| fib30        | 43     | 65        | 61        | 118       |
| loop_count   | 32     | 56        | 54        | 107       |
| collatz      | 41     | 49        | 52        | 103       |
| str_concat   | 107    | 73        | 62        | 116       |
| prime_count  | 34     | 40        | 42        | 89        |

NativeAOT publish times were ~2.4 s (C#) and ~4.8 s (F#), vs ~250 ms per
program for fss (lex + type + C codegen + `gcc -O2`). fss native code is
competitive with NativeAOT on compute-bound loops, though `str_concat` is
slower because fss strings leak intermediate allocations until the 16 MB GC
threshold trips.

## Takeaways

- **Compile time**: fss is ~2.5× faster (~250 ms vs ~700 ms). fss compiles a
  single file through lex/parse/type-check/monomorphize/C-codegen and a `gcc
  -O2` pass; F# pays the full `fsc` + .NET toolchain startup.
- **Run time**: fss native binaries are ~2× faster on most workloads, but there
  is a ~35 ms startup floor (the generated `main` constructs the builtin
  environment and the runtime prologue string at every start). The .NET runtime
  has a ~90 ms startup/JIT floor.
- **Allocation-heavy code**: `sort_list` is the exception — fss (165 ms) is
  slightly *slower* than F# (158 ms). fss lists are `malloc`-per-node with no
  GC, while .NET's generational GC makes short-lived-list churn cheaper.
- **Known edge case**: a *tail-recursive* function that accumulates a string and
  is not TCO'd by the C compiler (e.g. `repeat n acc = repeat (n-1) (acc+"x")`
  in the fss codegen) can blow past the 16 MB GC threshold and degrade
  severely; the loop form is the idiomatic equivalent and is fast.
