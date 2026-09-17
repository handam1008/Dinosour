import collections
import json
import math
import statistics
import sys
from pathlib import Path

root = Path(sys.argv[1])
results = []

def check(condition, label):
    if not condition:
        raise AssertionError(label)
    results.append(label)

client = json.loads((root / "client.trace.1.json").read_text())
frames = client["frames"]
direction = math.copysign(1, frames[-1]["view"]["x"] - frames[0]["view"]["x"])
steps = [direction * (b["view"]["x"] - a["view"]["x"]) for a, b in zip(frames, frames[1:])]
check(sum(steps) > 2, "Owner continuously travels more than two units")
check(min(steps) >= -0.005, "No owner movement reversals")
check(max(frame["correction"] for frame in frames) < 0.35, "Steady movement reconciliation stays below 0.35 units")
for peer in ("host", "client"):
    movement = json.loads((root / (peer + ".trace.1.json")).read_text())
    for field in ("view", "otherView"):
        samples = movement["frames"]
        values = [f[field]["x"] for f in samples]
        sign = math.copysign(1, values[-1] - values[0])
        changes = [sign * (b-a) for a,b in zip(values,values[1:])]
        check(abs(values[-1]-values[0]) > 1, peer + " " + field + ": sustained movement observed")
        check(min(changes) >= -0.01, peer + " " + field + ": no movement backtracking")
summary = {"build": client["build"], "movementMaxCorrection": max(f["correction"] for f in frames), "peers": {}}
for peer in ("host", "client"):
    trace = json.loads((root / (peer + ".trace.2.json")).read_text())
    groups = collections.defaultdict(list)
    deltas = []
    unique = True
    for frame in trace["frames"]:
        deltas.append(frame["delta"])
        keys = [(s["caster"], s["action"], s["part"]) for s in frame["shots"]]
        unique = unique and len(keys) == len(set(keys))
        for shot in frame["shots"]:
            groups[(shot["caster"], shot["action"], shot["part"])].append((frame["time"], shot))
    check(unique, peer + ": one visible object per logical shot")
    shots = []
    own = 0
    for key, samples in groups.items():
        steps = [(b[1]["position"]["x"] - a[1]["position"]["x"], b[0] - a[0], a[1], b[1]) for a, b in zip(samples, samples[1:])]
        if len(steps) < 3:
            continue
        check(min(s[0] for s in steps) >= -0.005, f"{peer} {key}: no backtracking")
        handoffs = [s for s in steps if s[2]["preview"] and not s[3]["preview"]]
        if peer == "client" and key[0] == trace["owner"]:
            own += 1
            check(len(handoffs) == 1, f"{peer} {key}: exactly one seamless handoff")
            check(handoffs[0][0] <= 20 * handoffs[0][1] + 0.02, f"{peer} {key}: handoff bounded by flight speed")
        active = [s for i, s in enumerate(steps) if i > 2 and not s[2].get("blocked", False) and not s[3].get("blocked", False)]
        stalls = sum(s[0] < 0.0001 for s in active)
        check(stalls == 0, f"{peer} {key}: no midflight stalls outside terrain contact ({stalls})")
        shots.append({"caster": key[0], "action": key[1], "part": key[2], "frames": len(samples), "minForwardStep": min(s[0] for s in steps), "handoffs": len(handoffs), "midflightStalls": stalls})
    if peer == "client":
        check(own == 3, "Three guest casts checked frame by frame")
    summary["peers"][peer] = {"frames": len(trace["frames"]), "frameMsP95": sorted(deltas)[int(len(deltas)*0.95)]*1000, "shots": shots}
summary["checks"] = len(results)
(root / "trajectory-results.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
print(json.dumps(summary, indent=2))
