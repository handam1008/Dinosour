import json
import pathlib
import sys


root = pathlib.Path(sys.argv[1])
result = {"views": {}}
for peer in ("host", "client"):
    trace = json.loads((root / (peer + ".trace.1.json")).read_text(encoding="utf-8-sig"))
    state = json.loads((root / ("final-" + peer + ".json")).read_text(encoding="utf-8-sig"))
    result["build"] = trace["build"]
    frames = trace["frames"]
    for field in ("view", "otherView"):
        speed = next(player["speed"] for player in state["players"] if player["owner"] == (field == "view"))
        steps = [
            (b["time"] - a["time"], abs(b[field]["x"] - a[field]["x"]))
            for a, b in zip(frames, frames[1:])
        ]
        moving = [index for index, (_, distance) in enumerate(steps) if distance > 0.002]
        if not moving:
            raise AssertionError("No movement was captured for " + peer + " " + field)
        steps = steps[moving[0]:moving[-1] + 1]
        speeds = sorted(distance / delta for delta, distance in steps if delta >= 0.004)
        pause = longest = 0
        for delta, distance in steps:
            pause = pause + delta if distance < 0.0001 else 0
            longest = max(longest, pause)
        result["views"][peer + " " + field] = {
            "distance": sum(distance for _, distance in steps),
            "speed": speed,
            "speedP95": speeds[int(len(speeds) * 0.95)],
            "longestPauseMs": longest * 1000,
        }
(root / "motion-quality.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps(result))
for label, view in result["views"].items():
    if view["distance"] < 2 or view["speedP95"] > view["speed"] * 1.3 or view["longestPauseMs"] > 120:
        raise AssertionError(label + ": continuous movement freezes or catches up at an excessive speed")
