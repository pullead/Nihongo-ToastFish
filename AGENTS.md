# Nihongo ToastFish Agent Guide

## Source Of Truth

Before implementing product, architecture, or planning changes, read these files first:

- `docs/product/nihongo-toastfish-design.md`
- `docs/decisions/ADR-001-base-on-toastfish-and-content-packs.md`
- `docs/superpowers/plans/2026-05-21-nihongo-toastfish.md`

Treat those documents as the current product direction unless the user explicitly changes the direction in a later request.

## Product Direction

Nihongo ToastFish is a Windows tray learning app for low-interruption Japanese study. It is based on the open-source ToastFish project and should evolve into a Japanese-first tool covering:

- N5-N1 vocabulary
- N5-N1 grammar
- N5-N1 example sentence practice
- Gojuon
- Built-in offline content
- Manual online content updates
- Furigana support
- SM2-style review scheduling

The app should be usable immediately after installation. Manual Excel import remains an advanced/custom-content feature, not the main onboarding path.

## Architecture Decisions

- Keep the first implementation on ToastFish's WPF/.NET Framework 4.7.2 base.
- Do not migrate to .NET 8 until a stable Japanese learning MVP exists.
- Use versioned content packs instead of runtime website scraping.
- Store content separately from user learning progress.
- Preserve tray, toast notification, global hotkey, SQLite, Excel import, log export, Japanese speech, and SM2 review capabilities.
- Display furigana in toast notifications as inline text such as `勉強(べんきょう)`.
- Use true ruby-like rendering only in WPF views where layout allows it.

## Scope Guardrails

Do not add these features unless the user explicitly asks:

- Account system
- Cloud sync
- Mobile app
- AI-generated content
- Runtime web scraping
- Workplace-monitoring evasion or stealth behavior
- Large course pages unrelated to notification-based learning

Do not copy copyrighted grammar explanations or example sentences from learning websites. Use open-license sources where appropriate and write first-party grammar explanations when needed.

## Implementation Workflow

Use the plan in `docs/superpowers/plans/2026-05-21-nihongo-toastfish.md` as the default implementation order:

1. Repository setup and build baseline
2. Japanese-first product shell
3. Service boundaries
4. Content data model
5. Built-in content packs
6. Study flow
7. Manual content updates
8. Polish, verification, and release docs

For each implementation task:

- Read the relevant source files before editing.
- Keep changes scoped to the current phase.
- Prefer small, verifiable increments.
- Run the relevant build/test command before claiming completion.
- Keep content and progress data separate.
- Track source and license metadata for content.

## Useful Skills

Use these skills when their situations apply:

- `context-engineering`: when starting or resuming work, or when project direction may drift.
- `documentation-and-adrs`: when changing product direction, architecture, content strategy, or public behavior.
- `planning-and-task-breakdown`: when turning requirements into ordered tasks.
- `writing-plans`: when creating detailed implementation handoff plans.
- `git-workflow-and-versioning`: before branches, commits, or major file moves.
- `incremental-implementation`: for multi-file implementation work.
- `test-driven-development`: for logic changes, importers, review scheduling, and content update behavior.
- `debugging-and-error-recovery`: for build, runtime, notification, SQLite, or update failures.
- `code-review-and-quality`: before considering a phase complete.
- `security-and-hardening`: for network content updates, hash verification, file downloads, and local database import.
- `frontend-ui-engineering`: for WPF settings views, content library UI, and furigana display controls.

## Development Notes

The upstream ToastFish source currently lives in `ToastFish-upstream/`. The root `docs/` directory contains project-specific planning and should be preserved if the source tree is reorganized.

Known high-risk files in upstream:

- `ToastFish-upstream/View/ToastFish.xaml.cs`
- `ToastFish-upstream/Model/PushControl/PushWords.cs`
- `ToastFish-upstream/Model/SqliteControl/Select.cs`

These files concentrate multiple responsibilities. Prefer extracting service boundaries before adding large new behavior.
