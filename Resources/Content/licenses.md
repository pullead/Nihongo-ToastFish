# Nihongo ToastFish Content Licenses

## First-Party Smoke Content

The files named `*-smoke.json` under `Resources/Content/packs/` are first-party sample content created for Nihongo ToastFish schema and importer validation.

They are intentionally small and are not a complete JLPT list. JLPT labels are reference levels only.

Do not copy explanations or example sentences from learning websites into these files unless their license allows redistribution and the source metadata is recorded.

## Built-In JLPT Reference Vocabulary

Files:

- `Resources/Content/packs/vocabulary-n5.json`
- `Resources/Content/packs/vocabulary-n4.json`
- `Resources/Content/packs/vocabulary-n3.json`
- `Resources/Content/packs/vocabulary-n2.json`
- `Resources/Content/packs/vocabulary-n1.json`

Source: [elzup/jlpt-word-list](https://github.com/elzup/jlpt-word-list), commit `13aa3c54b27115be72d8a62cd4071077c68d2171`.

License: [MIT](https://github.com/elzup/jlpt-word-list/blob/master/LICENSE).

Notes:

- The source project acknowledges original deck data from `chyyran/jlpt-anki-decks`, based on decks from tanos.co.uk and forked from `jamsinclair/open-anki-jlpt-decks`.
- Meanings are generated with Simplified Chinese localization overlays in `Resources/Content/localization/zh-cn/`.
- JLPT levels are reference labels, not official complete modern JLPT lists.

## Built-In JLPT Reference Example Sentences

Files:

- `Resources/Content/packs/examples-n5.json`
- `Resources/Content/packs/examples-n4.json`
- `Resources/Content/packs/examples-n3.json`
- `Resources/Content/packs/examples-n2.json`
- `Resources/Content/packs/examples-n1.json`

Source: [hanabira.org Japanese content](https://github.com/tristcoil/hanabira.org-japanese-content), commit `fb2d03f14e8000ef3c77612c7770e425c012c904`.

License: Creative Commons License, variant not specified by the source README. The README says reuse requires a link to `hanabira.org`.

Notes:

- Example sentences are extracted from `grammar_json/grammar_ja_N*_full_alphabetical_0001.json`.
- Meanings and prompts are generated with Simplified Chinese localization overlays in `Resources/Content/localization/zh-cn/`.
- These are grammar usage examples grouped by JLPT reference level.

## Simplified Chinese Localization Overlays

Files:

- `Resources/Content/localization/zh-cn/builtin-vocabulary-n5.json`
- `Resources/Content/localization/zh-cn/builtin-vocabulary-n4.json`
- `Resources/Content/localization/zh-cn/builtin-vocabulary-n3.json`
- `Resources/Content/localization/zh-cn/builtin-vocabulary-n2.json`
- `Resources/Content/localization/zh-cn/builtin-vocabulary-n1.json`
- `Resources/Content/localization/zh-cn/builtin-grammar-n5.json`
- `Resources/Content/localization/zh-cn/builtin-grammar-n4.json`
- `Resources/Content/localization/zh-cn/builtin-grammar-n3.json`
- `Resources/Content/localization/zh-cn/builtin-grammar-n2.json`
- `Resources/Content/localization/zh-cn/builtin-grammar-n1.json`
- `Resources/Content/localization/zh-cn/builtin-examples-n5.json`
- `Resources/Content/localization/zh-cn/builtin-examples-n4.json`
- `Resources/Content/localization/zh-cn/builtin-examples-n3.json`
- `Resources/Content/localization/zh-cn/builtin-examples-n2.json`
- `Resources/Content/localization/zh-cn/builtin-examples-n1.json`

Source: Nihongo ToastFish first-party Simplified Chinese localization, including machine-assisted draft translations from existing English source fields and project-authored manual corrections.

License: first-party project content.

Notes:

- These files override selected `meaningCn`, `promptCn`, `formation`, and `usageNote` fields during content pack generation.
- Run `scripts/check_content_localization.py` after generation to verify all generated packs have Chinese `meaningCn` and `promptCn` coverage.
- Machine-assisted Chinese draft text should be reviewed and improved over time, especially for nuance-sensitive grammar explanations and example sentence translations.
- Do not import Chinese explanations or example translations from third-party websites unless their redistribution license is compatible and recorded here.
