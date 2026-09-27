var game = SSW.NetGame.Current;
if (!UnityEditor.EditorApplication.isPlaying || game == null || !game.Manager.IsServer || game.Players.Count != 2 || game.State.Phase != SSW.MatchPhase.Playing)
    throw new System.InvalidOperationException("Requires an editor host, one connected client and Playing phase.");
if (UnityEditor.SessionState.GetBool("Augments.Effects.Running", false))
    throw new System.InvalidOperationException("Another augment effect script is still running.");
string root = System.IO.Path.GetFullPath("@ROOT@");
string filter = "@FILTER@";
if (filter.Length > 1 && filter[0] == '@') filter = string.Empty;
var filters = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(filter.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries), value => value.Trim()));
foreach (string value in filters)
    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^(?:ID)?(?:2[2-9]|3[01])(?::[^,]+)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        throw new System.ArgumentException("Filter format: 22:drain,26,28:heal,28:jackpot");
var matched = new bool[filters.Length];
var skipped = new System.Collections.Generic.List<string>();
int selectedCases = 0;
string log = System.IO.Path.Combine(root, "CoinEffects.json");
string clientPath = System.IO.Path.Combine(root, "client.json");
if (!System.IO.File.Exists(clientPath))
    throw new System.InvalidOperationException("Use the active NetProbe log root containing client.json.");
if (System.IO.File.Exists(log))
    throw new System.InvalidOperationException("Use a fresh CoinEffects result path.");
System.IO.Directory.CreateDirectory(root);
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
var checks = new System.Collections.Generic.List<string>();
var cases = new System.Collections.Generic.List<object>();
var failures = new System.Collections.Generic.List<object>();
var replicas = new System.Collections.Generic.List<object>();
var temporary = new System.Collections.Generic.List<UnityEngine.GameObject>();
var original = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(game.Players, p => new {
    p.OwnerClientId, p.Side, p.Info, p.Job, cards = System.Linq.Enumerable.ToArray(p.Draft.Owned)
}));
var prefab = (SSW.NetPlayer)new UnityEditor.SerializedObject(game).FindProperty("_playerPrefab").objectReferenceValue;
string stage = "setup";
uint action = 1000;
float began = UnityEngine.Time.time;
bool replaced = false;
System.Reflection.FieldInfo Field(object item, string name)
{
    for (var type = item.GetType(); type != null; type = type.BaseType)
    {
        var field = type.GetField(name, flags);
        if (field != null) return field;
    }
    throw new System.MissingFieldException(item.GetType().FullName, name);
}
object Read(object item, string name) => Field(item, name).GetValue(item);
void Set(object item, string name, object value) => Field(item, name).SetValue(item, value);
bool Near(float actual, float expected, float tolerance = 0.03f) => UnityEngine.Mathf.Abs(actual - expected) <= tolerance;
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(stage + ": " + label);
    checks.Add(stage + ": " + label);
}
object Snap(SSW.NetPlayer p) => new {
    owner = p.OwnerClientId, clientOwned = p.OwnerClientId != game.Manager.LocalClientId,
    objectId = p.NetworkObjectId, job = p.Job.ToString(), hp = p.Health.Current, max = p.Health.Max,
    speed = p.Motion.MoveSpeed, multiplier = p.Motion.CurrentMoveSpeedMultiplier,
    x = p.Body.position.x, y = p.Body.position.y, vx = p.Velocity.x, vy = p.Velocity.y,
    hidden = p.Effects.Hidden, blind = p.Effects.BlindActive, damageScale = p.Cast.Weapon.DamageScale,
    tick = p.InputSequence, ready = p.Cast.Weapon.Status.Ready, skill = p.Cast.Weapon.Status.Skill
};
void Save(string status, string error = null)
{
    string json = Newtonsoft.Json.JsonConvert.SerializeObject(new {
        status, stage, error, filter, selectedCases, skipped, started = began, now = UnityEngine.Time.time,
        dispatch = "Server JobCast.Apply on host-owned and client-owned actors; not native client input.",
        isolation = "Caller supplied isolate and wideground at (0,3) on both peers; no asset edits.",
        count = checks.Count, checks, cases, failures, replicas,
        reused = new[] { "EffectsFirst E2 ID46", "EffectsFirst E4 ID49 numeric", "EffectsFirst E3 active jackpot stack preservation IDs24/28/30" }
    }, Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText(log, json);
}
async System.Threading.Tasks.Task Wait(float seconds)
{
    float end = UnityEngine.Time.time + UnityEngine.Mathf.Max(0f, seconds);
    double watchdog = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds + 12d;
    while (UnityEngine.Time.time < end)
    {
        if (!UnityEditor.EditorApplication.isPlaying || game == null || game.State.Phase != SSW.MatchPhase.Playing || UnityEngine.Time.realtimeSinceStartupAsDouble > watchdog)
            throw new System.TimeoutException(stage + ": play session stopped, changed phase or stopped advancing.");
        await System.Threading.Tasks.Task.Delay(16);
    }
}
async System.Threading.Tasks.Task Until(System.Func<bool> condition, float timeout, string label)
{
    float end = UnityEngine.Time.time + timeout;
    while (!condition())
    {
        if (UnityEngine.Time.time >= end) throw new System.TimeoutException(stage + ": " + label);
        await Wait(0.016f);
    }
}
async System.Threading.Tasks.Task At(float time) => await Wait(UnityEngine.Mathf.Max(0f, time - UnityEngine.Time.time));
Newtonsoft.Json.Linq.JToken Client(SSW.NetPlayer player)
{
    try
    {
        var json = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(clientPath));
        foreach (var item in json["players"])
            if ((ulong)item["objectId"] == player.NetworkObjectId && (ulong)item["id"] == player.OwnerClientId) return item;
    }
    catch (System.IO.IOException) { }
    catch (Newtonsoft.Json.JsonReaderException) { }
    return null;
}
async System.Threading.Tasks.Task Replicate(SSW.NetPlayer p, string key, float expected, float timeout = 3f)
{
    await Until(() => { var value = Client(p); return value != null && Near((float)value[key], expected, 0.08f); }, timeout, "client " + key + " expected " + expected);
    replicas.Add(new { stage, owner = p.OwnerClientId, objectId = p.NetworkObjectId, key, expected, actual = (float)Client(p)[key] });
}
async System.Threading.Tasks.Task ReplicateFlag(SSW.NetPlayer p, string key, bool expected)
{
    await Until(() => { var value = Client(p); return value != null && (bool)value[key] == expected; }, 2f, "client " + key);
    replicas.Add(new { stage, owner = p.OwnerClientId, objectId = p.NetworkObjectId, key, expected, actual = (bool)Client(p)[key] });
}
void ClearShots() => typeof(SSW.NetGame).GetMethod("ClearShots", flags).Invoke(game, null);
void Max(SSW.NetPlayer p, float maximum)
{
    p.Buffs.SetBaseHealth(maximum);
    p.Health.Heal(maximum);
}
void SetHealth(SSW.NetPlayer p, float health)
{
    Check(health > 0f && health <= p.Health.Max, "health fixture is within current maximum");
    p.Health.Heal(p.Health.Max);
    if (p.Health.Current > health) p.Health.TakeDamage(p.Health.Current - health);
    cases.Add(new { stage, owner = p.OwnerClientId, type = "health fixture", expected = health, actual = p.Health.Current, maximum = p.Health.Max, sourceMaximum = p.Stats.Health });
    Check(Near(p.Health.Current, health), "health fixture actually reached requested value");
}
async System.Threading.Tasks.Task Place(SSW.NetPlayer actor, SSW.NetPlayer target, float distance = 1.1f)
{
    actor.Drive.Teleport(new UnityEngine.Vector2(-distance * 0.5f, 5f));
    target.Drive.Teleport(new UnityEngine.Vector2(distance * 0.5f, 5f));
    UnityEngine.Physics2D.SyncTransforms();
    await Until(() => actor.Drive.Grounded && target.Drive.Grounded, 5f, "players must reach caller's wideground");
    await Wait(0.08f);
}
SSW.NetPlayer Actor(ulong owner) => System.Linq.Enumerable.First(game.Players, p => p.OwnerClientId == owner);
SSW.NetPlayer Other(ulong owner) => System.Linq.Enumerable.First(game.Players, p => p.OwnerClientId != owner);
async System.Threading.Tasks.Task Reset(ulong owner, SSW.PlayerJob job, int card, SSW.PlayerJob otherJob = SSW.PlayerJob.Swordsman)
{
    replaced = true;
    ClearShots();
    foreach (var p in System.Linq.Enumerable.ToArray(game.Players)) p.NetworkObject.Despawn();
    foreach (var saved in original)
    {
        var p = UnityEngine.Object.Instantiate(prefab);
        bool selected = saved.OwnerClientId == owner;
        p.Init(selected ? job : otherJob, saved.Side, saved.Info);
        p.NetworkObject.SpawnAsPlayerObject(saved.OwnerClientId, true);
        p.Draft.Restore(selected && card >= 0 ? new[] { card } : System.Array.Empty<int>());
        p.Block(true);
    }
    var actor = Actor(owner);
    var target = Other(owner);
    UnityEngine.Physics2D.IgnoreCollision(actor.Collider, target.Collider);
    await Place(actor, target);
    await Until(() => Client(actor) != null && Client(target) != null, 8f, "replacement player replication");
    Check(actor.CanAct && target.CanAct, "isolated actors are alive in Playing phase");
}
async System.Threading.Tasks.Task Ready(SSW.NetPlayer player, SSW.CastKind kind)
{
    await Until(() => {
        var state = player.Cast.Weapon.Status;
        uint ready = kind == SSW.CastKind.Cycle ? state.Skill : kind == SSW.CastKind.Parry ? state.Parry : state.Ready;
        return player.InputSequence >= ready;
    }, 25f, "base cast cooldown " + kind);
}
bool Cast(SSW.NetPlayer player, SSW.CastKind kind, UnityEngine.Vector2 direction)
{
    var input = new SSW.CastInput { Action = ++action, Epoch = player.Epoch, Tick = player.InputSequence, Kind = kind, Direction = direction, ViewTime = game.PhysicsTime };
    return player.Cast.Weapon.Apply(input, player.Body.position, 0d);
}
async System.Threading.Tasks.Task<float> Hit(SSW.NetPlayer attacker, SSW.NetPlayer target)
{
    await Ready(attacker, SSW.CastKind.Press);
    float before = target.Health.Current;
    Check(Cast(attacker, SSW.CastKind.Press, new UnityEngine.Vector2(UnityEngine.Mathf.Sign(target.Body.position.x - attacker.Body.position.x), 0f)), "real basic cast accepted");
    await Until(() => target.Health.Current < before, 2f, "real melee damage");
    return before - target.Health.Current;
}
async System.Threading.Tasks.Task Case(string label, System.Func<System.Threading.Tasks.Task> test)
{
    bool selected = filters.Length == 0;
    string id = System.Text.RegularExpressions.Regex.Match(label, @"\bID(\d+)\b").Groups[1].Value;
    for (int i = 0; i < filters.Length; i++)
    {
        string[] parts = filters[i].Split(new[] { ':' }, 2);
        string wanted = parts[0].StartsWith("ID", System.StringComparison.OrdinalIgnoreCase) ? parts[0].Substring(2) : parts[0];
        if (wanted != id || parts.Length > 1 && label.IndexOf(parts[1], System.StringComparison.OrdinalIgnoreCase) < 0) continue;
        matched[i] = true;
        selected = true;
    }
    if (!selected) { skipped.Add(label); return; }
    selectedCases++;
    stage = label;
    Save("running");
    try { await test(); checks.Add(stage + ": completed"); }
    catch (System.Exception error)
    {
        failures.Add(new { stage, error = error.ToString(), players = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(game.Players, p => Snap(p))) });
    }
    Save("running");
}
void Restore()
{
    ClearShots();
    foreach (var item in temporary) if (item != null) UnityEngine.Object.Destroy(item);
    temporary.Clear();
    if (!replaced) return;
    foreach (var p in System.Linq.Enumerable.ToArray(game.Players)) p.NetworkObject.Despawn();
    foreach (var saved in original)
    {
        var p = UnityEngine.Object.Instantiate(prefab);
        p.Init(saved.Job, saved.Side, saved.Info);
        p.NetworkObject.SpawnAsPlayerObject(saved.OwnerClientId, true);
        p.Draft.Restore(saved.cards);
        p.Drive.Teleport(new UnityEngine.Vector2(saved.Side * -0.55f, 5f));
    }
}

var rollMethod = typeof(SSW.CoinCast).GetMethod("Roll", flags);
var tableMethod = typeof(SSW.CoinCast).GetMethod("RollTable", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
var seeds = new System.Collections.Generic.Dictionary<string, int>();
int Seed(float low, float high, float oldLow = 0f, float oldHigh = 100f)
{
    string key = low + ":" + high + ":" + oldLow + ":" + oldHigh;
    if (seeds.TryGetValue(key, out int found)) return found;
    var saved = UnityEngine.Random.state;
    try
    {
        for (int candidate = 1; candidate <= 100000; candidate++)
        {
            UnityEngine.Random.InitState(candidate);
            float main = UnityEngine.Random.Range(0f, 100f);
            float old = UnityEngine.Random.Range(0f, 100f);
            if (main >= low && main < high && old >= oldLow && old < oldHigh)
            {
                seeds.Add(key, candidate);
                return candidate;
            }
        }
        throw new System.InvalidOperationException("No controlled seed in requested range " + key);
    }
    finally { UnityEngine.Random.state = saved; }
}
object Roll(SSW.NetPlayer p, int seed)
{
    var cast = (SSW.CoinCast)p.Cast.Weapon;
    float before = p.Health.Current;
    var saved = UnityEngine.Random.state;
    float main;
    float old;
    try
    {
        UnityEngine.Random.InitState(seed);
        main = UnityEngine.Random.Range(0f, 100f);
        old = UnityEngine.Random.Range(0f, 100f);
        UnityEngine.Random.InitState(seed);
        rollMethod.Invoke(cast, null);
    }
    finally { UnityEngine.Random.state = saved; }
    var result = new {
        stage, owner = p.OwnerClientId, hook = "Seeded real private CoinCast.Roll, not a fired roulette/input sample.",
        seed, main, old, before, after = p.Health.Current, max = p.Health.Max,
        cast.Progress, cast.Stacks, cast.Probability, cast.Jackpot, cast.DamageScale,
        jackpotUntil = (float)Read(cast, "_jackpotUntil"),
        probabilityBonus = (float)Read(cast, "_probability"),
        damageBuffs = ((System.Collections.Generic.List<float>)Read(cast, "_damageUntil")).Count
    };
    cases.Add(result);
    return result;
}
string Table(int seed, float common, float lethal, float jackpot)
{
    var saved = UnityEngine.Random.state;
    try
    {
        UnityEngine.Random.InitState(seed);
        return tableMethod.Invoke(null, new object[] { common, lethal, jackpot }).ToString();
    }
    finally { UnityEngine.Random.state = saved; }
}
SSW.NetBolt Bolt(SSW.NetPlayer p)
{
    foreach (var item in game.Manager.SpawnManager.SpawnedObjects.Values)
        if (item.TryGetComponent<SSW.NetBolt>(out var bolt) && bolt.Caster == p.NetworkObjectId && bolt.Action == action) return bolt;
    throw new System.InvalidOperationException(stage + ": accepted Apply did not spawn the expected authoritative bolt.");
}
void OrdinaryCoin(SSW.NetPlayer p)
{
    var network = (Unity.Netcode.NetworkVariable<SSW.WeaponState>)Read(p.Cast.Weapon, "_state");
    var state = network.Value;
    Check(p.Stats.Capacity > 1, "ordinary-shot comparison requires at least two ammo slots");
    state.Filled = (uint)(state.Ammo % p.Stats.Capacity);
    network.Value = state;
}
async System.Threading.Tasks.Task<float> Fire(SSW.NetPlayer p, SSW.NetPlayer t)
{
    await Ready(p, SSW.CastKind.Press);
    await Until(() => p.Cast.Weapon.Status.Ammo > 0, p.Stats.Reload + 3f, "normal ammunition reload");
    OrdinaryCoin(p);
    float before = t.Health.Current;
    float start = UnityEngine.Time.time;
    UnityEngine.Vector2 direction = ((UnityEngine.Vector2)t.Collider.bounds.center - p.Body.position).normalized;
    Check(Cast(p, SSW.CastKind.Press, direction), "actual coin Apply accepted");
    var bolt = Bolt(p);
    int style = bolt.Spec.Style;
    float baseDamage = bolt.Spec.Damage;
    Check(style == 1, "controlled ordinary ammo avoids a new roulette result");
    await Until(() => t.Health.Current < before, 3f, "actual coin collision damage");
    float damage = before - t.Health.Current;
    cases.Add(new { stage, owner = p.OwnerClientId, type = "real projectile collision", hook = "Filled selects an ordinary ammo slot; no Hit invocation or injected victim damage.", style, baseDamage, damage, before, after = t.Health.Current, elapsed = UnityEngine.Time.time - start });
    return damage;
}
async System.Threading.Tasks.Task Run()
{
    string fatal = null;
    UnityEditor.SessionState.SetBool("Augments.Effects.Running", true);
    Save("running");
    try
    {
        foreach (var saved in original)
        {
            ulong owner = saved.OwnerClientId;
            string peer = owner == game.Manager.LocalClientId ? "host" : "client-owner";
            await Case(peer + " ID22 CoinUpgrade slow", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, -1);
                var p = Actor(owner); var t = Other(owner);
                Roll(p, Seed(22f, 26f));
                float control = await Fire(p, t);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 1f), "unaugmented SpeedUp coin does not slow victim");
                await Reset(owner, SSW.PlayerJob.Gambler, 22);
                p = Actor(owner); t = Other(owner);
                float start = UnityEngine.Time.time;
                Roll(p, Seed(22f, 26f));
                float until = (float)Read(p.Cast.Weapon, "_slowUntil");
                float dealt = await Fire(p, t);
                float slowEnd = (float)Read(t.Motion, "_slowEndTime");
                cases.Add(new { stage, control, dealt, baseDamage = p.Stats.Damage, buffWindow = until - start, slowRemaining = slowEnd - UnityEngine.Time.time, target = Snap(t) });
                Check(Near(dealt, p.Stats.Damage) && Near(t.Motion.CurrentMoveSpeedMultiplier, 0.8f), "actual upgraded coin applies twenty-percent slow");
                Check(Near(until - start, 7.4f, 0.08f) && slowEnd - UnityEngine.Time.time > 0.35f, "upgrade window and half-second hit slow are separate");
                await Replicate(t, "speed", t.Stats.MoveSpeed * 0.8f);
                await At(slowEnd + 0.08f);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 1f), "hit slow expires naturally");
                await At(until + 0.1f);
                await Fire(p, t);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 1f), "coin no longer slows after natural buff-window expiry");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID22 CoinUpgrade burn", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 22);
                var p = Actor(owner); var t = Other(owner);
                float start = UnityEngine.Time.time;
                Roll(p, Seed(1f, 6f));
                var amounts = new System.Collections.Generic.List<float>();
                var times = new System.Collections.Generic.List<float>();
                System.Action<float, bool> onHit = (amount, critical) => { amounts.Add(amount); times.Add(UnityEngine.Time.time - start); };
                t.Health.OnDamageDealt += onHit;
                try
                {
                    float before = t.Health.Current;
                    await Fire(p, t);
                    await Wait(2.35f);
                    float damage = before - t.Health.Current;
                    cases.Add(new { stage, damage, amounts = amounts.ToArray(), times = times.ToArray() });
                    Check(amounts.Count == 5 && Near(amounts[0], p.Stats.Damage * 1.3f), "one real projectile and four damage-over-time ticks");
                    for (int i = 1; i < amounts.Count; i++)
                    {
                        Check(Near(amounts[i], 2f * 1.3f), "burn tick uses actual outgoing multiplier " + i);
                        Check(times[i] - times[i - 1] >= 0.46f && times[i] - times[i - 1] < 0.8f, "half-second burn spacing " + i);
                    }
                    Check(Near(damage, p.Stats.Damage * 1.3f + 8f * 1.3f, 0.08f), "real total damage matches projectile plus burn");
                    await Replicate(t, "hp", t.Health.Current);
                    await At(start + 10.15f);
                    amounts.Clear(); times.Clear();
                    float afterExpiry = await Fire(p, t);
                    await Wait(2.2f);
                    cases.Add(new { stage, type = "natural expiry", afterExpiry, remainingDamageBuffs = ((System.Collections.Generic.List<float>)Read(p.Cast.Weapon, "_damageUntil")).Count, amounts = amounts.ToArray() });
                    Check(Near(afterExpiry, p.Stats.Damage) && amounts.Count == 1 && Near(p.Cast.Weapon.DamageScale, 1f), "expired DamageUp stops both extra damage and burn");
                }
                finally { t.Health.OnDamageDealt -= onHit; }
            });
            await Case(peer + " ID22 CoinUpgrade drain", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, -1);
                var p = Actor(owner); var t = Other(owner);
                SetHealth(p, p.Health.Max * 0.2f);
                Roll(p, Seed(8f, 13f));
                float controlHp = p.Health.Current;
                await Fire(p, t);
                Check(Near(p.Health.Current, controlHp), "ordinary Heal outcome does not grant drain without card");
                await Reset(owner, SSW.PlayerJob.Gambler, 22);
                p = Actor(owner); t = Other(owner);
                float prepared = p.Health.Max * 0.2f;
                SetHealth(p, prepared);
                Check(p.Health.Max - prepared > 50f + p.Stats.Damage * 0.2f, "fixture leaves room for Heal and projectile drain");
                float start = UnityEngine.Time.time;
                Roll(p, Seed(8f, 13f));
                Check(Near(p.Health.Current - prepared, 50f), "seeded real Heal outcome restores fifty HP");
                float until = (float)Read(p.Cast.Weapon, "_stealUntil");
                float before = p.Health.Current;
                float dealt = await Fire(p, t);
                float healed = p.Health.Current - before;
                cases.Add(new { stage, dealt, healed, duration = until - start, actor = Snap(p) });
                Check(Near(healed, dealt * 0.2f) && Near(until - start, 10f, 0.08f), "actual collision drains twenty percent during ten-second window");
                await Replicate(p, "hp", p.Health.Current);
                await At(until + 0.1f);
                before = p.Health.Current;
                await Fire(p, t);
                Check(Near(p.Health.Current, before), "natural expiry removes drain");
                await Replicate(t, "hp", t.Health.Current);
            });
            foreach (int card in new[] { 23, 25, 29 })
            {
                int selected = card;
                await Case(peer + " ID" + selected + " Jackpot perk", async () => {
                    await Reset(owner, SSW.PlayerJob.Gambler, selected);
                    var p = Actor(owner); var t = Other(owner);
                    var coin = (SSW.CoinCast)p.Cast.Weapon;
                    float start = UnityEngine.Time.time;
                    Roll(p, Seed(30f, 33f));
                    float end = (float)Read(coin, "_jackpotUntil");
                    float speed = selected == 23 ? 1.5f : selected == 25 ? 1.2f : 1f;
                    float scale = selected == 25 ? 1.3f : selected == 29 ? 1.5f : 1f;
                    float damage = await Fire(p, t);
                    cases.Add(new { stage, damage, baseDamage = p.Stats.Damage, duration = end - start, actor = Snap(p) });
                    Check(coin.Jackpot && Near(p.Health.Max, p.Stats.Health * 2f) && Near(end - start, 15f, 0.12f), "actual 777 starts fifteen-second state with doubled base health");
                    Check(Near(p.Motion.CurrentMoveSpeedMultiplier, speed) && Near(damage, p.Stats.Damage * scale), "isolated perk changes real speed and coin damage by expected amount");
                    await Replicate(p, "speed", p.Stats.MoveSpeed * speed);
                    await Replicate(p, "max", p.Health.Max);
                    await Replicate(t, "hp", t.Health.Current);
                    await At(end + 0.15f);
                    Check(!coin.Jackpot && Near(p.Motion.CurrentMoveSpeedMultiplier, 1f) && Near(p.Health.Max, p.Stats.Health), "natural 777 expiry restores speed and maximum health");
                    float expired = await Fire(p, t);
                    Check(Near(expired, p.Stats.Damage), "real projectile damage returns to baseline after expiry");
                    cases.Add(new { stage, type = "natural expiry", expired, actor = Snap(p) });
                    await Replicate(p, "speed", p.Stats.MoveSpeed);
                    await Replicate(p, "max", p.Health.Max);
                });
            }
            await Case(peer + " ID24 GuaranteedJackpot normal accumulation", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 24);
                var p = Actor(owner);
                var coin = (SSW.CoinCast)p.Cast.Weapon;
                var progress = new System.Collections.Generic.List<int>();
                int success = Seed(8f, 13f);
                for (int i = 1; i <= 20; i++)
                {
                    Roll(p, success);
                    progress.Add(coin.Progress);
                    Check(coin.Progress == i && !coin.Jackpot, "real non-777 successful roll increments progress " + i);
                }
                Roll(p, Seed(60f, 90f));
                cases.Add(new { stage, hook = "Twenty bounded real Roll calls without shots, followed by one guaranteed resolution; no active-777 preservation retest.", progress = progress.ToArray(), after = coin.Progress, coin.Jackpot, max = p.Health.Max });
                Check(coin.Progress == 0 && coin.Jackpot && Near(p.Health.Max, p.Stats.Health * 2f), "twenty accumulated successes actually trigger and consume guaranteed 777");
                await Replicate(p, "progress", 0f);
                await Replicate(p, "max", p.Health.Max);
            });
            await Case(peer + " ID26 Luck probability boundaries", async () => {
                int common = Seed(7.1f, 7.4f);
                await Reset(owner, SSW.PlayerJob.Gambler, -1);
                var p = Actor(owner);
                float prepared = p.Health.Max * 0.2f;
                SetHealth(p, prepared);
                Roll(p, common);
                float controlHeal = p.Health.Current - prepared;
                Check(Near(controlHeal, 50f) && Near(p.Cast.Weapon.DamageScale, 1f), "control draw above seven resolves as Heal");
                await Reset(owner, SSW.PlayerJob.Gambler, 26);
                p = Actor(owner); var t = Other(owner);
                prepared = p.Health.Max * 0.2f;
                SetHealth(p, prepared);
                Roll(p, common);
                float enhanced = await Fire(p, t);
                Check(Near(p.Health.Current, prepared) && Near(enhanced, p.Stats.Damage * 1.3f), "same draw becomes DamageUp when common bucket grows to 7.5");
                int lethal = Seed(31.1f, 31.4f);
                string controlResult = Table(lethal, 7f, 1f, 5f);
                await Reset(owner, SSW.PlayerJob.Gambler, 26);
                p = Actor(owner);
                Max(p, 10000f);
                await Wait(0.1f);
                Check(Near(p.Health.Max, 10000f) && Near(p.Health.Current, 10000f), "runtime-only lethal fixture survives health refresh");
                Roll(p, lethal);
                float selfDamage = 10000f - p.Health.Current;
                Check(controlResult == "Jackpot777" && Near(selfDamage, 4444f, 0.1f) && !((SSW.CoinCast)p.Cast.Weapon).Jackpot, "grown lethal bucket resolves real 4444 self damage");
                int upper = Seed(36.2f, 36.8f);
                string outside = Table(upper, 7f, 1f, 5f);
                await Reset(owner, SSW.PlayerJob.Gambler, 26);
                p = Actor(owner);
                Roll(p, upper);
                cases.Add(new { stage, controlHeal, enhanced, selfDamage, hook = "Only the lethal fixture uses NetBuff.SetBaseHealth(10000) on its temporary actor; StatsBook and original assets remain unchanged. Control classifications use seeded real RollTable.", controlResult, outside, chance = ((SSW.CoinCast)p.Cast.Weapon).Probability });
                Check(outside == "None" && ((SSW.CoinCast)p.Cast.Weapon).Jackpot && Near(((SSW.CoinCast)p.Cast.Weapon).Probability, 5.5f), "expanded upper 777 bucket triggers actual jackpot");
                await Replicate(p, "max", p.Health.Max);
            });
            await Case(peer + " ID27 MoreChances", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 27);
                var p = Actor(owner);
                var coin = (SSW.CoinCast)p.Cast.Weapon;
                var ammo = new System.Collections.Generic.List<object>();
                int none = Seed(60f, 90f);
                int capacity = p.Stats.Capacity;
                for (int i = 0; i < capacity; i++)
                {
                    await Ready(p, SSW.CastKind.Press);
                    int before = coin.Status.Ammo;
                    var rng = UnityEngine.Random.state;
                    SSW.NetBolt shot;
                    try
                    {
                        UnityEngine.Random.InitState(none);
                        Check(Cast(p, SSW.CastKind.Press, UnityEngine.Vector2.up), "real roulette shot accepted " + i);
                        shot = Bolt(p);
                    }
                    finally { UnityEngine.Random.state = rng; }
                    ammo.Add(new { index = i, before, after = coin.Status.Ammo, style = shot.Spec.Style });
                    Check(shot.Spec.Style == 2 && coin.Status.Ammo == before - 1, "every ammo position produces roulette style " + i);
                    ClearShots();
                }
                int edge = Seed(24.1f, 24.6f);
                string baseline = Table(edge, 7f, 1f, 5f);
                string changed = Table(edge, 5f, 1f, 3f);
                cases.Add(new { stage, ammo, capacity, chance = coin.Probability, baseline, changed, hook = "Real Apply for one clip; seeded real RollTable brackets probability change, not a statistical distribution estimate." });
                Check(coin.Status.Ammo == 0 && Near(coin.Probability, 3f) && baseline == "SpeedUp" && changed == "None", "full clip roulette and reduced common/777 probability thresholds");
                await Replicate(p, "ammo", 0f);
                await Until(() => coin.Status.Ammo == capacity, p.Stats.Reload + 3f, "normal reload after full clip");
                Check(coin.Status.Ammo == capacity, "reload restores actual ammo");
            });
            await Case(peer + " ID28 OldCoin damage", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 28);
                var p = Actor(owner); var t = Other(owner);
                Roll(p, Seed(1f, 6f, 0.1f, 0.9f));
                float doubled = await Fire(p, t);
                int buffs = ((System.Collections.Generic.List<float>)Read(p.Cast.Weapon, "_damageUntil")).Count;
                Check(buffs == 2 && Near(doubled, p.Stats.Damage * 1.3f * 1.3f, 0.06f), "identical main and old DamageUp both apply to actual projectile");
                cases.Add(new { stage, doubled, buffs });
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID28 OldCoin heal", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 28);
                var p = Actor(owner);
                float prepared = p.Health.Max * 0.2f;
                SetHealth(p, prepared);
                float capacity = p.Health.Max - prepared;
                Check(capacity > 50f, "fixture leaves room for two observable Heal outcomes");
                var amounts = new System.Collections.Generic.List<float>();
                System.Action<float> onHeal = amount => amounts.Add(amount);
                p.Health.OnHealed += onHeal;
                try { Roll(p, Seed(8f, 13f, 1.1f, 1.9f)); }
                finally { p.Health.OnHealed -= onHeal; }
                float healed = p.Health.Current - prepared;
                float first = UnityEngine.Mathf.Min(50f, capacity);
                float second = UnityEngine.Mathf.Min(50f, capacity - first);
                cases.Add(new { stage, prepared, maximum = p.Health.Max, capacity, healed, amounts = amounts.ToArray(), expectedFirst = first, expectedSecond = second, hook = "Real Heal callbacks include the existing max-health cap; no StatsBook override." });
                Check(amounts.Count == 2 && Near(amounts[0], first) && Near(amounts[1], second) && Near(healed, first + second), "identical Heal results apply twice with the normal maximum-health cap");
                await Replicate(p, "hp", p.Health.Current);
            });
            await Case(peer + " ID28 OldCoin jackpot", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 28);
                var p = Actor(owner);
                Roll(p, Seed(60f, 90f, 4.1f, 4.9f));
                Check(((SSW.CoinCast)p.Cast.Weapon).Jackpot && Near(p.Health.Max, p.Stats.Health * 2f), "old coin alone starts actual 777 when main result is None");
                int oldEdge = Seed(4.1f, 4.9f);
                string oldResult = Table(oldEdge, 1f, 0f, 1f);
                cases.Add(new { stage, oldResult, hook = "Old coin independently starts 777; old lethal=0 boundary classified by real RollTable; active-777 preservation reused." });
                Check(oldResult == "Jackpot777", "old coin boundary skips 444 and enters jackpot bucket");
                await Replicate(p, "max", p.Health.Max);
            });
            await Case(peer + " ID30 ProbabilityShift accumulation", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 30);
                var p = Actor(owner);
                var coin = (SSW.CoinCast)p.Cast.Weapon;
                var chances = new System.Collections.Generic.List<float>();
                for (int i = 1; i <= 6; i++)
                {
                    Check(!coin.Jackpot, "each tested roll starts outside 777");
                    Roll(p, Seed(30f, 33f));
                    chances.Add(coin.Probability);
                    Check(coin.Jackpot && Near(coin.Probability, UnityEngine.Mathf.Min(10f, 5f + i)), "actual 777 increments future chance and respects ten-percent cap " + i);
                    if (i < 6)
                    {
                        Set(coin, "_jackpotUntil", UnityEngine.Time.time - 0.1f);
                        await Until(() => Near(p.Health.Max, p.Stats.Health), 1f, "controlled clock expiry processed by ServerTick");
                    }
                }
                cases.Add(new { stage, chances = chances.ToArray(), hook = "Six actual valid Roll outcomes; only between-roll jackpot timer expiry is accelerated. Natural 15-second expiry is covered separately. Active-777 stacks are not retested." });
                await Reset(owner, SSW.PlayerJob.Gambler, 30);
                p = Actor(owner); coin = (SSW.CoinCast)p.Cast.Weapon;
                Check(Near(coin.Probability, 5f) && Near((float)Read(coin, "_probability"), 0f), "fresh respawn and restored card clear accumulated chance");
            });
            await Case(peer + " ID31 RaiseTheStakes", async () => {
                await Reset(owner, SSW.PlayerJob.Gambler, 31);
                var p = Actor(owner); var t = Other(owner);
                var coin = (SSW.CoinCast)p.Cast.Weapon;
                float baseline = await Fire(p, t);
                var damage = new System.Collections.Generic.List<float>();
                var stacks = new System.Collections.Generic.List<int>();
                for (int i = 1; i <= 4; i++)
                {
                    Roll(p, Seed(8f, 13f));
                    float amount = await Fire(p, t);
                    damage.Add(amount); stacks.Add(coin.Stacks);
                    Check(coin.Stacks == System.Math.Min(3, i) && Near(amount, p.Stats.Damage * (1f + System.Math.Min(3, i) * 0.1f), 0.06f), "successful roll raises actual damage up to three stacks " + i);
                }
                Roll(p, Seed(60f, 90f));
                float failed = await Fire(p, t);
                Check(coin.Stacks == 0 && Near(failed, p.Stats.Damage), "failed result clears stacks and actual bonus damage");
                await Replicate(t, "hp", t.Health.Current);
                Roll(p, Seed(8f, 13f));
                await Reset(owner, SSW.PlayerJob.Gambler, 31);
                p = Actor(owner); t = Other(owner); coin = (SSW.CoinCast)p.Cast.Weapon;
                float respawned = await Fire(p, t);
                cases.Add(new { stage, baseline, damage = damage.ToArray(), stacks = stacks.ToArray(), failed, respawned });
                Check(coin.Stacks == 0 && Near(respawned, p.Stats.Damage), "fresh respawn and restored card clear previous stacks");
            });
        }
        Check(selectedCases > 0, "filter selected at least one case");
        for (int i = 0; i < filters.Length; i++) Check(matched[i], "filter matched " + filters[i]);
    }
    catch (System.Exception error) { fatal = error.ToString(); }
    finally
    {
        try
        {
            if (UnityEditor.EditorApplication.isPlaying && game != null && game.Manager.IsServer)
            {
                Restore();
                await Until(() => System.Linq.Enumerable.All(game.Players, p => Client(p) != null), 8f, "restored player replication");
            }
        }
        catch (System.Exception error) { fatal = (fatal ?? "") + "\nCleanup: " + error; }
        UnityEditor.SessionState.SetBool("Augments.Effects.Running", false);
        stage = "finished";
        Save(fatal == null && failures.Count == 0 ? "completed" : "failed", fatal ?? (failures.Count > 0 ? "One or more cases failed; inspect failures." : null));
    }
}
_ = Run();
return new { running = true, log, dispatch = "seeded real server Roll plus actual JobCast.Apply projectile collisions; both ownership directions", estimatedSeconds = 220 };
