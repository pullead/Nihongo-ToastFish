# Nihongo ToastFish

Nihongo ToastFish is a Windows tray learning app for low-interruption Japanese study. It is based on the open-source [Uahh/ToastFish](https://github.com/Uahh/ToastFish) project and keeps the original desktop strengths: tray residency, Windows toast notifications, global hotkeys, SQLite storage, Excel import, logs, and SM2-style review.

The product direction is Japanese-first:

- N5-N1 reference vocabulary
- N5-N1 grammar points
- N5-N1 grammar usage examples
- Gojuon
- Built-in offline content
- Manual online content updates
- Furigana support
- Local review progress

Manual Excel import remains available for custom content, but the app should be usable immediately after installation without requiring users to import spreadsheets.

## Project Status

This repository is in the early development phase. The upstream ToastFish source has been promoted into this working tree as the baseline for the first implementation.

Current priority:

1. Establish a clean build baseline.
2. Keep the app on WPF/.NET Framework 4.7.2 for the first MVP.
3. Refactor toward service boundaries before adding large new features.
4. Add versioned content packs for built-in and manually updated Japanese content.

## Source Of Truth

Read these documents before changing product behavior, architecture, or implementation order:

- `AGENTS.md`
- `docs/product/nihongo-toastfish-design.md`
- `docs/decisions/ADR-001-base-on-toastfish-and-content-packs.md`
- `docs/superpowers/plans/2026-05-21-nihongo-toastfish.md`

## Tech Stack

- WPF
- .NET Framework 4.7.2
- C# 7.3
- SQLite
- Dapper
- NPOI
- Microsoft.Toolkit.Uwp.Notifications
- System.Speech

## Build

Requirements:

- Windows 10 or later
- Visual Studio 2019 or newer with .NET desktop development workload
- .NET Framework 4.7.2 targeting pack
- NuGet
- MSBuild

Commands from the repository root:

```powershell
nuget restore ToastFish.sln
msbuild ToastFish.sln /p:Configuration=Debug
```

Release build:

```powershell
nuget restore ToastFish.sln
msbuild ToastFish.sln /p:Configuration=Release
```

## Content Strategy

Nihongo ToastFish will use built-in offline content plus manual online updates. Content data and user learning progress must remain separate so content packs can be updated without resetting review history.

Planned built-in content:

- Gojuon
- N5-N1 vocabulary
- N5-N1 grammar
- N5-N1 example sentences

JLPT levels are treated as reference levels, not official complete JLPT lists.

Do not scrape learning websites at runtime. Use curated content packs with source and license metadata. Grammar explanations should be first-party text unless a source license explicitly permits redistribution.

## Upstream Attribution

This project is based on ToastFish by Uahh:

- Repository: https://github.com/Uahh/ToastFish
- License: MIT, included in `LICENSE`

Original ToastFish provides the baseline WPF desktop app and Windows notification learning workflow.
