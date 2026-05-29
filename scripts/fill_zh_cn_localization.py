import argparse
import json
import time
import urllib.parse
import urllib.request
from http.client import RemoteDisconnected
from urllib.error import HTTPError, URLError
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKS_DIR = ROOT / "Resources" / "Content" / "packs"
LOCALIZATION_DIR = ROOT / "Resources" / "Content" / "localization" / "zh-cn"
PACK_KINDS = ("vocabulary", "grammar", "examples")
JLPT_LEVELS = ("n5", "n4", "n3", "n2", "n1")


TRANSLATABLE_FIELDS_BY_KIND = {
    "vocabulary": ["meaningCn"],
    "grammar": ["meaningCn", "formation", "usageNote"],
    "example": ["meaningCn", "promptCn"],
}


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def translate_text(text):
    query = urllib.parse.urlencode(
        {
            "client": "gtx",
            "sl": "en",
            "tl": "zh-CN",
            "dt": "t",
            "q": text,
        }
    )
    url = "https://translate.googleapis.com/translate_a/single?" + query
    request = urllib.request.Request(url, headers={"User-Agent": "NihongoToastFishContentTool/0.1"})
    last_error = None
    for attempt in range(5):
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                payload = json.loads(response.read().decode("utf-8"))
            return "".join(part[0] for part in payload[0] if part and part[0]).strip()
        except (HTTPError, URLError, TimeoutError, RemoteDisconnected) as exc:
            last_error = exc
            time.sleep(2 ** attempt)
    raise last_error


def ensure_overlay(pack_id):
    path = LOCALIZATION_DIR / f"{pack_id}.json"
    if path.exists():
        data = load_json(path)
    else:
        data = {
            "locale": "zh-CN",
            "packId": pack_id,
            "translationSource": {
                "sourceId": "nihongo-toastfish-zh-cn-machine-assisted",
                "name": "Nihongo ToastFish machine-assisted Simplified Chinese localization",
                "notes": "Machine-assisted first-party draft localization. Requires human review.",
            },
            "items": {},
        }
    data.setdefault("items", {})
    return path, data


def fill_pack(pack_path, limit):
    pack = load_json(pack_path)
    metadata = pack.get("pack") or {}
    pack_id = metadata.get("packId")
    content_kind = metadata.get("contentKind")
    fields = TRANSLATABLE_FIELDS_BY_KIND.get(content_kind, [])
    if not pack_id or not fields:
        return 0

    overlay_path, overlay = ensure_overlay(pack_id)
    items = overlay["items"]
    translated = 0

    for item in pack.get("items") or []:
        content_id = item.get("contentId")
        if not content_id:
            continue
        localized = items.setdefault(content_id, {})
        for field in fields:
            if limit is not None and translated >= limit:
                write_json(overlay_path, overlay)
                return translated
            if localized.get(field):
                continue
            source_text = item.get(field)
            if not source_text:
                continue
            try:
                localized[field] = translate_text(source_text)
            except Exception:
                write_json(overlay_path, overlay)
                raise
            translated += 1
            write_json(overlay_path, overlay)
            if translated % 100 == 0:
                print(f"{pack_path.name}: progress={translated}", flush=True)
            time.sleep(0.35)

    write_json(overlay_path, overlay)
    return translated


def main():
    parser = argparse.ArgumentParser(
        description="Fill Simplified Chinese localization overlays from existing pack fields."
    )
    parser.add_argument(
        "--level",
        default="n5",
        help="JLPT level suffix, for example n5, or all for n5 through n1.",
    )
    parser.add_argument("--limit", type=int)
    args = parser.parse_args()

    total = 0
    levels = JLPT_LEVELS if args.level.lower() == "all" else (args.level.lower(),)
    for level in levels:
        for kind in PACK_KINDS:
            pack_path = PACKS_DIR / f"{kind}-{level}.json"
            if not pack_path.exists():
                continue
            remaining = None if args.limit is None else max(args.limit - total, 0)
            translated = fill_pack(pack_path, remaining)
            total += translated
            print(f"{pack_path.name}: TranslatedFields={translated}", flush=True)
            if args.limit is not None and total >= args.limit:
                print(f"TranslatedFields={total}", flush=True)
                return
    print(f"TranslatedFields={total}", flush=True)


if __name__ == "__main__":
    main()
