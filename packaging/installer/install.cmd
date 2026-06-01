@echo off
setlocal

set "APP_NAME=Nihongo ToastFish"
set "INSTALL_DIR=%LOCALAPPDATA%\Programs\Nihongo ToastFish"
set "SOURCE_DIR=%~dp0app"
set "EXE_PATH=%INSTALL_DIR%\Nihongo ToastFish.exe"
set "DESKTOP_SHORTCUT=%USERPROFILE%\Desktop\Nihongo ToastFish.lnk"
set "START_MENU_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Nihongo ToastFish"
set "START_MENU_SHORTCUT=%START_MENU_DIR%\Nihongo ToastFish.lnk"

if not exist "%SOURCE_DIR%\Nihongo ToastFish.exe" (
    echo Installer package is incomplete.
    pause
    exit /b 1
)

if exist "%EXE_PATH%" (
    taskkill /IM "Nihongo ToastFish.exe" /F >nul 2>nul
)

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
xcopy "%SOURCE_DIR%\*" "%INSTALL_DIR%\" /E /I /Y >nul
if errorlevel 1 (
    echo Failed to copy application files.
    pause
    exit /b 1
)

if not exist "%START_MENU_DIR%" mkdir "%START_MENU_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell=New-Object -ComObject WScript.Shell; $s=$shell.CreateShortcut('%DESKTOP_SHORTCUT%'); $s.TargetPath='%EXE_PATH%'; $s.WorkingDirectory='%INSTALL_DIR%'; $s.IconLocation='%EXE_PATH%,0'; $s.Save(); $s=$shell.CreateShortcut('%START_MENU_SHORTCUT%'); $s.TargetPath='%EXE_PATH%'; $s.WorkingDirectory='%INSTALL_DIR%'; $s.IconLocation='%EXE_PATH%,0'; $s.Save()"

reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Nihongo ToastFish" /v "DisplayName" /d "Nihongo ToastFish" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Nihongo ToastFish" /v "DisplayVersion" /d "1.0.0" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Nihongo ToastFish" /v "Publisher" /d "Nihongo ToastFish" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Nihongo ToastFish" /v "InstallLocation" /d "%INSTALL_DIR%" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Nihongo ToastFish" /v "UninstallString" /d "\"%INSTALL_DIR%\uninstall.cmd\"" /f >nul

start "" "%EXE_PATH%"

echo Nihongo ToastFish installed successfully.
endlocal
exit /b 0
