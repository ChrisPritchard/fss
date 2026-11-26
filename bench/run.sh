#!/usr/bin/env bash
# Benchmark: compile + run small programs in both fss (native C) and F# (.NET).
# Usage: bash bench/run.sh
set -uo pipefail
cd "$(dirname "$0")/.."

FSS_CMD=(dotnet src/bin/Release/net9.0/fss.dll)

# --- locate fsc.dll in the newest .NET SDK (portable: Windows/MSYS2 and Linux/WSL) ---
find_fsc() {
    local line sdk_ver sdk_root
    line="$(dotnet --list-sdks 2>/dev/null | tail -1)"
    [ -z "$line" ] && return 1
    sdk_ver="${line%% *}"
    sdk_root="${line#*[}"
    sdk_root="${sdk_root%]}"
    sdk_root="${sdk_root//\\//}"       # backslashes -> forward slashes (harmless on Linux)
    printf '%s/%s/FSharp/fsc.dll' "$sdk_root" "$sdk_ver"
}

FSC="$(find_fsc)"
if [ -z "$FSC" ] || [ ! -f "$FSC" ]; then
    echo "error: could not locate fsc.dll. Is the .NET SDK (with F#) installed?" >&2
    exit 1
fi

# On Windows/MSYS2 the fss compiler emits a `.exe`; on Linux/WSL it does not.
case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) EXE=.exe; RID=win-x64 ;;
    *) EXE=; RID=linux-x64 ;;
esac

RUNTIMECONFIG='{ "runtimeOptions": { "tfm": "net9.0", "framework": { "name": "Microsoft.NETCore.App", "version": "9.0.0" } } }'

rm -rf bench/build
mkdir -p bench/build

echo "Building the fss compiler (once, not part of per-program timings)..."
dotnet build src/Fss.fsproj -c Release -v q >/dev/null || { echo "fss compiler build failed" >&2; exit 1; }

now_ns() { date +%s%N; }

# time_ms <command...>  ->  wall-clock milliseconds
time_ms() {
    local s e
    s=$(now_ns)
    "$@" >/dev/null 2>&1
    e=$(now_ns)
    echo $(( (e - s) / 1000000 ))
}

# min_run <command...>  ->  best of 3 wall-clock ms
min_run() {
    local best=999999999 t
    for _ in 1 2 3; do
        t=$(time_ms "$@")
        [ "$t" -lt "$best" ] && best=$t
    done
    echo "$best"
}

echo "Publishing F# NativeAOT (once)..."
fs_aot_ct=$(time_ms dotnet publish bench/aot/fs/fs.fsproj -c Release -r "$RID" -o bench/build/aot_fs)
FS_AOT_BIN="bench/build/aot_fs/fss_bench_fs.exe"

printf '\n%-14s | %-16s | %-16s | %-14s | %-14s | %-14s | %-12s\n' \
    "program" "fss compile(ms)" "F# compile(ms)" "fss run(ms)" "F# JIT(ms)" "F# AOT(ms)" "match"
printf '%.0s-' {1..100}; printf '\n'

for src in bench/programs/*.fs; do
    name="$(basename "$src" .fs)"

    # --- compile fss -> native C -> gcc (same source file) ---
    fss_bin="bench/build/${name}_fss${EXE}"
    fss_ct=$(time_ms "${FSS_CMD[@]}" build "$src" -o "bench/build/${name}_fss")

    # --- compile F# -> .NET assembly (same source file) ---
    fs_bin="bench/build/${name}_fs.exe"
    fs_ct=$(time_ms dotnet "$FSC" --target:exe --optimize+ "$src" -o "$fs_bin")
    printf '%s' "$RUNTIMECONFIG" > "bench/build/${name}_fs.runtimeconfig.json"

    # --- run + verify output ---
    fss_out="$("$fss_bin" 2>/dev/null | tr -d '\r')"
    fs_out="$(dotnet "$fs_bin" 2>/dev/null | tr -d '\r')"
    fs_aot_out="$("$FS_AOT_BIN" "$name" 2>/dev/null | tr -d '\r')"
    if [ "$fss_out" = "$fs_out" ] && [ "$fss_out" = "$fs_aot_out" ]; then match=yes; else match="NO"; fi

    fss_rt=$(min_run "$fss_bin")
    fs_rt=$(min_run dotnet "$fs_bin")
    fs_aot_rt=$(min_run "$FS_AOT_BIN" "$name")

    printf '%-14s | %-16s | %-16s | %-14s | %-14s | %-14s | %-12s\n' \
        "$name" "$fss_ct" "$fs_ct" "$fss_rt" "$fs_rt" "$fs_aot_rt" "$match"
done

printf '\nTimings are wall-clock milliseconds (best of 3 runs for execution).\n'
printf 'fss compiles fss -> C -> native binary via gcc -O2.\n'
printf 'F# JIT is compiled with fsc --optimize+ (run on the .NET 9 runtime).\n'
printf 'F# NativeAOT published once in %s ms (single self-contained binary).\n' "$fs_aot_ct"
