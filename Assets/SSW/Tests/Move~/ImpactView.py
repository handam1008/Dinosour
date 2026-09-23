import json
import pathlib
import sys


root = pathlib.Path(sys.argv[1])
trace = json.loads((root / "client.trace.2.json").read_text(encoding="utf-8-sig"))
frames = trace["frames"]
owner = trace["owner"]
target_x = frames[0]["otherView"]["x"]
shots = [
    (frame, shot)
    for frame in frames
    for shot in frame["shots"]
    if shot["caster"] == owner
]
if not shots:
    raise AssertionError("No visible client attack was recorded")
for frame in frames:
    keys = [(s["caster"], s["action"], s["part"]) for s in frame["shots"]]
    if len(keys) != len(set(keys)):
        raise AssertionError("An attack has duplicate visible projectiles")
damage = frames[0]["otherHp"] - frames[-1]["otherHp"]
overrun = max(shot["position"]["x"] - target_x for _, shot in shots)
blocked = [(frame, shot) for frame, shot in shots if shot["blocked"]]
result = {
    "build": trace["build"],
    "damage": damage,
    "targetOverrun": overrun,
    "blockedFrames": len(blocked),
    "visibleFrames": len(shots),
    "remaining": len(frames[-1]["shots"]),
}
(root / "impact-view.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps(result))
if damage <= 0 or overrun > 0.3 or not blocked or result["remaining"]:
    raise AssertionError("A confirmed close hit passes through the visible target or leaves a projectile behind")
