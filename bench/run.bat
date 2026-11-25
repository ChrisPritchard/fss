@echo off
REM Benchmark: compile + run small programs in both fss and F#.
REM Usage: bench\run.bat   (or double-click)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1"
exit /b %errorlevel%
