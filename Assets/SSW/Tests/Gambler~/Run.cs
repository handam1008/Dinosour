var game = SSW.NetGame.Current;
if (!UnityEditor.EditorApplication.isPlaying || game == null || !game.Manager.IsServer || game.Players.Count != 2 || game.State.Phase != SSW.MatchPhase.Playing)
    throw new System.InvalidOperationException("An editor host and a Windows client must be playing");
string root = "@ROOT@";
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var checks = new System.Collections.Generic.List<string>();
var evidence = new System.Collections.Generic.List<object>();
string stage = "setup";
string error = null;
float began = UnityEngine.Time.time;
void Save(string file, bool success) => System.IO.File.WriteAllText(root + "/" + file + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
    success, stage, error, count = checks.Count, checks, evidence, seconds = UnityEngine.Time.time - began,
    scope = "Same PC Editor host and Windows development client; 60ms delay and 10ms jitter each direction; seeded server outcomes plus a real roulette projectile from each owner"
}, Newtonsoft.Json.Formatting.Indented));
void Check(bool condition, string label)
{
    if (!condition) throw new System.InvalidOperationException(stage + ": " + label);
    checks.Add(stage + ": " + label);
}
async System.Threading.Tasks.Task Wait(float seconds)
{
    double until = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds;
    while (UnityEngine.Time.realtimeSinceStartupAsDouble < until)
    {
        if (!UnityEditor.EditorApplication.isPlaying || game == null) throw new System.InvalidOperationException("Play session ended");
        await System.Threading.Tasks.Task.Delay(16);
    }
}
async System.Threading.Tasks.Task Until(System.Func<bool> condition, float seconds, string label)
{
    double until = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds;
    while (!condition())
    {
        if (UnityEngine.Time.realtimeSinceStartupAsDouble >= until) throw new System.TimeoutException(stage + ": " + label);
        await Wait(0.02f);
    }
}
bool Near(float value, float expected) => UnityEngine.Mathf.Abs(value - expected) < 0.08f;
SSW.CoinCast Coin(SSW.NetPlayer p) => p.GetComponent<SSW.CoinCast>();
Newtonsoft.Json.Linq.JObject Snapshot(string peer)
{
    try { return Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(root + "/" + peer + ".json")); }
    catch (System.IO.IOException) { return null; }
    catch (Newtonsoft.Json.JsonReaderException) { return null; }
}
Newtonsoft.Json.Linq.JToken Client(SSW.NetPlayer p)
{
    var snapshot = Snapshot("client");
    if (snapshot != null)
        foreach (var item in snapshot["players"])
            if ((ulong)item["id"] == p.OwnerClientId && (ulong)item["objectId"] == p.NetworkObjectId) return item;
    return null;
}
bool Flag(SSW.NetPlayer p, string key) => (bool?)Client(p)?["coin"]?[key] == true;
int Number(SSW.NetPlayer p, string key) => (int?)Client(p)?["coin"]?[key] ?? -1;
float Scalar(SSW.NetPlayer p, string key) => (float?)Client(p)?["coin"]?[key] ?? -1f;
System.Collections.Generic.HashSet<JJW.Script.Augments.GamblerAugmentType> Augments(SSW.CoinCast coin) =>
    (System.Collections.Generic.HashSet<JJW.Script.Augments.GamblerAugmentType>)typeof(SSW.CoinCast).GetField("_augments", flags).GetValue(coin);
int Seed(float low, float high, float oldLow = -1f, float oldHigh = -1f)
{
    var saved = UnityEngine.Random.state;
    try
    {
        for (int seed = 1; seed < 200000; seed++)
        {
            UnityEngine.Random.InitState(seed);
            float main = UnityEngine.Random.Range(0f, 100f);
            float old = UnityEngine.Random.Range(0f, 100f);
            if (main > low && main < high && (oldLow < 0f || old > oldLow && old < oldHigh)) return seed;
        }
        throw new System.InvalidOperationException("Seed not found");
    }
    finally { UnityEngine.Random.state = saved; }
}
void Roll(SSW.CoinCast coin, int seed)
{
    var saved = UnityEngine.Random.state;
    try { UnityEngine.Random.InitState(seed); typeof(SSW.CoinCast).GetMethod("Roll", flags).Invoke(coin, null); }
    finally { UnityEngine.Random.state = saved; }
}
async System.Threading.Tasks.Task Idle(SSW.CoinCast coin)
{
    await Until(() => !(bool)typeof(SSW.CoinCast).GetField("_spinning", flags).GetValue(coin) && coin.PendingRolls == 0, 8f, "roll queue idle");
}
void Evidence(SSW.NetPlayer p, string name)
{
    evidence.Add(new { name, time = UnityEngine.Time.time, actor = p.OwnerClientId, hp = p.Health.Current, max = p.Health.Max,
        speed = p.Motion.CurrentMoveSpeedMultiplier, damage = Coin(p).DamageScale, host = Snapshot("host"), client = Snapshot("client") });
    UnityEngine.ScreenCapture.CaptureScreenshot(root + "/" + p.OwnerClientId + "-" + name + ".png");
}
async System.Threading.Tasks.Task Outcome(SSW.NetPlayer p, int seed, string expected, int symbol, System.Func<bool> applied)
{
    var coin = Coin(p);
    await Idle(coin);
    stage = p.OwnerClientId + " " + expected;
    Save("Progress", false);
    int results = coin.Effects.Results;
    int rolls = coin.Effects.Rolls;
    float started = UnityEngine.Time.time;
    Roll(coin, seed);
    Check(coin.PendingRolls == 1 && coin.Effects.Results == results, "queued without applying result");
    await Until(() => Number(p, "rolls") == rolls + 1 && Flag(p, "spinning"), 1.5f, "client spinning");
    Check(coin.Effects.Reels.Spinning && coin.Effects.Main.ToString() == expected, "host and client spin for the authoritative result");
    Check(UnityEngine.Vector3.Dot(coin.Effects.Reels.transform.forward, game.Arena.View.transform.forward) > 0.999f && Flag(p, "facing"), "both roulette canvases face their local camera");
    await Until(() => UnityEngine.Time.time - started >= 2.25f, 3f, "pre-stop time");
    Check(coin.Effects.Results == results && Number(p, "results") == results, "no result before final reel stops");
    await Until(() => coin.Effects.Results > results && Number(p, "results") > results && applied(), 2f, "result applied on both peers");
    Check(UnityEngine.Time.time - started >= 2.5f, "server applies after 2.5 seconds");
    await Until(() => coin.Effects.Reels.Stops == 7 && Number(p, "stops") == 7, 1f, "all reels stopped");
    var symbols = Client(p)["coin"]["symbols"];
    Check(coin.Effects.Reels.Symbols == new UnityEngine.Vector3Int(symbol, symbol, symbol) && (int)symbols["x"] == symbol && (int)symbols["y"] == symbol && (int)symbols["z"] == symbol, "identical final symbols on both peers");
    Evidence(p, expected);
    if (!p.IsOwner) await CaptureClient(expected);
}
async System.Threading.Tasks.Task Send(string op, float x = 0f, float y = 0f)
{
    int seq = (int)Snapshot("client")["seq"] + 1;
    System.IO.File.WriteAllText(root + "/client.cmd.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { seq, op, value = 0, x, y }));
    await Until(() => (int?)Snapshot("client")?["seq"] >= seq, 3f, "client " + op);
}
async System.Threading.Tasks.Task CaptureClient(string name)
{
    var requested = System.DateTime.UtcNow;
    await Send("capture");
    await Until(() => System.IO.File.Exists(root + "/client.png") && System.IO.File.GetLastWriteTimeUtc(root + "/client.png") >= requested, 3f, "client screenshot");
    await Wait(0.05f);
    System.IO.File.Copy(root + "/client.png", root + "/client-" + name + ".png", true);
}
async System.Threading.Tasks.Task Fire777(SSW.NetPlayer p)
{
    var coin = Coin(p);
    await Idle(coin);
    stage = p.OwnerClientId + " real projectile 777 and queued roll";
    Save("Progress", false);
    Augments(coin).Add(JJW.Script.Augments.GamblerAugmentType.GuaranteedJackpot);
    coin.Progress = 20;
    var state = coin.Status;
    state.Ammo = 1; state.Filled = 0; state.Ready = 0; state.Reload = p.InputSequence + 100000;
    typeof(SSW.JobCast).GetProperty("State", flags).SetValue(coin, state);
    await Until(() => (int?)Client(p)?["ammo"] == 1, 3f, "roulette chamber replicated");
    int rolls = coin.Effects.Rolls;
    int shots = p.Cast.Shots;
    int results = coin.Effects.Results;
    if (p.IsOwner) { p.Cast.Attack(true, UnityEngine.Vector2.up); p.Cast.Attack(false, UnityEngine.Vector2.up); }
    else { await Send("press", 0f, 1f); await Send("release", 0f, 1f); }
    await Until(() => p.Cast.Shots == shots + 1 && coin.Effects.Rolls == rolls + 1 && Number(p, "rolls") == rolls + 1, 3f, "actual roulette projectile");
    Check(!coin.Jackpot && !coin.Effects.Jackpot && !Flag(p, "jackpot") && Near(p.Health.Max, 10000f), "real shot does not activate 777 early");
    Augments(coin).Clear();
    Roll(coin, Seed(30f, 33f));
    Check(coin.PendingRolls == 2, "second roll queued while first spins");
    await Until(() => coin.Jackpot && coin.Effects.Jackpot && Flag(p, "jackpot"), 4f, "777 applies on both peers");
    await Until(() => (float?)Client(p)?["max"] == 20000f, 2f, "777 health replicated");
    Check(Near(p.Health.Max, 20000f) && coin.Effects.Results == results + 1, "one 777 application doubles maximum health");
    await Until(() => p.IsOwner ? coin.Effects.OverlayAlpha > 0.1f : Scalar(p, "overlay") > 0.1f, 1f, "owner screen overlay");
    Check(p.IsOwner ? Near(Scalar(p, "overlay"), 0f) : Near(coin.Effects.OverlayAlpha, 0f), "777 overlay is owner-only");
    Check(coin.Effects.Reels.Symbols == new UnityEngine.Vector3Int(5, 5, 5), "777 reel symbols");
    Evidence(p, "Jackpot777");
    if (!p.IsOwner) await CaptureClient("Jackpot777");
    await Until(() => coin.Effects.Rolls == rolls + 2 && Number(p, "rolls") == rolls + 2, 3f, "queued spin starts");
    Check(coin.Effects.Main == JJW.Script.Jackpot.JackpotResultType.None && (string)Client(p)["coin"]["main"] == "None", "pending 777 cannot roll another 777");
    await Idle(coin);
    Check(coin.Effects.Results == results + 1 && coin.Effects.Reels.Symbols.x != coin.Effects.Reels.Symbols.y, "miss finishes without a second buff");
}
async System.Threading.Tasks.Task Run()
{
    try
    {
        foreach (var p in game.Players)
        {
            p.Buffs.SetBaseHealth(10000f); p.Health.Heal(10000f); p.Health.TakeDamage(2000f);
            p.Drive.Teleport(new UnityEngine.Vector2(p.Side * -3f, 3f));
        }
        foreach (var p in game.Players)
        {
            var coin = Coin(p);
            await Outcome(p, Seed(1f, 6f), "DamageUp", 1, () => coin.Effects.Damage && Flag(p, "damage"));
            Check(Near(coin.DamageScale, 1.3f), "damage buff applied once");
            await Outcome(p, Seed(23f, 27f), "SpeedUp", 3, () => coin.Effects.Speed && Flag(p, "speed"));
            Check(Near(p.Motion.CurrentMoveSpeedMultiplier, 1.523f), "speed buff matches existing balance");
            await Outcome(p, Seed(15f, 20f), "Invincible", 2, () => coin.Effects.Shield && Flag(p, "shield"));
            Check(p.Effects.ImmuneActive && (bool)Client(p)["immune"], "shield matches authoritative immunity");
            float hp = p.Health.Current;
            await Outcome(p, Seed(8f, 13f), "Heal", 0, () => coin.Effects.HealParticles > 0 && Number(p, "healed") > 0);
            Check(Near(p.Health.Current, hp + 50f), "heal applies once for fifty HP");
            await Fire777(p);
            await Outcome(p, Seed(28.2f, 28.8f), "InstantKill", 4, () => p.IsOwner ? coin.Effects.DeathAlpha > 0.1f : Scalar(p, "death") > 0.1f);
            Check(p.IsOwner ? Near(Scalar(p, "death"), 0f) : Near(coin.Effects.DeathAlpha, 0f), "444 screen mark is owner-only");
            stage = p.OwnerClientId + " natural effect expiry";
            Save("Progress", false);
            await Until(() => !coin.Jackpot && !coin.Effects.Jackpot && !Flag(p, "jackpot"), 15f, "fifteen-second jackpot expiry");
            Check(!coin.Effects.Damage && !coin.Effects.Speed && !coin.Effects.Shield && !Flag(p, "damage") && !Flag(p, "speed") && !Flag(p, "shield"), "all timed world effects naturally stopped");
            Check(Near(coin.Effects.OverlayAlpha, 0f) && Near(Scalar(p, "overlay"), 0f) && Near(p.Health.Max, 10000f) && Near(coin.DamageScale, 1f), "expiry clears overlay and restores balance");
            Evidence(p, "Expired");
        }
        var owner = game.Local;
        var cast = Coin(owner);
        Augments(cast).Add(JJW.Script.Augments.GamblerAugmentType.OldCoin);
        await Outcome(owner, Seed(1f, 6f, 3.1f, 3.9f), "DamageUp", 1, () => cast.Effects.Damage && cast.Effects.Speed && Flag(owner, "damage") && Flag(owner, "speed"));
        Check(cast.Effects.Old == JJW.Script.Jackpot.JackpotResultType.SpeedUp, "old coin additional result is replicated and presented");
        float beforeHeal = owner.Health.Current;
        await Outcome(owner, Seed(8f, 13f, 1.1f, 1.9f), "Heal", 0, () => cast.Effects.HealParticles > 0 && Number(owner, "healed") > 0);
        Check(Near(owner.Health.Current, beforeHeal + 100f), "matching old coin Heal applies two fifty-HP heals");
        Augments(cast).Clear();
        await Idle(cast);
        stage = "round end and pending roll cleanup";
        Save("Progress", false);
        owner.Health.TakeDamage(owner.Health.Current - 1f);
        Roll(cast, Seed(28.2f, 28.8f));
        Roll(cast, Seed(1f, 6f));
        await Until(() => owner.Health.Current <= 0f && cast.Effects.DeathAlpha > 0.1f, 4f, "lethal 444 still shows mark");
        await Wait(0.15f);
        Check(cast.PendingRolls == 0 && !cast.Effects.Damage && !cast.Effects.Speed && !cast.Effects.Jackpot && !cast.Effects.Reels.Spinning, "death discards queued buffs and clears presentation");
        Check(Near(Scalar(owner, "death"), 0f), "opponent does not receive the lethal 444 overlay");
        Evidence(owner, "Lethal444");
        await Wait(0.9f);
        Check(Near(cast.Effects.DeathAlpha, 0f), "lethal mark fades out after round end");
        Check(string.IsNullOrWhiteSpace((string)Snapshot("host")["error"]) && string.IsNullOrWhiteSpace((string)Snapshot("client")["error"]), "neither peer logged an exception");
    }
    catch (System.Exception exception) { error = exception.ToString(); }
    Save("Result", error == null);
}
_ = Run();
return new { running = true, root };
