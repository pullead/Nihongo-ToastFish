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

### Phase 3: Content Data Model

Completed:

- Added POCO models for `ContentPack`, `ContentSource`, `VocabularyItem`, `GrammarPoint`, `GrammarExample`, and `ReviewCard`.
- Added `ContentSchemaMigrator` to create separated content and progress tables in SQLite.
- Wired the migrator into the existing `Select` database open path so the schema is created idempotently at runtime.
- Kept content records (`VocabularyItem`, `GrammarPoint`, `GrammarExample`) separate from user progress (`ReviewCard`).

Verification:

```powershell
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
$p = Start-Process -FilePath '.\\bin\\Debug\\ToastFish.exe' -WindowStyle Hidden -PassThru; Start-Sleep -Seconds 5; $alive = Get-Process -Id $p.Id -ErrorAction SilentlyContinue; if ($alive) { Stop-Process -Id $p.Id; 'runtime smoke ok' } else { 'process exited early' }
```

Runtime SQLite check:

```text
ContentPack
ContentSource
GrammarExample
GrammarPoint
ReviewCard
VocabularyItem
```

Remaining work:

- Add structured furigana model and toast formatter.
- Define JSON content pack schema and importer validation.
- Add content repository methods for reading built-in vocabulary, grammar, and examples.

### Phase 3: Furigana Formatting

Completed:

- Added `Model/Japanese/FuriganaSegment.cs` for structured furigana segments.
- Added `Services/Japanese/FuriganaFormatter.cs` to parse furigana JSON arrays and format Toast-compatible inline text.
- Added `NotificationService.ShowFuriganaMessage` so future Japanese content cards can render `漢字(かな)` in Windows toast notifications.
- Kept malformed or missing furigana JSON safe by falling back to plain text.

Verification:

```powershell
$refs = @('System.Runtime.Serialization','System.Xml'); Add-Type -Path @('Model\\Japanese\\FuriganaSegment.cs','Services\\Japanese\\FuriganaFormatter.cs') -ReferencedAssemblies $refs; $formatter = New-Object ToastFish.Services.Japanese.FuriganaFormatter; $json = '[{\"text\":\"日本語\",\"kana\":\"にほんご\"},{\"text\":\"を\"},{\"text\":\"勉強\",\"kana\":\"べんきょう\"}]'; $inline = $formatter.ToInlineText($json, 'fallback', $true); $hidden = $formatter.ToInlineText($json, 'fallback', $false); $fallback = $formatter.ToInlineText('bad json', '日本語', $true)
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
```

Result:

```text
Inline: 日本語(にほんご)を勉強(べんきょう)
Hidden: 日本語を勉強
Fallback: 日本語
Debug build succeeded with 0 warnings and 0 errors.
```

Remaining work:

- Add a WPF ruby-like text control for detail/settings views.
- Use `ShowFuriganaMessage` when the new content repository starts producing vocabulary, grammar, and example cards.

### Phase 4: Content Pack Schema

Completed:

- Added JSON schemas under `Resources/Content/schema/` for:
  - content manifest
  - vocabulary packs
  - grammar packs
  - example sentence packs
  - gojuon packs
- Added schema files to `ToastFish.csproj` with `PreserveNewest` output copying.
- Manifest schema requires pack id, version, JLPT/reference level, content kind, path, SHA-256 hash, source metadata, and license metadata.
- Pack schemas cover N5, N4, N3, N2, and N1 where applicable; gojuon is represented as its own content kind and level.

Verification:

```powershell
Get-ChildItem Resources\\Content\\schema\\*.json | ForEach-Object { $null = Get-Content $_.FullName -Raw | ConvertFrom-Json; $_.Name }
node -e "<lightweight required-field/schema presence check>"
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
Get-ChildItem bin\\Debug\\Resources\\Content\\schema\\*.json | Select-Object Name,Length
```

Result:

```text
All schema JSON files parse successfully.
Required-field presence checks passed.
Debug build succeeded with 0 warnings and 0 errors.
Schema files copy to bin\\Debug\\Resources\\Content\\schema.
```

Remaining work:

- Add first small built-in content packs that conform to these schemas.
- Add importer validation with a real JSON Schema validator or equivalent in-process validation before SQLite import.

### Phase 4: Built-In Smoke Content

Completed:

- Added first-party smoke content packs under `Resources/Content/packs/`:
  - `gojuon-smoke.json`
  - `vocabulary-n5-smoke.json`
  - `grammar-n5-smoke.json`
  - `examples-n5-smoke.json`
- Added `Resources/Content/manifest-smoke.json` with SHA-256 hashes for each pack.
- Added `Resources/Content/licenses.md` to document that smoke content is first-party sample content and not a complete JLPT list.
- Added these content files to `ToastFish.csproj` with `PreserveNewest` output copying.

Verification:

```powershell
node - <manifest/hash/furigana-json validation script>
Get-ChildItem Resources\\Content -Recurse -Filter *.json | ForEach-Object { $null = (Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json); $_.Name }
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
Get-ChildItem bin\\Debug\\Resources\\Content -Recurse -File | Select-Object FullName,Length
```

Result:

```text
Manifest lists 4 packs.
Pack SHA-256 values match manifest.
All nested furiganaJson fields parse as JSON.
All content JSON files parse with UTF-8.
Debug build succeeded with 0 warnings and 0 errors.
Content files copy to bin\\Debug\\Resources\\Content.
```

Remaining work:

- Add importer validation and SQLite import into the separated content schema.
- Expand from smoke content to fuller N5-N1 pack coverage after importer behavior is tested.

### Phase 4/6: Verified Local Content Pack Importer

Completed:

- Added `GojuonItem` model and SQLite table so gojuon content is imported through the same content-pack path as vocabulary, grammar, and examples.
- Added `ContentPackImporter` for local manifest-based imports.
- Verified pack SHA-256 hashes before reading pack JSON into SQLite.
- Resolved pack paths relative to the manifest directory and rejected rooted or escaping paths.
- Imported content in a single SQLite transaction using parameterized commands.
- Kept content metadata and imported learning content separate from review progress tables.
- Adjusted the smoke manifest `baseUrl` to `packs/` so copied build output imports from `bin\Debug\Resources\Content`.

Verification:

```powershell
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
# Load bin\\Debug\\ToastFish.exe, run ContentSchemaMigrator, import manifest-smoke.json,
# then count ContentPack, ContentSource, GojuonItem, VocabularyItem, GrammarPoint, GrammarExample.
# Create a temporary manifest with a bad SHA-256 and confirm import rejection.
```

Result:

```text
Debug build succeeded with 0 warnings and 0 errors.
Import result: packs=4 sources=1 gojuon=5 vocab=5 grammar=3 examples=3.
SQLite counts: ContentPack=4, ContentSource=1, GojuonItem=5, VocabularyItem=5, GrammarPoint=3, GrammarExample=3.
Bad hash rejected with System.IO.InvalidDataException: Hash verification failed for gojuon-smoke.json.
```

Remaining work:

- Add downloader/update service around this importer for manual online updates.
- Add content repository queries for study card generation.
- Expand smoke packs into larger N5-N1 reference content after license/source metadata is finalized.

### Phase 5 Prep: Content Read Repository

Completed:

- Added `Services/Content/ContentRepository.cs` as the read boundary for imported content.
- Added parameterized read methods for gojuon, vocabulary, grammar points, and grammar examples.
- Added optional JLPT-level filtering for vocabulary, grammar, and examples.
- Added conservative read limits so future UI/study flows do not accidentally load unbounded content.

Verification:

```powershell
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
# Import manifest-smoke.json into bin\\Debug\\Resources\\inami.db,
# then query imported rows through ContentRepository.
```

Result:

```text
Debug build succeeded with 0 warnings and 0 errors.
ContentRepository probe returned:
gojuon=5 firstGojuon=a
vocab=5 firstVocab=学校
grammar=3 firstGrammar=Nです
examples=3 firstExample=example-n5-desu-nihongo
```

Remaining work:

- Add unified study card models and factory methods that consume `ContentRepository`.
- Route at least one tray study path through imported smoke content before expanding packs.

### Phase 5: Unified Study Card Model

Completed:

- Added `StudyCardKind`, `StudyCard`, and `StudyCardFactory`.
- Added card conversion for vocabulary, grammar points, grammar examples, and gojuon.
- Used `FuriganaFormatter` so vocabulary/example card text can preserve toast-compatible inline furigana.
- Parsed grammar example distractors into choices and inserted the correct answer when it is not already present.
- Kept this slice independent of WPF and Toast so the next step can wire it into study flows without mixing concerns.

Verification:

```powershell
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
# Import manifest-smoke.json into bin\\Debug\\Resources\\inami.db,
# read content through ContentRepository, then create four StudyCard instances.
```

Result:

```text
Debug build succeeded with 0 warnings and 0 errors.
StudyCardFactory probe returned:
vocab=Vocabulary|学校(がっこう)|がっこう / 名词 / 学校
grammar=Grammar|Nです|是……
example=Example|choices=4|answer=这是日语书。
gojuon=Gojuon|あ / ア|a
```

Remaining work:

- Add a notification presenter for `StudyCard`.
- Route one Japanese study entry through imported smoke content and `StudyCardFactory`.

### Phase 5: Study Card Toast Text Formatter

Completed:

- Added `StudyCardNotificationFormatter` to convert study cards into compact toast-friendly text.
- Formatted vocabulary and grammar cards as primary text, secondary text, and optional detail lines.
- Formatted example practice cards as prompt, answer choices, and the Japanese sentence without revealing extra explanation.
- Kept formatting separate from actual toast display so it can be verified without launching notifications.

Verification:

```powershell
.\\.local\\BuildTools\\MSBuild\\Current\\Bin\\MSBuild.exe ToastFish.sln /p:Configuration=Debug
# Import manifest-smoke.json, build study cards, and format vocabulary/example cards.
```

Result:

```text
Debug build succeeded with 0 warnings and 0 errors.
Vocabulary toast text:
学校(がっこう)
がっこう / 名词 / 学校
学校(がっこう)へ行(い)きます。
去学校。

Example toast text:
请选择句子的中文意思。
A. 这是日语书。
B. 我学习日语。
C. 这是学校。
D. 我喝水。
これは日本語(にほんご)の本(ほん)です。
```

Remaining work:

- Wire `StudyCardNotificationFormatter` into `NotificationService`.
- Add one imported-content study path from the tray menu.
