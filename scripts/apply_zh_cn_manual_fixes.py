import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
LOCALIZATION_DIR = ROOT / "Resources" / "Content" / "localization" / "zh-cn"


FIXES = {
    "builtin-examples-n2": {
        "hanabira-example-n2-0010": {
            "meaningCn": "谈话变得有点冗长了。那么，请多告诉我一些关于纪美子的事。",
            "promptCn": "谈话变得有点冗长了。那么，请多告诉我一些关于纪美子的事。",
        },
        "hanabira-example-n2-0090": {
            "meaningCn": "他很聪明，而且也擅长运动。",
        },
        "hanabira-example-n2-0120": {
            "meaningCn": "那个购物中心聚集了包括某服装品牌在内的许多品牌。",
            "promptCn": "那个购物中心聚集了包括某服装品牌在内的许多品牌。",
        },
        "hanabira-example-n2-0122": {
            "meaningCn": "这场音乐会预计将邀请包括防弹少年团在内的流行艺人参加。",
            "promptCn": "这场音乐会预计将邀请包括防弹少年团在内的流行艺人参加。",
        },
    },
    "builtin-examples-n4": {
        "hanabira-example-n4-0364": {
            "meaningCn": "那个男人（被称为“他”）真的很有趣。",
            "promptCn": "那个男人（被称为“他”）真的很有趣。",
        },
    },
    "builtin-grammar-n1": {
        "hanabira-grammar-n1-0110": {
            "meaningCn": "表示“作为、伴随、由……开始”等含义，用来说明某事的结束或变化点。",
        },
        "hanabira-grammar-n1-0231": {
            "meaningCn": "“ぶる”的て形，表示“装作……、摆出……的样子”。",
        },
    },
    "builtin-grammar-n2": {
        "hanabira-grammar-n2-0178": {
            "meaningCn": "用于条件语境，表示“既然……、只要……”。",
        },
    },
    "builtin-grammar-n3": {
        "hanabira-grammar-n3-0021": {
            "meaningCn": "表示某种性质或状态的程度，相当于“……程度、……性”。",
        },
        "hanabira-grammar-n3-0119": {
            "meaningCn": "表示被动动作，相当于“被……、受到……”。",
        },
    },
    "builtin-grammar-n4": {
        "hanabira-grammar-n4-0006": {
            "meaningCn": "表示变化或决定；与な形容词一起表示“变得……、使其……”。",
        },
        "hanabira-grammar-n4-0115": {
            "meaningCn": "表示动作或期限在某个时间之前完成。",
        },
    },
    "builtin-grammar-n5": {
        "hanabira-grammar-n5-0031": {
            "meaningCn": "连接名词时表示“和、与、同……一起”。",
        },
        "hanabira-grammar-n5-0125": {
            "meaningCn": "表示所属、并列或描述，相当于“……的”。",
        },
    },
    "builtin-vocabulary-n1": {
        "elzup-vocab-n1-0339": {"meaningCn": "在路上；途中"},
        "elzup-vocab-n1-0505": {"meaningCn": "神经症"},
        "elzup-vocab-n1-0876": {"meaningCn": "病历；诊疗记录"},
        "elzup-vocab-n1-1258": {"meaningCn": "铝"},
        "elzup-vocab-n1-1838": {"meaningCn": "草图；素描"},
        "elzup-vocab-n1-1996": {"meaningCn": "从头到尾；一贯"},
        "elzup-vocab-n1-2155": {"meaningCn": "改革；改造"},
        "elzup-vocab-n1-2462": {"meaningCn": "家伙；小子（粗俗说法）"},
    },
    "builtin-vocabulary-n2": {
        "elzup-vocab-n2-0062": {"meaningCn": "尿；小便（口语）"},
        "elzup-vocab-n2-0172": {"meaningCn": "背负；担负"},
        "elzup-vocab-n2-0417": {"meaningCn": "咀嚼；咬"},
        "elzup-vocab-n2-0455": {"meaningCn": "扑克牌；纸牌"},
        "elzup-vocab-n2-0705": {"meaningCn": "竞赛；比赛"},
        "elzup-vocab-n2-1042": {"meaningCn": "往返；往返票"},
        "elzup-vocab-n2-1220": {"meaningCn": "进入；插入；穿上"},
        "elzup-vocab-n2-1249": {"meaningCn": "阻止；抑制"},
        "elzup-vocab-n2-1317": {"meaningCn": "塞满；堵住；关闭"},
        "elzup-vocab-n2-1365": {"meaningCn": "并排；并行；同时"},
        "elzup-vocab-n2-1562": {"meaningCn": "在夕阳中；夕阳"},
        "elzup-vocab-n2-1717": {"meaningCn": "主题；题目"},
    },
    "builtin-vocabulary-n3": {
        "elzup-vocab-n3-0346": {"meaningCn": "出现；变得可见；表达"},
        "elzup-vocab-n3-0476": {"meaningCn": "能量；能源"},
        "elzup-vocab-n3-0666": {"meaningCn": "空间；间隔"},
        "elzup-vocab-n3-0780": {"meaningCn": "偶然；碰巧"},
        "elzup-vocab-n3-1014": {"meaningCn": "错误；失误；失败；小姐"},
        "elzup-vocab-n3-1079": {"meaningCn": "基于；建立在……之上"},
        "elzup-vocab-n3-1212": {"meaningCn": "问卷调查；调查"},
        "elzup-vocab-n3-1400": {"meaningCn": "帮助；拯救"},
        "elzup-vocab-n3-1603": {"meaningCn": "独立；自立"},
        "elzup-vocab-n3-1679": {"meaningCn": "不擅长；弱于；不喜欢"},
        "elzup-vocab-n3-1854": {"meaningCn": "否定前缀；非……；不……"},
        "elzup-vocab-n3-1855": {"meaningCn": "否定前缀；非……；不……"},
        "elzup-vocab-n3-2126": {"meaningCn": "碰巧看到；注意到；发现"},
    },
    "builtin-vocabulary-n4": {
        "elzup-vocab-n4-0029": {"meaningCn": "摩托车"},
        "elzup-vocab-n4-0040": {"meaningCn": "府上；贵宅；别人家"},
        "elzup-vocab-n4-0457": {"meaningCn": "并且；因此；因为"},
        "elzup-vocab-n4-0494": {"meaningCn": "给予；授予（尊敬语）"},
        "elzup-vocab-n4-0648": {"meaningCn": "第……；序数"},
    },
    "builtin-vocabulary-n5": {
        "elzup-vocab-n5-0212": {"meaningCn": "公斤；千克"},
        "elzup-vocab-n5-0218": {"meaningCn": "请为我做……"},
        "elzup-vocab-n5-0395": {"meaningCn": "谁"},
        "elzup-vocab-n5-0409": {"meaningCn": "期间；同时"},
        "elzup-vocab-n5-0464": {"meaningCn": "谁"},
        "elzup-vocab-n5-0551": {"meaningCn": "第一；最好的"},
    },
}


def main() -> int:
    updated = 0
    for pack_id, items in FIXES.items():
        path = LOCALIZATION_DIR / f"{pack_id}.json"
        data = json.loads(path.read_text(encoding="utf-8"))
        overlay_items = data.setdefault("items", {})
        for content_id, fields in items.items():
            overlay_items.setdefault(content_id, {}).update(fields)
            updated += len(fields)
        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"ManualFixFields={updated}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
