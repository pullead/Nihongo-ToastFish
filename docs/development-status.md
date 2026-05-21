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

### Phase 1: Product Shell And Japanese-First Scope

Completed:

- Updated visible branding from `ToastFish` to `Nihongo ToastFish` for the main window, tray tooltip, notification title, duplicate-run message, and startup shortcut name.
- Reorganized the tray menu so Japanese study is the primary content entry.
- Moved old English content under `旧版英语词库`.
- Renamed practice and help labels for the Japanese-first product direction.
- Removed fragile tray-menu setup code that depended on `Cms.Items[index]` positions.

Static verification:

```powershell
[xml](Get-Content View\ToastFish.xaml -Encoding UTF8 -Raw)
git diff --check
rg "Cms\.Items\[[0-9]+\]|随机测试|英语词汇|日语词汇|开始！|参数设置|版本号：2\.3\.3|官方网站" -n View\ToastFish.xaml.cs
```

Result:

- XAML parses as XML.
- `git diff --check` passes.
- No old tray menu labels or numeric `Cms.Items[index]` access remain.

Remaining verification:

- Build with MSBuild.
- Launch the app and manually inspect tray menu order.
- Confirm selecting gojuon and Japanese vocabulary still starts sessions.
