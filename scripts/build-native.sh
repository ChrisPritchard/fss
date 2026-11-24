#!/usr/bin/env bash
# Build a native (AOT) fss compiler. One-time cost; thereafter compilation
# has no .NET startup, which keeps fss->C compile times minimal.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet publish src/Fss.fsproj -c Release -p:PublishAot=true -o build/aot -v q
echo "built build/aot/fss.exe"
