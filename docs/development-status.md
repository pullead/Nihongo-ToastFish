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
- Installed local Microsoft Build Tools under `.local/BuildTools` for restore/build verification.
- Restored NuGet dependencies with local MSBuild.
- Built Debug successfully.
- Launched `bin\Debug\ToastFish.exe` successfully and confirmed the process started, then stopped it.

Current blocker:

- Branch creation is blocked by `.git` ref lock permission errors. Attempts to create `codex/phase0-baseline` and `codex-phase0-baseline` failed, so the initial commit currently remains on `master`.

Verification:

```powershell
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /t:Restore /p:RestorePackagesConfig=true
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
Start-Process -FilePath '.\\bin\\Debug\\ToastFish.exe' -WindowStyle Hidden
```

Result:

```text
Restore succeeded.
Debug build succeeded with 0 warnings and 0 errors.
Runtime smoke test started a ToastFish process successfully.
```

`msbuild`, `dotnet`, and `nuget` are still not on `PATH`; use the local MSBuild path above unless a full Visual Studio environment is available.

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

- Launch the app and manually inspect tray menu order.
- Confirm selecting gojuon and Japanese vocabulary still starts sessions.

### Phase 2: Service Boundaries

Completed:

- Added `Services/Notifications/NotificationAction.cs` and `Services/Notifications/NotificationService.cs`.
- Routed basic message toasts through `NotificationService.ShowMessage`.
- Centralized toast action waiting and hotkey mapping through `NotificationService.WaitForActionAsync`.
- Migrated vocabulary recitation, SM2 review, vocabulary quiz, and gojuon prompt waits away from direct `ToastNotificationManagerCompat.OnActivated` subscriptions.
- Added `NotificationInputResult` and routed toast selection input through `NotificationService.WaitForInputAsync`.
- Confirmed `Model/PushControl` no longer directly subscribes to Toast activation events or reads Toast `UserInput`.

Verification:

```powershell
rg "OnActivated|UserInput" Model Services -n
git diff --check
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
```

Result:

```text
Only Services\Notifications\NotificationService.cs contains Toast activation and UserInput handling.
Debug build succeeded with 0 warnings and 0 errors.
```

Remaining work:

- Extract database/content read-write boundaries before adding Japanese content packs.
- Extract review-session flow enough to keep vocabulary, grammar, sentence practice, and gojuon from duplicating notification loop logic.
- Add automated tests around pure mapping/import/review logic once those seams are separated from WPF and Toast APIs.
