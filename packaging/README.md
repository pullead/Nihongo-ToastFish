# Nihongo ToastFish Installer Packaging

This folder contains the per-user Windows installer packaging flow.

## Build Installer

Run from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File packaging\build-installer.ps1
```

If NuGet packages need to be restored first, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File packaging\build-installer.ps1 -Restore
```

The generated installer is saved to:

```text
dist\installers\Nihongo-ToastFish-Setup.exe
```

## Installer Behavior

- Installs to `%LOCALAPPDATA%\Programs\Nihongo ToastFish`
- Creates Desktop and Start Menu shortcuts
- Registers a per-user uninstall entry under Windows "Apps & features"
- Launches `Nihongo ToastFish.exe` after installation
- Does not modify or remove source project files

The app targets .NET Framework 4.7.2. Windows 10/11 normally includes a compatible runtime; older systems may need the runtime installed separately.
