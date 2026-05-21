# Nihongo ToastFish Product Design

## Product Positioning

Nihongo ToastFish is a Windows tray learning app for low-interruption Japanese study. It extends the ToastFish idea from notification-based vocabulary review into a Japanese learning tool that covers vocabulary, grammar points, and grammar usage examples.

The app should be useful immediately after installation. Users should not need to import spreadsheets before their first study session.

## Core Decisions

- Base the first version on the existing ToastFish WPF desktop app.
- Keep Windows tray, toast notification, global hotkey, SQLite, Excel import, and SM2 review capabilities.
- Focus the product on Japanese learning instead of keeping English as a primary workflow.
- Ship built-in offline content for N5, N4, N3, N2, and N1.
- Use manual online content updates instead of scraping web pages at runtime.
- Support vocabulary, grammar explanations, and example sentence usage practice.
- Display furigana by default for beginner and intermediate levels.

## User Experience

The primary interaction is through Windows toast notifications.

Typical vocabulary card:

```text
勉強(べんきょう)
名詞 / する動詞
学习
```

Typical grammar teaching card:

```text
文法: 〜てもいい
意味: 可以做某事
接続: 動詞て形 + もいい
例: ここで写真(しゃしん)を撮(と)ってもいいです。
```

Typical grammar usage question:

```text
"可以在这里坐吗？"

A. ここに座(すわ)ってもいいですか。
B. ここに座(すわ)らなければなりませんか。
C. ここに座(すわ)ったことがありますか。
```

Toast notifications should use inline furigana formatting such as `漢字(かんじ)` because Windows toast does not reliably support true ruby layout. Full WPF views may render furigana as small text above kanji.

## Learning Modes

### Vocabulary

Users review Japanese words by JLPT reference level. Each card includes:

- Japanese expression
- Kana reading
- Chinese meaning
- Part of speech
- Optional example sentence
- Optional audio
- Review buttons: no impression, vague, remembered, mastered

### Grammar

Users review grammar points by JLPT reference level. Each grammar card includes:

- Grammar pattern
- Chinese meaning
- Formation / conjugation rule
- Usage note
- One short example sentence
- Review buttons

### Usage Examples

Users practice grammar through example sentences and questions. Supported first-version question types:

- Choose the correct sentence for a Chinese prompt.
- Choose the correct meaning of a Japanese sentence.
- Fill in the correct grammar pattern from options.

### Mixed Study

Users may choose:

- Vocabulary only
- Grammar only
- Example practice only
- Mixed vocabulary + grammar + examples

## Built-In Content

The app should include offline content packs:

- Gojuon
- N5 vocabulary
- N4 vocabulary
- N3 vocabulary
- N2 vocabulary
- N1 vocabulary
- N5 grammar
- N4 grammar
- N3 grammar
- N2 grammar
- N1 grammar
- N5 example sentences
- N4 example sentences
- N3 example sentences
- N2 example sentences
- N1 example sentences

JLPT levels should be labeled as reference levels, not official complete lists, because modern JLPT vocabulary and grammar lists are not officially published.

## Content Updates

The app should support manual online updates:

```text
Settings -> Content Library -> Check for Updates
```

Update process:

1. Download a content manifest.
2. Compare local and remote content versions.
3. Download changed content packs.
4. Verify hashes before import.
5. Import content into SQLite.
6. Preserve user learning progress and review scheduling data.
7. Show update results and content source notes.

The app should not scrape arbitrary web pages during normal use.

## Furigana Strategy

Content records should store both plain text and furigana data.

Required fields:

```text
sentenceJp
sentenceKana
furiganaJson
```

Example `furiganaJson`:

```json
[
  { "text": "日本語", "kana": "にほんご" },
  { "text": "を" },
  { "text": "勉強", "kana": "べんきょう" },
  { "text": "しています" }
]
```

Display rules:

- Toast: `日本語(にほんご)を勉強(べんきょう)しています。`
- WPF detail view: true ruby-like layout with kana above kanji.
- N5-N3: show furigana by default.
- N2-N1: show furigana automatically for configured difficult words, with a setting to always show or hide.

## Scope Exclusions For First Version

- No account system.
- No cloud sync.
- No mobile app.
- No AI-generated content in the first release.
- No runtime website scraping.
- No attempt to hide from workplace monitoring systems.
- No long-form course pages.

## Success Criteria

- A new user can install the app and immediately study N5 vocabulary or grammar.
- Built-in content works offline.
- Manual content update can refresh libraries without losing progress.
- Vocabulary, grammar, and example sentence cards all use the same review engine.
- Furigana is visible enough for beginner users without making notifications unreadable.
