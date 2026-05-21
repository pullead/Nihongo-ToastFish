# Development Status

## 2026-05-22

### Phase 0: Repository Setup And Baseline

Completed:

- Promoted upstream ToastFish source files into the repository root.
- Preserved project planning documents under `docs/`.
- Added `AGENTS.md` as the persistent project guide for future agent sessions.
- Replaced upstream README with a Nihongo ToastFish project README.
- Updated `.gitignore` to ignore runtime/build artifacts and the temporary upstream clone while keeping source folders trackable.
- Created initial baseline commit `a557b98` with the promoted source and project documentation.

Current blocker:

- Build verification cannot run in the current shell environment because `nuget`, `msbuild`, and `dotnet` are not available on `PATH`, and MSBuild was not found under the default Visual Studio installation path.
- Branch creation is blocked by `.git` ref lock permission errors. Attempts to create `codex/phase0-baseline` and `codex-phase0-baseline` failed, so the initial commit currently remains on `master`.

Attempted verification:

```powershell
nuget restore ToastFish.sln
```

Result:

```text
nuget: command not found
```

Toolchain discovery also found no `msbuild`, `dotnet`, or `nuget` command on `PATH`.

Next required verification on a Windows machine with Visual Studio/.NET Framework tooling:

```powershell
nuget restore ToastFish.sln
msbuild ToastFish.sln /p:Configuration=Debug
```

Do not mark Phase 0 complete until restore and build pass.
