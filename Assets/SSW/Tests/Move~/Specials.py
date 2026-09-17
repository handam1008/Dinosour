import collections
import json
import sys
from pathlib import Path

root = Path(sys.argv[1])
state = json.loads((root / "final-host.json").read_text(encoding="utf-8-sig"))
mage = next(p for p in state["players"] if p["job"] == "Magician")
witch = next(p for p in state["players"] if p["job"] == "Witch")
result = {}
for peer in ("host", "client"):
    trace = json.loads((root / (peer + ".trace.3.json")).read_text())
    shots = collections.defaultdict(list)
    for frame in trace["frames"]:
        for shot in frame["shots"]:
            shots[(shot["caster"], shot["action"], shot["part"])].append(shot)
    cards = [v for k, v in shots.items() if k[0] == mage["objectId"]]
    assert len(cards) == 1, "One delayed return card on " + peer
    card = cards[0]
    backwards = sum(b["position"]["x"] < a["position"]["x"] - 0.001 for a, b in zip(card, card[1:]))
    assert backwards > 5, "Return card must leave the wall and fly back on " + peer
    assert card[-1]["position"]["x"] >= mage["position"]["x"] - 0.4, "Return card must not overshoot its caster on " + peer
    potions = [v for k, v in shots.items() if k[0] == witch["objectId"]]
    assert len(potions) == 3, "Three independently tracked potion parts on " + peer
    bounces = sum(any(a["velocity"]["y"] < -0.1 and b["velocity"]["y"] > 0.1 for a, b in zip(v, v[1:])) for v in potions)
    assert bounces > 0, "At least one potion bounce is visible on " + peer
    result[peer] = {"returnFrames": backwards, "returnEndX": card[-1]["position"]["x"], "potionParts": len(potions), "bouncingParts": bounces}
(root / "special-results.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps(result, indent=2))
