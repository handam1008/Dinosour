import argparse
import json
import pathlib
import subprocess
import time


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--run", required=True)
    parser.add_argument("--build", default="Builds/Local/Game.exe")
    parser.add_argument("--repeat", type=int, default=8)
    parser.add_argument("--delay", type=int, default=180)
    parser.add_argument("--jitter", type=int, default=150)
    parser.add_argument("--scenario", default="cadence", choices=("cadence", "impact", "motion", "stall", "turns", "self", "stock", "burst", "effects"))
    parser.add_argument("--step", action="store_true")
    parser.add_argument("--loss", type=int, default=3)
    parser.add_argument("--host-fps", type=int, default=60)
    parser.add_argument("--client-fps", type=int, default=60)
    parser.add_argument("--host-job", default="Magician", choices=("Magician", "Witch"))
    parser.add_argument("--client-job", default="Magician", choices=("Magician", "Witch"))
    parser.add_argument("--port", type=int, default=7797)
    parser.add_argument("--expect-pass", action="store_true")
    parser.add_argument("--augment", type=int, default=-1)
    args = parser.parse_args()
    project = pathlib.Path.cwd()
    root = project / "Logs" / "Combat" / args.run
    root.mkdir(parents=True, exist_ok=False)
    sequence = {"host": 0, "client": 0}
    processes = []
    records = []

    def read(peer):
        path = root / (peer + ".json")
        if not path.exists():
            return None
        for attempt in range(25):
            try:
                return json.loads(path.read_text(encoding="utf-8-sig"))
            except (OSError, ValueError):
                time.sleep(0.005)
        return None

    def own(peer):
        state = read(peer)
        return next((p for p in state["players"] if p["owner"]), None) if state else None

    def wait(label, condition, timeout=20):
        until = time.monotonic() + timeout
        while time.monotonic() < until:
            if condition():
                return
            for peer in sequence:
                state = read(peer)
                if state and state.get("error"):
                    raise RuntimeError(state["error"])
            time.sleep(0.015)
        raise TimeoutError(label)

    def send(peer, op, value=0, x=0, y=0):
        sequence[peer] += 1
        command = dict(seq=sequence[peer], op=op, value=value, x=x, y=y)
        until = time.monotonic() + 2
        while True:
            try:
                (root / (peer + ".cmd.json")).write_text(json.dumps(command), encoding="utf-8")
                break
            except OSError:
                if time.monotonic() > until:
                    raise
                time.sleep(0.01)
        if op != "quit":
            wait(peer + " " + op, lambda: read(peer) and read(peer)["seq"] >= sequence[peer], 8)

    def save():
        (root / "shots.json").write_text(json.dumps(records, indent=2), encoding="utf-8")

    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = subprocess.SW_HIDE
    try:
        for peer in sequence:
            processes.append(subprocess.Popen([
                str(project / args.build), "--net-mode", peer, "--net-job", args.host_job if peer == "host" else args.client_job,
                "--net-port", str(args.port), "--net-name", peer, "--net-probe", str(root / peer),
                "-logFile", str(root / (peer + ".log")), "-screen-fullscreen", "0"
            ], cwd=project, startupinfo=startup))
        wait("draft", lambda: all(read(p) and read(p)["phase"] == "Draft" for p in sequence), 60)
        for peer in sequence:
            send(peer, "fps", args.host_fps if peer == "host" else args.client_fps)
            offer = own(peer)["offer"]
            slot = next(i for i, key in enumerate(("x", "y", "z")) if offer[key] != 16)
            send(peer, "choose", slot)
        wait("playing", lambda: all(read(p)["phase"] == "Playing" for p in sequence))
        if args.augment >= 0:
            send("host", "grant", args.augment, 0)
            send("host", "grant", args.augment, 1)
        send("host", "warp", 0, 13, 0)
        send("host", "warp", 1, -8, 0)
        time.sleep(1)
        for peer in sequence:
            send(peer, "lag", args.delay, args.jitter, args.loss)
        time.sleep(2)
        if args.scenario == "burst":
            if args.client_job != "Witch":
                raise ValueError("Burst requires a Witch client")
            send("host", "grant", 0, 1)
            send("host", "warp", 1, -18, 0)
            for index in range(args.repeat):
                time.sleep(4)
                before = own("client")
                releases = []
                for part in range(2):
                    wait("local stock", lambda: own("client")["heldView"] >= 0)
                    send("client", "press", 0, 0, 1)
                    releases.append(own("client"))
                    if part == 0:
                        time.sleep(0.18)
                actions = {r["action"] for r in releases}
                wait("two authoritative potions", lambda: actions <= {p["action"] for p in read("host")["shots"] if p["type"] == "potion"})
                server = read("host")
                actual = {p["action"]: p["kind"] for p in server["shots"] if p["type"] == "potion" and p["action"] in actions}
                wait("burst acknowledgement", lambda: own("client")["confirmed"] >= max(actions))
                wait("two preview handoffs", lambda: own("client")["matches"] >= before["matches"] + 2)
                after = own("client")
                pairs = [(r["shotKind"], actual[r["action"]]) for r in releases]
                result = dict(pairs=pairs, rejected=after["rejected"]-before["rejected"], matches=after["matches"]-before["matches"])
                records.append(dict(before=before, releases=releases, server=server, after=after, result=result))
                save()
                print(json.dumps(dict(burst=index, **result)), flush=True)
                if args.expect_pass and (any(a != b for a,b in pairs) or result["rejected"] or result["matches"] != 2):
                    raise AssertionError("Rapid potion pair changed kind, duplicated, or lost its handoff")
        elif args.scenario == "stock":
            if args.client_job != "Witch":
                raise ValueError("Stock boundary requires a Witch client")
            send("host", "warp", 1, -18, 0)
            time.sleep(2)
            for index in range(args.repeat):
                wait("stock boundary", lambda: own("client")["heldView"] >= 0 and own("client")["action"] <= own("client")["confirmed"] and own("client")["potion"] != next(p for p in read("host")["players"] if not p["owner"])["potion"], 25)
                before = own("client")
                send("client", "press", 0, 0, 1)
                release = own("client")
                action = release["action"]
                wait("authoritative potion", lambda: any(p["type"] == "potion" and p["action"] == action for p in read("host")["shots"]))
                server = read("host")
                wait("shot acknowledgement", lambda: own("client")["confirmed"] >= action)
                authority = next(p for p in server["players"] if not p["owner"])
                shots = [p for p in server["shots"] if p["type"] == "potion" and p["action"] == action and p["caster"] == authority["objectId"]]
                predicted = release.get("shotKind", before["heldView"])
                actual = authority.get("shotKind", shots[0]["kind"] if shots else -1)
                rejected = own("client")["rejected"] - before["rejected"]
                records.append(dict(before=before, release=release, server=server, predicted=predicted, actual=actual, rejected=rejected))
                save()
                print(json.dumps(dict(shot=index, action=action, predicted=predicted, actual=actual, rejected=rejected)), flush=True)
                if args.expect_pass and (predicted != actual or rejected):
                    raise AssertionError("The potion at the stock boundary changed kind or was rejected")
                time.sleep(0.2)
            result = dict(result="stock", shots=len(records), mismatches=sum(r["predicted"] != r["actual"] for r in records), rejected=sum(r["rejected"] for r in records), root=str(root))
            (root / "results.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
            print(json.dumps(result), flush=True)
        elif args.scenario == "self":
            if args.client_job != "Witch":
                raise ValueError("Self splash requires a Witch client")
            send("host", "warp", 1, -8, 0)
            wait("grounded", lambda: all(p["grounded"] for p in read("host")["players"]))
            send("host", "grant", 21, 1)
            send("host", "remote", 40)
            time.sleep(1.5)
            wait("held healing potion", lambda: own("client")["heldView"] == 1, 25)
            send("client", "trace", 7, 12)
            send("host", "trace", 7, 12)
            before = read("host")
            send("client", "press", 0, 0, -1)
            action = own("client")["action"]
            time.sleep(12.5)
            after = read("host")
            client = read("client")
            health_before = next(p["hp"] for p in before["players"] if not p["owner"])
            health_after = next(p["hp"] for p in after["players"] if not p["owner"])
            turns = {}
            for peer in sequence:
                trace = json.loads((root / (peer + ".trace.7.json")).read_text(encoding="utf-8-sig"))
                shots = [s for f in trace["frames"] for s in f["shots"] if s["action"] == action]
                turns[peer] = max((s["turn"] for s in shots), default=0)
            records.append(dict(before=before, after=after, client=client, action=action, turns=turns))
            save()
            result = dict(result="self", healed=health_after-health_before, turns=turns,
                          healthSynced=own("client")["hp"] == health_after,
                          previews=own("client")["previews"], root=str(root))
            (root / "results.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
            print(json.dumps(result), flush=True)
            if args.expect_pass and (result["healed"] <= 0 or not result["healthSynced"] or result["previews"] or any(turns.values())):
                raise AssertionError("Self healing splash or bounce termination changed")
        elif args.scenario == "impact":
            send("host", "warp", 0, -6, 0)
            send("host", "warp", 1, -8, 0)
            wait("grounded", lambda: all(p["grounded"] for p in read("host")["players"]))
            time.sleep(1.5)
            if args.client_job == "Witch":
                wait("damage potion", lambda: own("client")["heldView"] == 0, 30)
            send("client", "trace", 2, 5)
            send("host", "trace", 2, 5)
            before = read("host")
            if args.client_job == "Magician":
                send("client", "press", 0, 1, 0)
                time.sleep(0.15)
            send("host", "jump")
            send("client", "release" if args.client_job == "Magician" else "press", 0, 1, 0)
            release = read("client")
            time.sleep(5.5)
            after = read("host")
            records.append(dict(before=before, release=release, after=after, client=read("client")))
            save()
            health_before = next(p["hp"] for p in before["players"] if p["owner"])
            health_after = next(p["hp"] for p in after["players"] if p["owner"])
            print(json.dumps(dict(result="impact", damage=health_before-health_after, root=str(root))), flush=True)
            if args.expect_pass and health_after >= health_before:
                raise AssertionError("Visible close-range shot missed the rewound target")
        elif args.scenario == "motion":
            for index in range(args.repeat):
                send("host", "warp", 1, -8, 0)
                time.sleep(1.5)
                wait("ready", lambda: own("client")["readyIn"] <= 0)
                send("client", "move", 0, 1 if index % 2 == 0 else -1, 0)
                time.sleep(0.25)
                send("client", "jump")
                time.sleep(0.15)
                send("client", "press", 0, 0, 1)
                time.sleep(0.12)
                send("client", "release", 0, 0, 1)
                release = own("client")
                send("client", "move")
                action = release["action"]
                wait("ack", lambda: own("client")["confirmed"] >= action)
                if args.expect_pass:
                    wait("authoritative snapshot", lambda: next(p for p in read("host")["players"] if not p["owner"])["shotTick"] >= release["shotTick"])
                server = next(p for p in read("host")["players"] if not p["owner"])
                distance = sum((release["shotOrigin"][k] - server["shotOrigin"][k]) ** 2 for k in ("x", "y")) ** 0.5
                records.append(dict(release=release, server=server, distance=distance))
                save()
                print(json.dumps(dict(shot=index, originError=distance, tick=release["shotTick"], serverTick=server["shotTick"])), flush=True)
                if args.expect_pass and (distance > 0.15 or release["shotTick"] != server["shotTick"]):
                    raise AssertionError("Moving shot origin differs from its authoritative input frame")
        elif args.scenario == "effects":
            if args.client_job != "Magician":
                raise ValueError("Controlled speed effects require a Magician client")
            for index in range(args.repeat):
                effect = "slow" if index % 2 == 0 else "haste"
                direction = 1 if index % 4 in (0, 3) else -1
                send("host", "warp", 1, -18 if direction > 0 else 10, 0)
                wait("effect position settled", lambda: own("client")["grounded"])
                time.sleep(1.5)
                send("client", "metrics")
                trace_id = 8 + index
                send("client", "trace", trace_id, 6)
                send("host", "trace", trace_id, 6)
                send("client", "move", 0, direction * own("client")["side"], 0)
                time.sleep(0.5)
                begin = read("client")["time"]
                send("host", effect, 0, 0.5, 1.2)
                wait("effect reaches moving client", lambda: own("client")["speed"] < 4 if effect == "slow" else own("client")["speed"] > 10)
                send("client", "press", 0, 0, 1)
                time.sleep(0.1)
                send("client", "release", 0, 0, 1)
                samples = []
                release = read("client")
                until = time.monotonic() + 1.8
                while time.monotonic() < until:
                    samples.append(dict(client=read("client"), host=read("host")))
                    time.sleep(0.025)
                end = read("client")["time"]
                send("client", "move")
                time.sleep(2)
                trace_path = root / ("client.trace." + str(trace_id) + ".json")
                wait("effect trace", trace_path.exists, 10)
                trace = json.loads(trace_path.read_text(encoding="utf-8-sig"))
                steps = [(b["view"]["x"]-a["view"]["x"], b["time"]-a["time"])
                         for a,b in zip(trace["frames"], trace["frames"][1:])
                         if begin <= a["time"] and b["time"] < end-0.15 and a["epoch"] == b["epoch"]]
                owner = own("client")
                server = next(p for p in read("host")["players"] if not p["owner"])
                rates = [next(p for p in r["client"]["players"] if p["owner"])["speed"] for r in samples]
                caster = next(p for p in release["players"] if p["owner"])
                preview = next(s for f in trace["frames"] for s in f["shots"] if s["preview"] and s["caster"] == trace["owner"] and s["action"] == caster["action"])
                spawn_error = abs(preview["position"]["x"]-caster["viewPosition"]["x"])
                body_error = sum((owner["position"][k]-server["position"][k])**2 for k in ("x","y"))**0.5
                view_error = sum((owner["viewPosition"][k]-owner["position"][k])**2 for k in ("x","y"))**0.5
                result = dict(effect=effect, minStep=min(dx*direction for dx,dt in steps),
                              maxFrameStep=max(abs(dx) for dx,dt in steps),
                              maxInstantExcess=max(abs(dx)-32*dt for dx,dt in steps),
                              maxCorrection=owner["maxCorrection"], minSpeed=min(rates), maxSpeed=max(rates),
                              bodyError=body_error, viewError=view_error, spawnError=spawn_error, matches=owner["matches"], rejected=owner["rejected"])
                records.append(dict(result=result, release=release, samples=samples))
                save()
                print(json.dumps(result), flush=True)
                if args.expect_pass and (result["minStep"] < -0.002 or result["maxInstantExcess"] > 0.04 or
                    body_error > 0.1 or view_error > 0.02 or spawn_error > 0.1 or owner["maxCorrection"] < 0.25 or not (min(rates) < 4 if effect == "slow" else max(rates) > 10) or owner["rejected"]):
                    raise AssertionError("Speed effect snaps, reverses the view, or does not converge")
            (root / "results.json").write_text(json.dumps([r["result"] for r in records], indent=2), encoding="utf-8")
        elif args.scenario == "turns":
            send("client", "metrics")
            send("client", "trace", 6, 30)
            send("host", "trace", 6, 30)
            start = time.monotonic()
            direction = 0
            jump_at = 0
            shot_at = {"host": 0, "client": 0}
            stage = -1
            while time.monotonic() - start < 27:
                elapsed = time.monotonic() - start
                next_stage = min(3, int(elapsed // 6))
                if next_stage != stage:
                    stage = next_stage
                    delay = (20, 220, 20, 20)[stage]
                    for peer in sequence:
                        send(peer, "lag", delay, 35 if stage == 1 else 0, 2 if stage == 1 else 0)
                turn = int(elapsed / 0.55) % 2 * 2 - 1
                if turn != direction:
                    direction = turn
                    send("client", "move", 0, turn, 0)
                    send("host", "move", 0, turn, 0)
                if elapsed >= jump_at:
                    send("client", "jump")
                    send("host", "jump")
                    jump_at = elapsed + 1.1
                for peer in sequence:
                    job = args.host_job if peer == "host" else args.client_job
                    if elapsed < shot_at[peer] or own(peer)["readyIn"] > 0:
                        continue
                    if job == "Magician":
                        send(peer, "press", 0, 0, 1)
                        time.sleep(0.1)
                        send(peer, "release", 0, 0, 1)
                        shot_at[peer] = elapsed + 0.2
                    elif own(peer)["potion"] >= 0:
                        send(peer, "press", 0, 1 if peer == "host" else -1, 1)
                        shot_at[peer] = elapsed + 1.6
                records.append(dict(elapsed=elapsed, stage=stage, host=read("host"), client=read("client")))
                time.sleep(0.025)
            send("client", "move")
            send("host", "move")
            time.sleep(4)
            save()
            backlogs = [next(p for p in r["host"]["players"] if not p["owner"])["buffered"] for r in records if r["elapsed"] > 22]
            owner = own("client")
            trace = json.loads((root / "client.trace.6.json").read_text(encoding="utf-8-sig"))
            steps = [(abs(b["view"]["x"]-a["view"]["x"]), b["time"]-a["time"])
                     for a,b in zip(trace["frames"], trace["frames"][1:]) if a["epoch"] == b["epoch"]]
            result = dict(result="turns", maxCorrection=owner["maxCorrection"], endingBacklog=max(backlogs),
                          maxViewStep=max(dx for dx,dt in steps), maxInstantExcess=max(dx-32*dt for dx,dt in steps),
                          matches=owner["matches"], rejected=owner["rejected"], root=str(root))
            (root / "results.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
            print(json.dumps(result), flush=True)
            if args.expect_pass and (result["endingBacklog"] > 6 or result["maxInstantExcess"] > 0.04 or owner["rejected"] or
                (args.host_job == args.client_job == "Magician" and owner["maxCorrection"] > 0.5)):
                raise AssertionError("Rapid turns and firing keep excessive delay or cause large position corrections")
        elif args.scenario == "stall":
            before = read("host")
            send("host", "stall", 900)
            time.sleep(1.5)
            after = read("host")
            error = abs(after["physicsTime"] - after["serverTime"])
            records.append(dict(before=before, after=after, clockError=error))
            save()
            wait("ready", lambda: own("client")["readyIn"] <= 0)
            send("client", "press", 0, 1, 0)
            time.sleep(0.1)
            send("client", "release", 0, 1, 0)
            wait("shot after stall", lambda: own("client")["matches"] >= 1)
            if args.expect_pass and error > 0.12:
                raise AssertionError("Physics clock stayed behind after a host frame stall")
            print(json.dumps(dict(result="stall", clockError=error, root=str(root))), flush=True)
        else:
            send("client", "trace", 1, 30)
            send("host", "trace", 1, 30)
            for index in range(args.repeat):
                if args.step and index == 1:
                    send("host", "lag", 20, 0, 0)
                    send("client", "lag", 20, 0, 0)
                wait("cooldown", lambda: own("client")["readyIn"] <= 0)
                before = own("client")
                send("client", "press", 0, 0, 1)
                time.sleep(0.12)
                send("client", "release", 0, 0, 1)
                release = own("client")
                action = release["action"]
                wait("ack", lambda: own("client")["confirmed"] >= action)
                if args.expect_pass:
                    wait("authoritative snapshot", lambda: next(p for p in read("host")["players"] if not p["owner"])["shotTick"] >= release["shotTick"])
                after = own("client")
                records.append(dict(index=index, rtt=read("client")["rtt"], before=before,
                                    release=release, after=after, server=read("host")))
                save()
                authority = next(p for p in records[-1]["server"]["players"] if not p["owner"])
                if args.expect_pass and release["rank"] != authority["rank"]:
                    raise AssertionError("Displayed card rank differs from the authoritative shot")
                print(json.dumps(dict(shot=index, action=action, rejected=after["rejected"] - before["rejected"],
                                      charging=release["charging"], matches=after["matches"], rtt=read("client")["rtt"])), flush=True)
            time.sleep(4)
            result = own("client")
            print(json.dumps(dict(result="done", totalRejected=result["rejected"], matches=result["matches"], root=str(root))), flush=True)
            if args.expect_pass and (result["rejected"] != 0 or result["matches"] != args.repeat):
                raise AssertionError("A legal shot was rejected or lost its authoritative handoff")
    finally:
        save()
        for peer in sequence:
            if read(peer):
                send(peer, "quit")
        for process in processes:
            try:
                process.wait(timeout=8)
            except subprocess.TimeoutExpired:
                process.terminate()


if __name__ == "__main__":
    main()
