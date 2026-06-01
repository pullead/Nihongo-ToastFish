@echo off
setlocal

set "INSTALL_DIR=%LOCALAPPDATA%\Programs\Nihongo ToastFish"
set "DESKTOP_SHORTCUT=%USERPROFILE%\Desktop\Nihongo ToastFish.lnk"
set "START_MENU_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Nihongo ToastFish"

taskkill /IM "Nihongo ToastFish.exe" /F >nul 2>nul

del "%DESKTOP_SHORTCUT%" >nul 2>nul
rd /S /Q "%START_MENU_DIR%" >nul 2>nul
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Nihongo ToastFish" /f >nul 2>nul

if exist "%INSTALL_DIR%" (
    rd /S /Q "%INSTALL_DIR%"
)

echo Nihongo ToastFish uninstalled.
endlocal
exit /b 0
