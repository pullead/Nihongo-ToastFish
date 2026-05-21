# ADR-001: Base Nihongo ToastFish on ToastFish and Use Versioned Content Packs

## Status

Accepted

## Date

2026-05-21

## Context

We are building a Windows desktop Japanese learning app based on the open-source ToastFish project. ToastFish already provides several desktop-specific capabilities that are expensive to rebuild:

- Windows tray residency
- Windows toast notifications with action buttons
- Global hotkeys
- SQLite local storage
- Excel import
- Learning logs
- Japanese vocabulary and gojuon support
- SM2-style review scheduling

The new product needs to expand beyond vocabulary into grammar teaching and grammar usage example practice. It also needs to be usable immediately after installation without requiring users to import spreadsheets.

## Decision

Use the existing ToastFish WPF/.NET Framework 4.7.2 codebase as the first implementation base.

Add a versioned content pack system for built-in and manually updated Japanese learning content:

- N5-N1 vocabulary
- N5-N1 grammar
- N5-N1 example sentences
- Gojuon
- License and source metadata

Keep content data separate from user learning progress. Manual online updates should download signed or hash-verified content packs and import them into local SQLite without overwriting review history.

## Alternatives Considered

### Rewrite as a modern .NET 8 WPF app immediately

Pros:

- Cleaner project structure.
- Modern framework and tooling.
- Easier future maintenance.

Cons:

- High migration risk around toast notifications, global hotkeys, tray behavior, SQLite package behavior, and existing app lifecycle code.
- Delays product validation.
- Makes it harder to distinguish migration bugs from product bugs.

Rejected for the first version. Consider after a stable Japanese learning MVP exists.

### Keep manual Excel import as the main content path

Pros:

- Already supported by ToastFish.
- Simple to implement.

Cons:

- Poor first-run experience.
- Users must find or create their own content.
- Hard to provide consistent grammar and example sentence practice.

Rejected as the primary path. Keep Excel import as an advanced/custom content feature.

### Scrape online learning sites at runtime

Pros:

- Large amount of available content.
- Less initial manual content curation.

Cons:

- Web page structure is unstable.
- Copyright and terms-of-use risk.
- Network dependency hurts offline use.
- Quality and formatting vary heavily.

Rejected. Use curated content packs with source and license tracking.

### Use only third-party open datasets for all content

Pros:

- Faster content acquisition.
- Clear reuse path when licenses allow.

Cons:

- Grammar explanations in Chinese may not be available under suitable licenses.
- Example quality varies.
- Attribution and share-alike obligations must be tracked carefully.

Partially accepted. Use open datasets where suitable, but write first-party grammar explanations and source metadata.

## Consequences

- The first version can reuse the most important ToastFish desktop functionality.
- The codebase needs staged refactoring because ToastFish currently concentrates too much behavior in large files such as `View/ToastFish.xaml.cs`, `Model/PushControl/PushWords.cs`, and `Model/SqliteControl/Select.cs`.
- Built-in content requires a content generation/import pipeline.
- Content licensing must be visible in the app and documentation.
- The app can work offline while still supporting manual updates.

## Implementation Notes

Content and progress should be stored separately:

```text
ContentPack
VocabularyItem
GrammarPoint
GrammarExample
ContentSource
LearningProgress
ReviewCard
```

The content update flow should be:

1. Fetch remote manifest.
2. Compare local content versions.
3. Download changed packs.
4. Verify hash.
5. Import into SQLite in a transaction.
6. Preserve learning progress.
7. Record source and license metadata.

Furigana should be stored as structured data so notifications and WPF views can render different formats from the same content.
