import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKS_DIR = ROOT / "Resources" / "Content" / "packs"


ASCII_WORD_RE = re.compile(r"[A-Za-z]{2,}")


def looks_unlocalized(value):
    if not value:
        return True
    return bool(ASCII_WORD_RE.search(value))


def check_pack(path):
    data = json.loads(path.read_text(encoding="utf-8"))
    items = data.get("items") or []
    content_kind = (data.get("pack") or {}).get("contentKind")
    fields = {
        "vocabulary": ["meaningCn"],
        "grammar": ["meaningCn"],
        "example": ["meaningCn", "promptCn"],
    }.get(content_kind, [])

    total = len(items) * len(fields)
    unlocalized = 0
    for item in items:
        for field in fields:
            if looks_unlocalized(item.get(field)):
                unlocalized += 1
    return total, unlocalized


def main():
    for path in sorted(PACKS_DIR.glob("*.json")):
        total, unlocalized = check_pack(path)
        if total == 0:
            continue
        localized = total - unlocalized
        print(f"{path.name}: localized={localized} unlocalized={unlocalized} total={total}")


if __name__ == "__main__":
    main()
