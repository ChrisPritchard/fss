#!/usr/bin/env bash
# AOT benchmark: fss (native C) vs C# NativeAOT vs F# NativeAOT.
# Usage: bash bench/aot_bench.sh
set -uo pipefail
cd "$(dirname "$0")/.."

FSS_CMD=(dotnet src/bin/Release/net9.0/fss.dll)
RID=win-x64
PROGS=(fib30 loop_count collatz str_concat prime_count)

echo "Building the fss compiler (once)..."
dotnet build src/Fss.fsproj -c Release -v q >/dev/null || { echo "fss build failed" >&2; exit 1; }

rm -rf bench/build
mkdir -p bench/build

now_ns() { date +%s%N; }
ms() { local s e; s=$(now_ns); "$@" >/dev/null 2>&1; e=$(now_ns); echo $(( (e - s) / 1000000 )); }
min3() { local best=999999999 t; for _ in 1 2 3; do t=$(ms "$@"); [ "$t" -lt "$best" ] && best=$t; done; echo "$best"; }

# Publish the AOT binaries (timed).
echo "Publishing C# NativeAOT..."
cs_ct=$(ms dotnet publish bench/aot/cs/cs.csproj -c Release -r "$RID" -o bench/build/aot_cs)
echo "Publishing F# NativeAOT..."
fs_ct=$(ms dotnet publish bench/aot/fs/fs.fsproj -c Release -r "$RID" -o bench/build/aot_fs)

CS_BIN="bench/build/aot_cs/fss_bench_cs.exe"
FS_BIN="bench/build/aot_fs/fss_bench_fs.exe"

printf '\n%-12s | %-9s | %-9s | %-9s | %-9s | %-12s\n' \
    "program" "fss ms" "F# AOT ms" "C# AOT ms" "F# JIT ms" "output match"
printf '%.0s-' {1..78}; printf '\n'

for name in "${PROGS[@]}"; do
    fss_src="bench/programs/$name.fss"
    fs_src="bench/programs/$name.fs"
    [ -f "$fss_src" ] || continue

    # fss native
    fss_bin="bench/build/${name}_fss.exe"
    "${FSS_CMD[@]}" build "$fss_src" -o "bench/build/${name}_fss" >/dev/null 2>&1
    fss_rt=$(min3 "$fss_bin")
    fss_out="$("$fss_bin" 2>/dev/null | tr -d '\r')"

    # F# JIT (fsc) for reference
    FSC="$(dotnet --list-sdks | tail -1)"
    sdk_ver="${FSC%% *}"; sdk_root="${FSC#*[}"; sdk_root="${sdk_root%]}"
    fsc="${sdk_root//\\//}/$sdk_ver/FSharp/fsc.dll"
    fs_bin="bench/build/${name}_fsjit.exe"
    dotnet "$fsc" --target:exe --optimize+ "$fs_src" -o "$fs_bin" >/dev/null 2>&1
    echo '{ "runtimeOptions": { "tfm": "net9.0", "framework": { "name": "Microsoft.NETCore.App", "version": "9.0.0" } } }' > "bench/build/${name}_fsjit.runtimeconfig.json"
    fsjit_rt=$(min3 dotnet "$fs_bin")

    # AOT binaries
    fs_aot_rt=$(min3 "$FS_BIN" "$name")
    cs_aot_rt=$(min3 "$CS_BIN" "$name")

    fs_out="$("$FS_BIN" "$name" 2>/dev/null | tr -d '\r')"
    cs_out="$("$CS_BIN" "$name" 2>/dev/null | tr -d '\r')"
    if [ "$fss_out" = "$fs_out" ] && [ "$fss_out" = "$cs_out" ]; then match=yes; else match="NO"; fi

    printf '%-12s | %-9s | %-9s | %-9s | %-9s | %-12s\n' \
        "$name" "$fss_rt" "$fs_aot_rt" "$cs_aot_rt" "$fsjit_rt" "$match"
done

printf '\nPublish (compile) times: C# NativeAOT %s ms, F# NativeAOT %s ms.\n' "$cs_ct" "$fs_ct"
printf 'fss compile time is ~250 ms/program (lex + type + C + gcc).\n'
