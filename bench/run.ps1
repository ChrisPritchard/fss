# Benchmark: compile + run small programs in both fss (native C) and F# (.NET).
# Run via bench\run.bat, or directly:  powershell -NoProfile -File bench\run.ps1
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

# Locate fsc.dll in the newest .NET SDK (F# ships with the .NET SDK).
$sdkLine = dotnet --list-sdks 2>$null | Select-Object -Last 1
if (-not $sdkLine) {
    Write-Error 'The .NET SDK was not found. Install it (with F#) and re-run.'
    exit 1
}
$sdkVer = ($sdkLine -split '\s+')[0]
$sdkRoot = $sdkLine.Substring($sdkLine.IndexOf('[') + 1).TrimEnd(']')
$fsc = Join-Path $sdkRoot (Join-Path $sdkVer 'FSharp\fsc.dll')
if (-not (Test-Path $fsc)) {
    Write-Error "fsc.dll not found at '$fsc'. Is the F# compiler part of your SDK?"
    exit 1
}

Write-Host 'Building the fss compiler (once, not part of per-program timings)...'
dotnet build src\Fss.fsproj -c Release -v q *> $null
if ($LASTEXITCODE -ne 0) { Write-Error 'fss compiler build failed.'; exit 1 }

Remove-Item -Recurse -Force bench\build -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force bench\build | Out-Null

$runtimeconfig = '{ "runtimeOptions": { "tfm": "net9.0", "framework": { "name": "Microsoft.NETCore.App", "version": "9.0.0" } } }'

function Time-Command([scriptblock]$cmd) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $cmd *> $null
    $sw.Stop()
    return [math]::Round($sw.Elapsed.TotalMilliseconds)
}

function Min-Run([scriptblock]$cmd) {
    $best = [int64]::MaxValue
    for ($i = 0; $i -lt 3; $i++) {
        $t = Time-Command $cmd
        if ($t -lt $best) { $best = $t }
    }
    return $best
}

Write-Host 'Publishing F# NativeAOT (once)...'
$fsAotCT = Time-Command { dotnet publish bench\aot\fs\fs.fsproj -c Release -r win-x64 -o bench\build\aot_fs }
$fsAotBin = 'bench\build\aot_fs\fss_bench_fs.exe'

Write-Host ''
Write-Host ('{0,-14} {1,11} {2,11} {3,11} {4,11} {5,11}  {6}' -f 'program','fss cmp ms','F# cmp ms','fss run ms','F# JIT ms','F# AOT ms','match')
Write-Host ('-' * 92)

Get-ChildItem bench\programs\*.fs | Sort-Object Name | ForEach-Object {
    $name = $_.BaseName

    $fssBin = "bench\build\${name}_fss.exe"
    $fsBin  = "bench\build\${name}_fs.exe"

    $fssCT = Time-Command { dotnet src\bin\Release\net9.0\fss.dll build $_.FullName -o "bench\build\${name}_fss" }
    $fsCT  = Time-Command { dotnet $fsc --target:exe --optimize+ $_.FullName -o $fsBin }
    Set-Content -Path "bench\build\${name}_fs.runtimeconfig.json" -Value $runtimeconfig -NoNewline

    $fssOut = ((& $fssBin 2>$null) -join "`n").Trim()
    $fsOut  = ((& dotnet $fsBin 2>$null) -join "`n").Trim()
    $fsAotOut = ((& $fsAotBin $name 2>$null) -join "`n").Trim()
    $match = if ($fssOut -eq $fsOut -and $fssOut -eq $fsAotOut) { 'yes' } else { 'NO' }

    $fssRT = Min-Run { & $fssBin }
    $fsRT  = Min-Run { & dotnet $fsBin }
    $fsAotRT = Min-Run { & $fsAotBin $name }

    Write-Host ('{0,-14} {1,11} {2,11} {3,11} {4,11} {5,11}  {6}' -f $name,$fssCT,$fsCT,$fssRT,$fsRT,$fsAotRT,$match)
}

Write-Host ''
Write-Host 'Timings are wall-clock milliseconds (best of 3 runs for execution).'
Write-Host 'fss compiles fss -> C -> native binary via gcc -O2.'
Write-Host 'F# JIT is compiled with fsc --optimize+ (run on the .NET 9 runtime).'
Write-Host "F# NativeAOT published once in $fsAotCT ms (single self-contained binary)."
