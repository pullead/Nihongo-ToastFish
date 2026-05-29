import unittest
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_builtin_jlpt_content as generator


class LocalizationOverlayTests(unittest.TestCase):
    def test_apply_item_localization_overrides_translation_fields(self):
        item = {
            "contentId": "sample-001",
            "meaningCn": "to meet, to see",
            "promptCn": "to meet, to see",
        }
        localized = {
            "sample-001": {
                "meaningCn": "见面；看见",
                "promptCn": "见面；看见",
            }
        }

        changed = generator.apply_item_localization(item, localized)

        self.assertTrue(changed)
        self.assertEqual("见面；看见", item["meaningCn"])
        self.assertEqual("见面；看见", item["promptCn"])

    def test_apply_item_localization_keeps_source_text_when_missing(self):
        item = {
            "contentId": "sample-001",
            "meaningCn": "blue",
        }

        changed = generator.apply_item_localization(item, {})

        self.assertFalse(changed)
        self.assertEqual("blue", item["meaningCn"])


if __name__ == "__main__":
    unittest.main()
