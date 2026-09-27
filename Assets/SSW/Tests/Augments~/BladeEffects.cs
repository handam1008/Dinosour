var game = SSW.NetGame.Current;
if (!UnityEditor.EditorApplication.isPlaying || game == null || !game.Manager.IsServer || game.Players.Count != 2 || game.State.Phase != SSW.MatchPhase.Playing)
    throw new System.InvalidOperationException("Requires an editor host, one connected client and Playing phase.");
if (UnityEditor.SessionState.GetBool("Augments.Effects.Running", false))
    throw new System.InvalidOperationException("Another augment effect script is still running.");
string root = System.IO.Path.GetFullPath("@ROOT@");
string log = System.IO.Path.Combine(root, "BladeEffects.json");
string clientPath = System.IO.Path.Combine(root, "client.json");
if (!System.IO.File.Exists(clientPath))
    throw new System.InvalidOperationException("Use the active NetProbe log root containing client.json.");
if (System.IO.File.Exists(log))
    throw new System.InvalidOperationException("Use a fresh BladeEffects result path.");
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
        status, stage, error, started = began, now = UnityEngine.Time.time,
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
    typeof(SSW.Health).GetMethod("SetMax", flags).Invoke(p.Health, new object[] { maximum });
    p.Health.Heal(maximum);
}
void SetHealth(SSW.NetPlayer p, float health)
{
    p.Health.Heal(p.Health.Max);
    if (p.Health.Current > health) p.Health.TakeDamage(p.Health.Current - health);
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
        Max(p, 1000f);
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

async System.Threading.Tasks.Task<float> Dash(SSW.NetPlayer p)
{
    await Ready(p, SSW.CastKind.Cycle);
    float x = p.Body.position.x;
    float start = UnityEngine.Time.time;
    Check(Cast(p, SSW.CastKind.Cycle, UnityEngine.Vector2.right), "real dash accepted");
    float expectedDuration = (float)Read(p.Cast.Weapon, "_dashUntil") - start;
    float peak = 0f;
    await Until(() => {
        peak = UnityEngine.Mathf.Max(peak, UnityEngine.Mathf.Abs(p.Velocity.x));
        return ((SSW.MotionState)Read(p.Drive, "_state")).DashTime <= 0f;
    }, expectedDuration + 2f, "actual dash motion finished");
    float distance = p.Body.position.x - x;
    cases.Add(new { stage, owner = p.OwnerClientId, type = "dash motion", duration = UnityEngine.Time.time - start, plannedDuration = expectedDuration, distance, peak, speed = p.Stats.DashSpeed });
    Check(peak >= p.Stats.DashSpeed * 0.8f && distance > 0.1f, "dash changes physical position at expected speed");
    return distance;
}
void Face(SSW.NetPlayer target, float sign)
{
    var visual = (UnityEngine.Transform)Read(target.Motion, "_visual");
    var scale = visual.localScale;
    scale.x = UnityEngine.Mathf.Abs(scale.x) * sign;
    visual.localScale = scale;
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
            await Case(peer + " ID42 Ambush", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, 42);
                var p = Actor(owner); var t = Other(owner);
                float start = UnityEngine.Time.time;
                float dealt = await Hit(p, t);
                float ready = (float)Read(p.Cast.Weapon, "_hideAt");
                double end = ((Unity.Netcode.NetworkVariable<double>)Read(p.Effects, "_hide")).Value;
                cases.Add(new { stage, dealt, baseDamage = p.Stats.Damage, cooldown = ready - start, remaining = end - game.ServerTime, state = Snap(p) });
                Check(Near(dealt, p.Stats.Damage) && p.Effects.Hidden, "accepted hit starts concealment");
                Check(Near(ready - start, 5f, 0.12f) && end - game.ServerTime > 0.6d && end - game.ServerTime <= 0.81d, "hide lasts 0.8 seconds with five-second start cooldown");
                await ReplicateFlag(p, "hidden", true);
                await At(start + 1f);
                Check(!p.Effects.Hidden, "concealment expires naturally");
                await ReplicateFlag(p, "hidden", false);
                await Hit(p, t);
                Check(!p.Effects.Hidden && Near((float)Read(p.Cast.Weapon, "_hideAt"), ready), "another hit inside cooldown does not restart concealment");
                await At(ready + 0.08f);
                await Hit(p, t);
                Check(p.Effects.Hidden && (float)Read(p.Cast.Weapon, "_hideAt") > ready, "concealment reactivates after real cooldown");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID43 Backstab", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, 43);
                var p = Actor(owner); var t = Other(owner);
                await Ready(p, SSW.CastKind.Press);
                Face(t, -1f);
                float front = await Hit(p, t);
                await Ready(p, SSW.CastKind.Press);
                Face(t, 1f);
                float back = await Hit(p, t);
                cases.Add(new { stage, hook = "Target visual facing set immediately before synchronous melee; damage uses real SO angle test.", front, back, baseDamage = p.Stats.Damage });
                Check(Near(front, p.Stats.Damage) && Near(back, p.Stats.Damage * 1.4f), "front baseline and rear 1.4 multiplier use actual melee");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID44 Blackout", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, 44);
                var p = Actor(owner); var t = Other(owner);
                float start = UnityEngine.Time.time;
                Check(Cast(p, SSW.CastKind.Cycle, UnityEngine.Vector2.up), "real knife skill accepted");
                float ready = (float)Read(p.Cast.Weapon, "_blindAt");
                double end = ((Unity.Netcode.NetworkVariable<double>)Read(t.Effects, "_dark")).Value;
                cases.Add(new { stage, cooldown = ready - start, remaining = end - game.ServerTime, target = Snap(t) });
                Check(t.Effects.BlindActive && Near(ready - start, 17.5f, 0.08f) && end - game.ServerTime > 2.3d, "skill starts 2.5-second blind and 17.5-second cooldown");
                await ReplicateFlag(t, "blind", true);
                ClearShots();
                await At(start + 2.7f);
                Check(!t.Effects.BlindActive, "blind expires naturally");
                await ReplicateFlag(t, "blind", false);
                await Ready(p, SSW.CastKind.Cycle);
                Check(UnityEngine.Time.time < ready, "base skill allows an attempt during blackout cooldown");
                Check(Cast(p, SSW.CastKind.Cycle, UnityEngine.Vector2.up), "second real skill accepted");
                Check(!t.Effects.BlindActive && Near((float)Read(p.Cast.Weapon, "_blindAt"), ready), "cooldown blocks another blind");
                ClearShots();
                await At(ready + 0.08f);
                await Ready(p, SSW.CastKind.Cycle);
                Check(Cast(p, SSW.CastKind.Cycle, UnityEngine.Vector2.up) && t.Effects.BlindActive, "blind returns after real cooldown");
                await ReplicateFlag(t, "blind", true);
                ClearShots();
            });
            await Case(peer + " ID45 Concealed", async () => {
                var outcomes = new System.Collections.Generic.List<object>();
                foreach (int card in new[] { -1, 45 })
                {
                    await Reset(owner, SSW.PlayerJob.Assassin, card);
                    var p = Actor(owner); var t = Other(owner);
                    p.Drive.Teleport(new UnityEngine.Vector2(0f, 6f));
                    t.Drive.Teleport(new UnityEngine.Vector2(12f, 5f));
                    int layer = 0;
                    while (layer < 31 && (p.GroundMask.value & (1 << layer)) == 0) layer++;
                    var walls = new System.Collections.Generic.List<UnityEngine.GameObject>();
                    try
                    {
                        foreach (float x in new[] { -2f, 2f })
                        {
                            var wall = new UnityEngine.GameObject("Effect test wall");
                            wall.layer = layer;
                            wall.transform.position = new UnityEngine.Vector3(x, 8f, 0f);
                            wall.AddComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(0.3f, 12f);
                            walls.Add(wall); temporary.Add(wall);
                        }
                        UnityEngine.Physics2D.SyncTransforms();
                        Check(Cast(p, SSW.CastKind.Cycle, UnityEngine.Vector2.right), "knife actually spawned");
                        var knife = (SSW.NetBolt)Read(p.Cast.Weapon, "_knife");
                        Check(knife != null && knife.IsSpawned, "authoritative knife exists");
                        int initial = (int)Read(knife, "_bounces");
                        if (card == 45)
                        {
                            await Until(() => knife != null && knife.IsSpawned && (int)Read(knife, "_bounces") == 0, 3f, "first wall bounce");
                            float reflected = knife.Velocity.x;
                            Check(initial == 1 && reflected < 0f && !(bool)Read(knife, "_stuck"), "first wall consumes exactly one bounce and reverses trajectory");
                            await Until(() => knife != null && knife.IsSpawned && (bool)Read(knife, "_stuck"), 4f, "second wall stick");
                            Check(knife.Position.x < 0f && knife.Velocity.sqrMagnitude < 0.01f, "second wall stops the reflected knife");
                            outcomes.Add(new { card, initial, reflected, finalX = knife.Position.x, finalY = knife.Position.y, stuck = true });
                        }
                        else
                        {
                            await Until(() => knife != null && knife.IsSpawned && (bool)Read(knife, "_stuck"), 3f, "baseline first wall stick");
                            Check(initial == 0 && knife.Position.x > 0f, "baseline knife sticks at first wall");
                            outcomes.Add(new { card, initial, finalX = knife.Position.x, finalY = knife.Position.y, stuck = true });
                        }
                    }
                    finally
                    {
                        ClearShots();
                        foreach (var wall in walls) { temporary.Remove(wall); UnityEngine.Object.Destroy(wall); }
                    }
                    await Wait(0.08f);
                }
                cases.Add(new { stage, hook = "Two temporary server-only collision walls; authoritative bounce test, not client scenery/prediction validation.", outcomes });
            });
            await Case(peer + " ID47 ExploitWeakness", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, -1);
                var baseline = Actor(owner); var blocker = Other(owner);
                Check(Cast(blocker, SSW.CastKind.Parry, UnityEngine.Vector2.left), "control target parry starts");
                float controlHp = blocker.Health.Current;
                Check(Cast(baseline, SSW.CastKind.Press, UnityEngine.Vector2.right), "control melee accepted");
                Check(Near(blocker.Health.Current, controlHp), "unaugmented melee is blocked");
                await Reset(owner, SSW.PlayerJob.Assassin, 47);
                var p = Actor(owner); var t = Other(owner);
                Check(Cast(t, SSW.CastKind.Parry, UnityEngine.Vector2.left), "target parry starts");
                float start = UnityEngine.Time.time;
                float first = await Hit(p, t);
                await Wait(t.Stats.ParryTime + 0.05f);
                float second = await Hit(p, t);
                Check(UnityEngine.Time.time < start + 8f, "comparison is inside eight-second cooldown");
                await At(start + 8.08f);
                await Ready(t, SSW.CastKind.Parry);
                Check(Cast(t, SSW.CastKind.Parry, UnityEngine.Vector2.left), "parry starts after cooldown");
                float third = await Hit(p, t);
                cases.Add(new { stage, first, second, third, baseDamage = p.Stats.Damage, elapsed = UnityEngine.Time.time - start });
                Check(Near(first, p.Stats.Damage * 1.2f) && Near(second, p.Stats.Damage) && Near(third, p.Stats.Damage * 1.2f), "actual parry bypass, 1.2 multiplier, cooldown baseline and reactivation");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID48 FinishingBlow", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, 48);
                var p = Actor(owner); var t = Other(owner);
                float before = await Hit(p, t);
                Check(Cast(p, SSW.CastKind.Cycle, UnityEngine.Vector2.up), "real knife skill primes the next basic hit");
                ClearShots();
                float primed = await Hit(p, t);
                float consumed = await Hit(p, t);
                cases.Add(new { stage, before, primed, consumed, baseDamage = p.Stats.Damage });
                Check(Near(before, p.Stats.Damage) && Near(primed, p.Stats.Damage * 1.3f) && Near(consumed, p.Stats.Damage), "skill bonus applies to exactly one actual basic strike");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID50 Lure", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, 50);
                var p = Actor(owner); var t = Other(owner);
                float full = await Hit(p, t);
                float hurt = await Hit(p, t);
                t.Health.Heal(t.Health.Max);
                float restored = await Hit(p, t);
                cases.Add(new { stage, full, hurt, restored, baseDamage = p.Stats.Damage });
                Check(Near(full, p.Stats.Damage * 1.2f) && Near(hurt, p.Stats.Damage) && Near(restored, p.Stats.Damage * 1.2f), "full-health bonus follows actual target HP");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID51 NoEscape", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, 51);
                var p = Actor(owner); var t = Other(owner);
                float start = UnityEngine.Time.time;
                await Hit(p, t);
                float firstEnd = (float)Read(t.Motion, "_slowEndTime");
                cases.Add(new { stage, duration = firstEnd - start, state = Snap(t) });
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 0.3f) && Near(firstEnd - start, 0.5f, 0.08f), "seventy-percent reduction lasts half a second");
                await Replicate(t, "speed", t.Stats.MoveSpeed * 0.3f);
                await At(start + 0.7f);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 1f), "slow expires naturally");
                await Hit(p, t);
                Check(UnityEngine.Time.time < start + 4.5f && Near(t.Motion.CurrentMoveSpeedMultiplier, 1f), "cooldown prevents reapplying slow");
                await At(start + 4.6f);
                await Hit(p, t);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 0.3f), "slow returns after duration plus four-second cooldown");
                await Replicate(t, "speed", t.Stats.MoveSpeed * 0.3f);
            });
            await Case(peer + " ID52 TacticalShift", async () => {
                await Reset(owner, SSW.PlayerJob.Assassin, 52, SSW.PlayerJob.Assassin);
                var p = Actor(owner); var t = Other(owner);
                SetHealth(p, p.Health.Max * 0.3f + t.Stats.Damage * 0.5f);
                Check(!(bool)Read(p.Cast.Weapon, "_shifted"), "controlled setup remains above thirty percent");
                float start = UnityEngine.Time.time;
                float trigger = await Hit(t, p);
                float ownEnd = (float)Read(p.Cast.Weapon, "_shiftUntil");
                float enemyEnd = (float)Read(t.Motion, "_slowEndTime");
                float boosted = await Hit(p, t);
                cases.Add(new { stage, hook = "Nonlethal HP setup just above threshold; actual enemy melee crosses it.", trigger, boosted, ownEnd = ownEnd - start, enemyEnd = enemyEnd - start, actor = Snap(p), target = Snap(t) });
                Check(p.Health.Current <= p.Health.Max * 0.3f && p.Health.Current > 0f, "real hit crosses nonlethal threshold");
                Check(Near(p.Motion.CurrentMoveSpeedMultiplier, 0.3f) && Near(t.Motion.CurrentMoveSpeedMultiplier, 0.5f) && Near(boosted, p.Stats.Damage * 1.5f), "own slow, nearby enemy slow and actual bonus damage");
                Check(Near(ownEnd - start, 10f, 0.12f) && Near(enemyEnd - start, 7f, 0.12f), "ten- and seven-second durations start at threshold");
                await Replicate(p, "speed", p.Stats.MoveSpeed * 0.3f);
                await Replicate(t, "speed", t.Stats.MoveSpeed * 0.5f);
                await At(enemyEnd + 0.1f);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 1f) && Near(p.Motion.CurrentMoveSpeedMultiplier, 0.3f), "enemy slow expires before own effect");
                await At(ownEnd + 0.1f);
                Check(Near(p.Motion.CurrentMoveSpeedMultiplier, 1f) && Near(p.Cast.Weapon.DamageScale, 1f), "own speed and outgoing damage expire naturally");
                float normal = await Hit(p, t);
                Check(Near(normal, p.Stats.Damage), "actual damage returns to baseline");
                SetHealth(p, p.Health.Max * 0.3f + t.Stats.Damage * 0.5f);
                await Hit(t, p);
                Check(Near((float)Read(p.Cast.Weapon, "_shiftUntil"), ownEnd) && Near(p.Motion.CurrentMoveSpeedMultiplier, 1f), "another threshold crossing cannot refresh once-per-life effect");
                await Replicate(p, "speed", p.Stats.MoveSpeed);
                await Reset(owner, SSW.PlayerJob.Assassin, 52, SSW.PlayerJob.Assassin);
                p = Actor(owner); t = Other(owner);
                SetHealth(p, p.Health.Max * 0.3f + t.Stats.Damage * 0.5f);
                await Hit(t, p);
                Check((bool)Read(p.Cast.Weapon, "_shifted") && Near(p.Cast.Weapon.DamageScale, 1.5f), "fresh respawn and restored card reset once-per-life availability");
            });
            await Case(peer + " ID53 ParryHeal", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 53, SSW.PlayerJob.Assassin);
                var p = Actor(owner); var t = Other(owner);
                SetHealth(p, p.Health.Max - 30f);
                var amounts = new System.Collections.Generic.List<float>();
                var times = new System.Collections.Generic.List<float>();
                float start = UnityEngine.Time.time;
                System.Action<float> onHeal = amount => { amounts.Add(amount); times.Add(UnityEngine.Time.time - start); };
                p.Health.OnHealed += onHeal;
                try
                {
                    float before = p.Health.Current;
                    Check(Cast(p, SSW.CastKind.Parry, UnityEngine.Vector2.right), "real parry accepted");
                    Check(Cast(t, SSW.CastKind.Press, UnityEngine.Vector2.left), "enemy real melee accepted");
                    Check(Near(p.Health.Current, before), "parry blocks actual incoming melee");
                    await Wait(1.5f);
                    float after = p.Health.Current;
                    cases.Add(new { stage, before, after, amounts = amounts.ToArray(), times = times.ToArray() });
                    Check(amounts.Count == 5 && System.Linq.Enumerable.All(amounts, amount => Near(amount, 2f)) && Near(after - before, 10f), "five real healing events restore ten HP");
                    Check(times[0] >= 0.18f && times[0] < 0.5f, "first heal waits 0.2 seconds");
                    for (int i = 1; i < times.Count; i++) Check(times[i] - times[i - 1] >= 0.18f && times[i] - times[i - 1] < 0.5f, "heal tick spacing " + i);
                    await Wait(0.4f);
                    Check(amounts.Count == 5 && Near(p.Health.Current, after), "healing stops after five ticks");
                    await Replicate(p, "hp", p.Health.Current);
                }
                finally { p.Health.OnHealed -= onHeal; }
            });
            await Case(peer + " ID54 DashSpeed", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 54);
                var p = Actor(owner); var t = Other(owner);
                await Place(p, t, 20f);
                float start = UnityEngine.Time.time;
                float distance = await Dash(p);
                float end = (float)Read(p.Motion, "_speedEndTime");
                cases.Add(new { stage, distance, duration = end - start, actor = Snap(p) });
                Check(Near(p.Motion.CurrentMoveSpeedMultiplier, 1.5f) && Near(end - start, p.Stats.DashTime + 3f, 0.15f), "actual dash grants fifty-percent speed for dash duration plus three seconds");
                await Replicate(p, "speed", p.Stats.MoveSpeed * 1.5f);
                await At(end + 0.1f);
                Check(Near(p.Motion.CurrentMoveSpeedMultiplier, 1f), "haste expires naturally");
                await Replicate(p, "speed", p.Stats.MoveSpeed);
            });
            await Case(peer + " ID55 DashRange", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, -1);
                var p = Actor(owner); var t = Other(owner);
                await Place(p, t, 20f);
                float control = await Dash(p);
                await Reset(owner, SSW.PlayerJob.Swordsman, 55);
                p = Actor(owner); t = Other(owner);
                await Place(p, t, 20f);
                float extended = await Dash(p);
                cases.Add(new { stage, control, extended, ratio = extended / control });
                Check(extended > control && Near(extended / control, 1.5f, 0.22f), "actual dash travel increases by approximately fifty percent");
            });
            await Case(peer + " ID56 DashPower", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 56, SSW.PlayerJob.Assassin);
                var p = Actor(owner); var t = Other(owner);
                float before = p.Health.Current;
                Check(Cast(p, SSW.CastKind.Parry, UnityEngine.Vector2.right), "real parry accepted");
                Check(Cast(t, SSW.CastKind.Press, UnityEngine.Vector2.left), "real incoming melee accepted");
                Check(Near(p.Health.Current, before) && (bool)Read(p.Cast.Weapon, "_boosted"), "blocked melee primes dash");
                await Place(p, t, 3f);
                float targetBefore = t.Health.Current;
                await Dash(p);
                float first = targetBefore - t.Health.Current;
                Check(Near(first, p.Stats.DashDamage * 2f) && !(bool)Read(p.Cast.Weapon, "_boosted"), "actual dash collision consumes double damage once");
                await Ready(p, SSW.CastKind.Cycle);
                await Place(p, t, 3f);
                targetBefore = t.Health.Current;
                await Dash(p);
                float second = targetBefore - t.Health.Current;
                cases.Add(new { stage, first, second, baseDamage = p.Stats.DashDamage });
                Check(Near(second, p.Stats.DashDamage), "next unprimed dash deals baseline damage");
                await Replicate(t, "hp", t.Health.Current);
            });
        }
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
return new { running = true, log, dispatch = "server JobCast.Apply; both ownership directions; caller polls status", estimatedSeconds = 180 };
