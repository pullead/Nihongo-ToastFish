import csv
import copy
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKS_DIR = ROOT / "Resources" / "Content" / "packs"
LOCALIZATION_DIR = ROOT / "Resources" / "Content" / "localization" / "zh-cn"
MANIFEST_PATH = ROOT / "Resources" / "Content" / "manifest-builtin-jlpt.json"
JLPT_WORD_LIST_DIR = ROOT / "_external" / "jlpt-word-list"
HANABIRA_DIR = ROOT / "_external" / "hanabira.org-japanese-content"

PACK_VERSION = "2026.05.22"
MANIFEST_VERSION = "0.3.1"
GENERATED_AT = "2026-05-27T00:00:00Z"
LEVELS = ["N5", "N4", "N3", "N2", "N1"]


VOCAB_SOURCE = {
    "sourceId": "elzup-jlpt-word-list",
    "name": "elzup/jlpt-word-list",
    "url": "https://github.com/elzup/jlpt-word-list",
    "attribution": "Vocabulary data from elzup/jlpt-word-list; original deck data acknowledged by that project.",
    "notes": "Simplified Chinese meanings are generated from Nihongo ToastFish localization overlays.",
}

VOCAB_LICENSE = {
    "name": "MIT",
    "url": "https://github.com/elzup/jlpt-word-list/blob/master/LICENSE",
}

EXAMPLE_SOURCE = {
    "sourceId": "hanabira-org-japanese-content",
    "name": "hanabira.org Japanese content",
    "url": "https://github.com/tristcoil/hanabira.org-japanese-content",
    "attribution": "Content from hanabira.org. Attribution link required by the source repository README.",
    "notes": "Simplified Chinese meanings and explanations are generated from Nihongo ToastFish localization overlays.",
}

EXAMPLE_LICENSE = {
    "name": "Creative Commons License (variant not specified by source README)",
    "url": "https://github.com/tristcoil/hanabira.org-japanese-content",
}

GOJUON_SMOKE_REFERENCE = {
    "packId": "builtin-gojuon-smoke",
    "version": "0.1.0",
    "jlptLevel": "gojuon",
    "contentKind": "gojuon",
    "displayName": "五十音入门样本",
    "description": "用于验证内置五十音内容包格式的最小样本。",
    "path": "gojuon-smoke.json",
    "source": {
        "sourceId": "nihongo-toastfish-first-party",
        "name": "Nihongo ToastFish first-party content",
        "notes": "Created for schema and importer validation.",
    },
    "license": {
        "name": "Nihongo ToastFish first-party sample license",
    },
}


def normalize(value):
    if value is None:
        return ""
    return " ".join(str(value).strip().split())


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(data, ensure_ascii=False, indent=2)
    path.write_text(text + "\n", encoding="utf-8")


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_localization(pack_id):
    path = LOCALIZATION_DIR / f"{pack_id}.json"
    if not path.exists():
        return {}

    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("packId") != pack_id:
        raise ValueError(f"{path} packId does not match {pack_id}")

    items = data.get("items") or {}
    if not isinstance(items, dict):
        raise ValueError(f"{path} items must be an object keyed by contentId")
    return items


def apply_item_localization(item, localized_items):
    localized = localized_items.get(item.get("contentId"))
    if not localized:
        return False

    changed = False
    for field in ["meaningCn", "promptCn", "formation", "usageNote"]:
        value = normalize(localized.get(field))
        if value:
            item[field] = value
            changed = True
    return changed


def mark_localized_pack(pack, localized_count):
    if localized_count <= 0:
        return

    metadata = pack["pack"]
    metadata["description"] = metadata["description"] + " Chinese localization is applied from Nihongo ToastFish first-party overlays."
    source = metadata.get("source") or {}
    notes = normalize(source.get("notes"))
    localization_note = "Chinese localization is first-party Nihongo ToastFish content."
    source["notes"] = localization_note if not notes else notes + " " + localization_note
    metadata["source"] = source


def build_vocabulary_pack(level):
    source_file = JLPT_WORD_LIST_DIR / "src" / f"{level.lower()}.csv"
    if not source_file.exists():
        raise FileNotFoundError(source_file)

    pack_id = f"builtin-vocabulary-{level.lower()}"
    localized_items = load_localization(pack_id)
    localized_count = 0
    items = []
    with source_file.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for index, row in enumerate(reader, start=1):
            expression = normalize(row.get("expression"))
            meaning = normalize(row.get("meaning"))
            if not expression or not meaning:
                continue

            item = {
                "contentId": f"elzup-vocab-{level.lower()}-{index:04d}",
                "headword": expression,
                "meaningCn": meaning,
            }

            reading = normalize(row.get("reading"))
            tags = normalize(row.get("tags"))
            if reading:
                item["reading"] = reading
            if tags:
                item["partOfSpeech"] = tags

            if apply_item_localization(item, localized_items):
                localized_count += 1

            items.append(item)

    pack = {
        "pack": {
            "packId": pack_id,
            "version": PACK_VERSION,
            "jlptLevel": level,
            "contentKind": "vocabulary",
            "displayName": f"{level} vocabulary reference pack",
            "description": f"{level} reference vocabulary imported from elzup/jlpt-word-list with Simplified Chinese meanings from Nihongo ToastFish localization overlays.",
            "source": copy.deepcopy(VOCAB_SOURCE),
            "license": copy.deepcopy(VOCAB_LICENSE),
        },
        "items": items,
    }
    mark_localized_pack(pack, localized_count)
    return pack


def build_example_pack(level):
    source_file = HANABIRA_DIR / "grammar_json" / f"grammar_ja_{level}_full_alphabetical_0001.json"
    if not source_file.exists():
        raise FileNotFoundError(source_file)

    pack_id = f"builtin-examples-{level.lower()}"
    localized_items = load_localization(pack_id)
    localized_count = 0
    grammar_points = json.loads(source_file.read_text(encoding="utf-8"))
    flat_examples = []
    for grammar_index, grammar in enumerate(grammar_points, start=1):
        grammar_id = f"hanabira-grammar-{level.lower()}-{grammar_index:04d}"
        for example_index, example in enumerate(grammar.get("examples") or [], start=1):
            sentence = normalize(example.get("jp"))
            meaning = normalize(example.get("en"))
            if not sentence or not meaning:
                continue
            flat_examples.append(
                {
                    "grammarId": grammar_id,
                    "sentenceJp": sentence,
                    "meaningCn": meaning,
                    "questionType": "choose_meaning",
                    "promptCn": meaning,
                    "correctAnswer": sentence,
                    "sourceTitle": normalize(grammar.get("title")),
                    "exampleIndex": example_index,
                }
            )

    sentence_pool = [item["sentenceJp"] for item in flat_examples]
    items = []
    for index, item in enumerate(flat_examples, start=1):
        distractors = []
        offset = 1
        while len(distractors) < 3 and offset < len(sentence_pool):
            candidate = sentence_pool[(index - 1 + offset) % len(sentence_pool)]
            if candidate != item["sentenceJp"] and candidate not in distractors:
                distractors.append(candidate)
            offset += 1

        content_item = {
            "contentId": f"hanabira-example-{level.lower()}-{index:04d}",
            "grammarId": item["grammarId"],
            "sentenceJp": item["sentenceJp"],
            "meaningCn": item["meaningCn"],
            "questionType": item["questionType"],
            "promptCn": item["promptCn"],
            "correctAnswer": item["correctAnswer"],
        }
        if distractors:
            content_item["distractors"] = distractors
        if apply_item_localization(content_item, localized_items):
            localized_count += 1
        items.append(content_item)

    pack = {
        "pack": {
            "packId": pack_id,
            "version": PACK_VERSION,
            "jlptLevel": level,
            "contentKind": "example",
            "displayName": f"{level} grammar example sentence reference pack",
            "description": f"{level} grammar example sentences imported from hanabira.org Japanese content with Simplified Chinese meanings from Nihongo ToastFish localization overlays.",
            "source": copy.deepcopy(EXAMPLE_SOURCE),
            "license": copy.deepcopy(EXAMPLE_LICENSE),
        },
        "items": items,
    }
    mark_localized_pack(pack, localized_count)
    return pack


def build_grammar_pack(level):
    source_file = HANABIRA_DIR / "grammar_json" / f"grammar_ja_{level}_full_alphabetical_0001.json"
    if not source_file.exists():
        raise FileNotFoundError(source_file)

    pack_id = f"builtin-grammar-{level.lower()}"
    localized_items = load_localization(pack_id)
    localized_count = 0
    grammar_points = json.loads(source_file.read_text(encoding="utf-8"))
    items = []
    for grammar_index, grammar in enumerate(grammar_points, start=1):
        title = normalize(grammar.get("title"))
        meaning = normalize(grammar.get("short_explanation"))
        if not title or not meaning:
            continue

        item = {
            "contentId": f"hanabira-grammar-{level.lower()}-{grammar_index:04d}",
            "pattern": title,
            "meaningCn": meaning,
        }

        formation = normalize(grammar.get("formation"))
        usage_note = normalize(grammar.get("long_explanation"))
        if formation:
            item["formation"] = formation
        if usage_note:
            item["usageNote"] = usage_note

        if apply_item_localization(item, localized_items):
            localized_count += 1

        items.append(item)

    pack = {
        "pack": {
            "packId": pack_id,
            "version": PACK_VERSION,
            "jlptLevel": level,
            "contentKind": "grammar",
            "displayName": f"{level} grammar reference pack",
            "description": f"{level} grammar points imported from hanabira.org Japanese content with Simplified Chinese explanations from Nihongo ToastFish localization overlays.",
            "source": copy.deepcopy(EXAMPLE_SOURCE),
            "license": copy.deepcopy(EXAMPLE_LICENSE),
        },
        "items": items,
    }
    mark_localized_pack(pack, localized_count)
    return pack


def validate_pack(pack):
    metadata = pack.get("pack") or {}
    items = pack.get("items") or []
    required_metadata = ["packId", "version", "jlptLevel", "contentKind", "displayName", "source", "license"]
    missing_metadata = [key for key in required_metadata if not metadata.get(key)]
    if missing_metadata:
        raise ValueError(f"{metadata.get('packId', '<unknown>')} missing metadata: {missing_metadata}")
    if not items:
        raise ValueError(f"{metadata['packId']} has no items")

    content_kind = metadata["contentKind"]
    required_by_kind = {
        "vocabulary": ["contentId", "headword", "meaningCn"],
        "grammar": ["contentId", "pattern", "meaningCn"],
        "example": ["contentId", "sentenceJp", "meaningCn"],
    }
    required_item_fields = required_by_kind[content_kind]
    seen_ids = set()
    for item in items:
        missing = [key for key in required_item_fields if not item.get(key)]
        if missing:
            raise ValueError(f"{metadata['packId']} item missing fields: {missing}")
        content_id = item["contentId"]
        if content_id in seen_ids:
            raise ValueError(f"{metadata['packId']} duplicate contentId: {content_id}")
        seen_ids.add(content_id)


def main():
    if not JLPT_WORD_LIST_DIR.exists():
        raise FileNotFoundError(f"Missing vocabulary source: {JLPT_WORD_LIST_DIR}")
    if not HANABIRA_DIR.exists():
        raise FileNotFoundError(f"Missing example source: {HANABIRA_DIR}")

    gojuon_reference = dict(GOJUON_SMOKE_REFERENCE)
    gojuon_reference["sha256"] = sha256_file(PACKS_DIR / gojuon_reference["path"])
    manifest_packs = [gojuon_reference]
    generated_counts = []

    for level in LEVELS:
        for pack in [build_vocabulary_pack(level), build_grammar_pack(level), build_example_pack(level)]:
            validate_pack(pack)
            metadata = pack["pack"]
            path = PACKS_DIR / f"{metadata['packId'].replace('builtin-', '')}.json"
            write_json(path, pack)
            manifest_packs.append(
                {
                    "packId": metadata["packId"],
                    "version": metadata["version"],
                    "jlptLevel": metadata["jlptLevel"],
                    "contentKind": metadata["contentKind"],
                    "displayName": metadata["displayName"],
                    "description": metadata["description"],
                    "path": path.name,
                    "sha256": sha256_file(path),
                    "source": metadata["source"],
                    "license": metadata["license"],
                }
            )
            generated_counts.append((metadata["packId"], len(pack["items"])))

    write_json(
        MANIFEST_PATH,
        {
            "manifestVersion": MANIFEST_VERSION,
            "generatedAt": GENERATED_AT,
            "baseUrl": "packs/",
            "packs": manifest_packs,
        },
    )

    for pack_id, count in generated_counts:
        print(f"{pack_id}: {count}")
    print(f"manifest: {MANIFEST_PATH}")


if __name__ == "__main__":
    main()
