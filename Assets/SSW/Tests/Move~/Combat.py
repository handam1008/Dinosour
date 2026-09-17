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
    parser.add_argument("--scenario", default="cadence", choices=("cadence", "impact", "motion", "stall"))
    parser.add_argument("--step", action="store_true")
    parser.add_argument("--loss", type=int, default=3)
    parser.add_argument("--host-fps", type=int, default=60)
    parser.add_argument("--client-fps", type=int, default=60)
    parser.add_argument("--port", type=int, default=7797)
    parser.add_argument("--expect-pass", action="store_true")
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
                str(project / args.build), "--net-mode", peer, "--net-job", "Magician",
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
        send("host", "warp", 0, 13, 0)
        send("host", "warp", 1, -8, 0)
        time.sleep(1)
        for peer in sequence:
            send(peer, "lag", args.delay, args.jitter, args.loss)
        time.sleep(2)
        if args.scenario == "impact":
            send("host", "warp", 0, -6, 0)
            send("host", "warp", 1, -8, 0)
            wait("grounded", lambda: all(p["grounded"] for p in read("host")["players"]))
            time.sleep(1.5)
            send("client", "trace", 2, 5)
            send("host", "trace", 2, 5)
            before = read("host")
            send("client", "press", 0, 1, 0)
            time.sleep(0.15)
            send("host", "jump")
            send("client", "release", 0, 1, 0)
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
