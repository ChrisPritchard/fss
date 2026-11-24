#!/usr/bin/env bash
# Benchmark: compile + run small programs in both fss (native C) and F# (.NET).
# Usage: bash bench/run.sh
set -uo pipefail
cd "$(dirname "$0")/.."

FSS_CMD=(dotnet src/bin/Release/net9.0/fss.dll)
FSC="/c/Program Files/dotnet/sdk/9.0.321/FSharp/fsc.dll"
RUNTIMECONFIG='{ "runtimeOptions": { "tfm": "net9.0", "framework": { "name": "Microsoft.NETCore.App", "version": "9.0.0" } } }'

echo "Building the fss compiler (once, not part of per-program timings)..."
dotnet build src/Fss.fsproj -c Release -v q >/dev/null

rm -rf bench/build
mkdir -p bench/build

now_ns() { date +%s%N; }

# time_ms <command...>  ->  wall-clock milliseconds (hard timeout 120s)
time_ms() {
    local s e
    s=$(now_ns)
    timeout 120 "$@" >/dev/null 2>&1
    e=$(now_ns)
    echo $(( (e - s) / 1000000 ))
}

# min_run <binary> <args...>  ->  best of 3 wall-clock ms
min_run() {
    local best=999999999 t
    for _ in 1 2 3; do
        t=$(time_ms "$@")
        [ "$t" -lt "$best" ] && best=$t
    done
    echo "$best"
}

printf '\n%-14s | %-16s | %-16s | %-14s | %-14s | %-12s\n' \
    "program" "fss compile(ms)" "F# compile(ms)" "fss run(ms)" "F# run(ms)" "output match"
printf '%.0s-' {1..100}; printf '\n'

for fss_src in bench/programs/*.fss; do
    name="$(basename "$fss_src" .fss)"
    fs_src="bench/programs/$name.fs"
    [ -f "$fs_src" ] || continue

    # --- compile fss -> native C -> gcc ---
    fss_bin="bench/build/${name}_fss"
    fss_ct=$(time_ms "${FSS_CMD[@]}" build "$fss_src" -o "$fss_bin")
    [ -f "$fss_bin" ] || fss_bin="$fss_bin.exe"
    fss_bin="bench/build/${name}_fss.exe"

    # --- compile F# -> .NET assembly ---
    fs_bin="bench/build/${name}_fs.exe"
    fs_ct=$(time_ms dotnet "$FSC" --target:exe --optimize+ "$fs_src" -o "$fs_bin")
    printf '%s' "$RUNTIMECONFIG" > "bench/build/${name}_fs.runtimeconfig.json"

    # --- run + verify output ---
    fss_out="$("$fss_bin" 2>/dev/null | tr -d '\r')"
    fs_out="$(dotnet "$fs_bin" 2>/dev/null | tr -d '\r')"
    if [ "$fss_out" = "$fs_out" ]; then match=yes; else match="NO ($fss_out vs $fs_out)"; fi

    fss_rt=$(min_run "$fss_bin")
    fs_rt=$(min_run dotnet "$fs_bin")

    printf '%-14s | %-16s | %-16s | %-14s | %-14s | %-12s\n' \
        "$name" "$fss_ct" "$fs_ct" "$fss_rt" "$fs_rt" "$match"
done

printf '\nTimings are wall-clock milliseconds (best of 3 runs for execution).\n'
printf 'fss compiles fss -> C -> native binary via gcc -O2.\n'
printf 'F# is compiled with fsc --optimize+ and runs on the .NET 9 runtime (JIT).\n'
