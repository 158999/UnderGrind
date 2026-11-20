@echo off
setlocal

cd /d "%~dp0"

powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File ".\Tools\Testing\run-all-tests.ps1"
set "TEST_EXIT=%ERRORLEVEL%"

echo.
if "%TEST_EXIT%"=="0" (
    echo Tutte le suite sono state completate con successo.
) else (
    echo Esecuzione test fallita. Exit code: %TEST_EXIT%
)
echo Report aggregato: TestResults\test\all-latest.json
echo.
pause
exit /b %TEST_EXIT%
