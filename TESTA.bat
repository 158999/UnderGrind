
@echo off
setlocal

cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Testing\run-tests.ps1"

echo.
echo Test uno fatto.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Tools\Testing\run-unity-tests.ps1"

echo.
echo Tutti i test terminati.
pause
