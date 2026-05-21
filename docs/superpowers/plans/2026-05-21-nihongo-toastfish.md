# Nihongo ToastFish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows tray Japanese learning app based on ToastFish with built-in N5-N1 vocabulary, grammar, example sentences, gojuon, furigana display, and manual content updates.

**Architecture:** Keep ToastFish as the first implementation base, then refactor toward services around notifications, hotkeys, study sessions, review scheduling, content storage, and content updates. Store content separately from user progress so built-in and downloaded content can be updated without resetting learning history.

**Tech Stack:** WPF, .NET Framework 4.7.2, C# 7.3, SQLite, Dapper, NPOI, Microsoft.Toolkit.Uwp.Notifications, System.Speech.

---

## Phase 0: Repository Setup And Baseline

### Task 1: Promote Upstream Source Into The Working Project

**Description:** Convert the cloned ToastFish source into the actual working application tree while preserving documentation at the repository root.

**Acceptance criteria:**

- [ ] ToastFish source files are in the main working tree or a clearly named application directory.
- [ ] The project builds from the documented path.
- [ ] Root documentation remains outside generated build output.

**Verification:**

- [ ] Run `nuget restore` from the solution directory.
- [ ] Run `msbuild ToastFish.sln /p:Configuration=Debug`.
- [ ] Confirm no build artifacts are tracked.

**Dependencies:** None

**Files likely touched:**

- `ToastFish.sln`
- `ToastFish.csproj`
- `.gitignore`
- `README.md`

**Estimated scope:** Medium

### Task 2: Add Project README And Development Notes

**Description:** Replace the upstream README with a project-specific README that explains the product, current status, build commands, and content licensing approach.

**Acceptance criteria:**

- [ ] README says this is based on ToastFish.
- [ ] README explains how to build locally.
- [ ] README explains that JLPT level data is reference content, not official complete JLPT lists.
- [ ] README links to ADR-001.

**Verification:**

- [ ] A new developer can identify the solution file and build command from README.

**Dependencies:** Task 1

**Files likely touched:**

- `README.md`
- `docs/decisions/ADR-001-base-on-toastfish-and-content-packs.md`

**Estimated scope:** Small

## Phase 1: Product Shell And Japanese-First Scope

### Task 3: Rename App Branding

**Description:** Update visible app naming from ToastFish to Nihongo ToastFish without changing internal namespaces yet.

**Acceptance criteria:**

- [ ] Window title shows `Nihongo ToastFish`.
- [ ] Tray tooltip shows `Nihongo ToastFish`.
- [ ] Notification title shows `Nihongo ToastFish`.
- [ ] Existing startup behavior still works.

**Verification:**

- [ ] Build succeeds.
- [ ] Launch app and confirm tray tooltip and initial notification text.

**Dependencies:** Task 1

**Files likely touched:**

- `View/ToastFish.xaml`
- `View/ToastFish.xaml.cs`
- `Model/PushControl/PushWords.cs`

**Estimated scope:** Small

### Task 4: Simplify Tray Menu Around Japanese Study

**Description:** Reorganize tray menu entries around Japanese content: start study, study content, content library, settings, pause, help, and exit.

**Acceptance criteria:**

- [ ] Japanese vocabulary and gojuon are visible first-class options.
- [ ] English vocabulary is hidden from the primary menu.
- [ ] Random test options are renamed as practice options.
- [ ] Start, pause, settings, and exit remain accessible.

**Verification:**

- [ ] Launch app and inspect tray menu.
- [ ] Selecting gojuon and Japanese vocabulary still starts sessions.

**Dependencies:** Task 3

**Files likely touched:**

- `View/ToastFish.xaml.cs`

**Estimated scope:** Medium

## Phase 2: Service Boundaries

### Task 5: Extract Notification Service

**Description:** Move toast construction and activation handling behind a `NotificationService` so study flows do not subscribe to global toast events repeatedly.

**Acceptance criteria:**

- [ ] Toast card rendering is called through a service interface.
- [ ] Toast activation is centrally handled.
- [ ] Hotkey and toast actions return the same action model.
- [ ] Existing vocabulary review buttons still work.

**Verification:**

- [ ] Build succeeds.
- [ ] Manual check: answer buttons in notifications advance a study session once, not multiple times.

**Dependencies:** Task 4

**Files likely touched:**

- Create: `Services/Notifications/NotificationService.cs`
- Create: `Services/Notifications/NotificationAction.cs`
- Modify: `Model/PushControl/PushWords.cs`
- Modify: `Model/PushControl/PushJpWords.cs`
- Modify: `Model/PushControl/PushGoinWords.cs`

**Estimated scope:** Medium

### Task 6: Replace Thread Abort With Session Cancellation

**Description:** Replace `Thread.Abort()` session stopping with a cancellation-aware study session controller.

**Acceptance criteria:**

- [ ] Starting a new session cancels or completes the previous session cleanly.
- [ ] Pause stops future notifications.
- [ ] App exit clears notifications and stops active work.

**Verification:**

- [ ] Start a session, pause it, start again, and confirm there is no duplicate notification stream.
- [ ] Build succeeds.

**Dependencies:** Task 5

**Files likely touched:**

- Create: `Services/Study/StudySessionController.cs`
- Modify: `View/ToastFish.xaml.cs`
- Modify: `Model/PushControl/PushWords.cs`

**Estimated scope:** Medium

## Phase 3: Content Data Model

### Task 7: Add Content Schema

**Description:** Add content tables for versioned content packs, vocabulary, grammar, examples, source metadata, learning progress, and review cards.

**Acceptance criteria:**

- [ ] Content records are separate from user progress.
- [ ] Vocabulary, grammar, and examples can all be reviewed through a shared review card table.
- [ ] Source/license metadata is stored with imported content.

**Verification:**

- [ ] Migration can create tables in a fresh SQLite database.
- [ ] Existing `Resources/inami.db` can still be opened.

**Dependencies:** Task 6

**Files likely touched:**

- Create: `Model/Content/ContentPack.cs`
- Create: `Model/Content/VocabularyItem.cs`
- Create: `Model/Content/GrammarPoint.cs`
- Create: `Model/Content/GrammarExample.cs`
- Create: `Model/Content/ContentSource.cs`
- Create: `Model/Study/ReviewCard.cs`
- Create: `Model/Storage/ContentSchemaMigrator.cs`

**Estimated scope:** Medium

### Task 8: Add Furigana Model And Formatting

**Description:** Add structured furigana data and render it in both toast-compatible inline format and WPF ruby-like format.

**Acceptance criteria:**

- [ ] Furigana can be stored as JSON segments.
- [ ] Toast output formats kanji as `漢字(かんじ)`.
- [ ] WPF detail view can render kana above kanji.
- [ ] Settings can choose automatic, always show, only difficult words, or hide.

**Verification:**

- [ ] Unit test formatting `日本語 + にほんご` to `日本語(にほんご)`.
- [ ] Manual check with a sentence containing multiple kanji words.

**Dependencies:** Task 7

**Files likely touched:**

- Create: `Model/Japanese/FuriganaSegment.cs`
- Create: `Services/Japanese/FuriganaFormatter.cs`
- Create: `View/Controls/FuriganaTextBlock.xaml`
- Create: `View/Controls/FuriganaTextBlock.xaml.cs`

**Estimated scope:** Medium

## Phase 4: Built-In Content Packs

### Task 9: Define Content Pack Format

**Description:** Define JSON schemas and manifest format for built-in and downloaded content.

**Acceptance criteria:**

- [ ] Manifest includes pack id, version, level, content type, hash, source, license.
- [ ] Vocabulary pack supports N5-N1.
- [ ] Grammar pack supports N5-N1.
- [ ] Example pack supports N5-N1.
- [ ] Gojuon pack is represented as content.

**Verification:**

- [ ] Schema validation rejects missing required fields.

**Dependencies:** Task 7

**Files likely touched:**

- Create: `Resources/Content/schema/content-manifest.schema.json`
- Create: `Resources/Content/schema/vocabulary-pack.schema.json`
- Create: `Resources/Content/schema/grammar-pack.schema.json`
- Create: `Resources/Content/schema/example-pack.schema.json`
- Create: `Resources/Content/schema/gojuon-pack.schema.json`

**Estimated scope:** Medium

### Task 10: Seed First Built-In Content

**Description:** Add initial curated content for gojuon and a small validated slice of each JLPT reference level so the import pipeline can be proven before full content expansion.

**Acceptance criteria:**

- [ ] Gojuon content imports.
- [ ] At least 10 vocabulary records per level import.
- [ ] At least 5 grammar records per level import.
- [ ] At least 10 example records per level import.
- [ ] Each imported record has source metadata.

**Verification:**

- [ ] Import command creates or updates `Resources/Content/builtin-content.db`.
- [ ] App can select imported content from the menu.

**Dependencies:** Task 9

**Files likely touched:**

- Create: `Resources/Content/packs/gojuon.json`
- Create: `Resources/Content/packs/vocabulary-n5.json`
- Create: `Resources/Content/packs/vocabulary-n4.json`
- Create: `Resources/Content/packs/vocabulary-n3.json`
- Create: `Resources/Content/packs/vocabulary-n2.json`
- Create: `Resources/Content/packs/vocabulary-n1.json`
- Create: `Resources/Content/packs/grammar-n5.json`
- Create: `Resources/Content/packs/grammar-n4.json`
- Create: `Resources/Content/packs/grammar-n3.json`
- Create: `Resources/Content/packs/grammar-n2.json`
- Create: `Resources/Content/packs/grammar-n1.json`
- Create: `Resources/Content/packs/examples-n5.json`
- Create: `Resources/Content/packs/examples-n4.json`
- Create: `Resources/Content/packs/examples-n3.json`
- Create: `Resources/Content/packs/examples-n2.json`
- Create: `Resources/Content/packs/examples-n1.json`
- Create: `Resources/Content/sources.json`
- Create: `Resources/Content/licenses.md`

**Estimated scope:** Large; split by level during implementation if needed.

## Phase 5: Study Flow

### Task 11: Add Unified Study Card Types

**Description:** Create a shared study card model for vocabulary, grammar, and examples.

**Acceptance criteria:**

- [ ] Vocabulary cards show word, reading, meaning, part of speech, and optional example.
- [ ] Grammar cards show pattern, meaning, formation, usage note, and example.
- [ ] Example cards show a prompt, choices, answer, and explanation.
- [ ] All card types produce review results.

**Verification:**

- [ ] Manual session can show one card of each type.

**Dependencies:** Task 8, Task 10

**Files likely touched:**

- Create: `Services/Study/StudyCard.cs`
- Create: `Services/Study/StudyCardFactory.cs`
- Modify: `Services/Notifications/NotificationService.cs`

**Estimated scope:** Medium

### Task 12: Add Mixed Study Mode

**Description:** Let users choose vocabulary only, grammar only, examples only, or mixed mode.

**Acceptance criteria:**

- [ ] Tray menu exposes study mode selection.
- [ ] The selected mode persists across restarts.
- [ ] Mixed mode interleaves due review cards and new cards.

**Verification:**

- [ ] Select each mode and start a session.
- [ ] Confirm only expected card types appear.

**Dependencies:** Task 11

**Files likely touched:**

- Modify: `View/ToastFish.xaml.cs`
- Modify: `Services/Study/StudySessionController.cs`
- Modify: `Services/Settings/SettingsService.cs`

**Estimated scope:** Medium

## Phase 6: Manual Content Updates

### Task 13: Add Content Update Service

**Description:** Implement manual manifest download, content pack download, hash verification, and import.

**Acceptance criteria:**

- [ ] User can click `Check for Updates`.
- [ ] App downloads manifest from configured URL.
- [ ] App downloads changed packs only.
- [ ] App verifies hash before import.
- [ ] Failed update leaves existing content usable.

**Verification:**

- [ ] Test with a local HTTP server or file URL manifest.
- [ ] Simulate bad hash and confirm import is rejected.

**Dependencies:** Task 9, Task 10

**Files likely touched:**

- Create: `Services/ContentUpdate/ContentManifestClient.cs`
- Create: `Services/ContentUpdate/ContentPackDownloader.cs`
- Create: `Services/ContentUpdate/ContentPackImporter.cs`
- Create: `Services/ContentUpdate/ContentUpdateService.cs`

**Estimated scope:** Medium

### Task 14: Add Content Library Settings View

**Description:** Add a settings page that shows installed content versions, sources, licenses, and update controls.

**Acceptance criteria:**

- [ ] User can see installed content pack versions.
- [ ] User can manually check for updates.
- [ ] User can view source/license notes.
- [ ] Update result is visible.

**Verification:**

- [ ] Manual check in WPF settings window.

**Dependencies:** Task 13

**Files likely touched:**

- Create: `View/Settings/ContentLibrarySettings.xaml`
- Create: `View/Settings/ContentLibrarySettings.xaml.cs`
- Create: `ViewModel/ContentLibrarySettingsViewModel.cs`

**Estimated scope:** Medium

## Phase 7: Polish, Verification, And Release

### Task 15: Add Low-Interruption Settings

**Description:** Add notification interval, pause duration, auto audio, and quiet mode settings.

**Acceptance criteria:**

- [ ] User can set notification interval.
- [ ] User can pause for 30 minutes, 1 hour, or today.
- [ ] User can enable or disable automatic audio.
- [ ] Settings persist.

**Verification:**

- [ ] Manual check each setting.

**Dependencies:** Task 12

**Files likely touched:**

- Create: `Services/Settings/SettingsService.cs`
- Modify: `View/ToastFish.xaml.cs`
- Modify: `Services/Study/StudySessionController.cs`

**Estimated scope:** Medium

### Task 16: Add Build And Release Documentation

**Description:** Document build, content pack generation, attribution requirements, and release checklist.

**Acceptance criteria:**

- [ ] README has quick start.
- [ ] Developer docs explain content pack format.
- [ ] Release checklist includes license/source verification.
- [ ] CI build is documented.

**Verification:**

- [ ] Follow README from a clean clone.

**Dependencies:** All prior phases

**Files likely touched:**

- `README.md`
- Create: `docs/content-packs.md`
- Create: `docs/release-checklist.md`

**Estimated scope:** Small

## Risks And Mitigations

| Risk | Impact | Mitigation |
|---|---:|---|
| Content licensing ambiguity | High | Track source/license per pack and write first-party grammar explanations. |
| Toast UI cannot render true ruby | Medium | Use `漢字(かな)` in toast and true ruby-like rendering only in WPF views. |
| Existing ToastFish code is tightly coupled | High | Extract service boundaries before adding large content features. |
| Content updates overwrite user progress | High | Store content and progress separately and test update imports. |
| JLPT level lists are treated as official | Medium | Label levels as reference levels in UI and docs. |

## Checkpoints

### Checkpoint A: Baseline

- [ ] Project builds.
- [ ] App launches.
- [ ] Tray and notifications still work.

### Checkpoint B: Japanese MVP

- [ ] Japanese vocabulary and gojuon sessions work.
- [ ] Tray menu is Japanese-first.
- [ ] Thread/session handling is stable.

### Checkpoint C: Content MVP

- [ ] Built-in content imports.
- [ ] Vocabulary, grammar, and example cards render.
- [ ] Furigana displays in notification-compatible format.

### Checkpoint D: Update MVP

- [ ] Manual update downloads and verifies content packs.
- [ ] Existing learning progress survives update.
- [ ] Content source/license info is visible.

### Checkpoint E: Release Candidate

- [ ] Build succeeds in Debug and Release.
- [ ] Core manual flows pass.
- [ ] README and license/source docs are complete.
