@echo off
setlocal

set "WORK_DIR=%TEMP%\NihongoToastFishInstaller_%RANDOM%%RANDOM%"
mkdir "%WORK_DIR%" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '%~dp0payload.zip' -DestinationPath '%WORK_DIR%' -Force"
if errorlevel 1 (
    echo Failed to extract installer payload.
    pause
    exit /b 1
)

call "%WORK_DIR%\install.cmd"
set "INSTALL_RESULT=%ERRORLEVEL%"

rd /S /Q "%WORK_DIR%" >nul 2>nul
endlocal & exit /b %INSTALL_RESULT%
