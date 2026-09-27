var game = SSW.NetGame.Current;
if (!UnityEditor.EditorApplication.isPlaying || game == null || !game.Manager.IsServer || game.Players.Count != 2 || game.State.Phase != SSW.MatchPhase.Playing)
    throw new System.InvalidOperationException("Requires an editor host, one connected client and Playing phase.");
if (UnityEditor.SessionState.GetBool("Augments.Effects.Running", false))
    throw new System.InvalidOperationException("Another augment effect script is still running.");
string root = System.IO.Path.GetFullPath("@ROOT@");
string log = System.IO.Path.Combine(root, "SwordLinks.json");
string clientPath = System.IO.Path.Combine(root, "client.json");
string filter = "@FILTER@";
if (!System.IO.File.Exists(clientPath))
    throw new System.InvalidOperationException("Use the active NetProbe log root containing client.json.");
if (System.IO.File.Exists(log))
    throw new System.InvalidOperationException("Use a fresh SwordLinks result path.");
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
    hidden = p.Effects.Hidden, blind = p.Effects.BlindActive, damageScale = p.Cast.Weapon != null ? p.Cast.Weapon.DamageScale : 1f,
    tick = p.InputSequence, ready = p.Cast.Weapon != null ? p.Cast.Weapon.Status.Ready : 0, skill = p.Cast.Weapon != null ? p.Cast.Weapon.Status.Skill : 0
};
void Save(string status, string error = null)
{
    string json = Newtonsoft.Json.JsonConvert.SerializeObject(new {
        status, stage, error, started = began, now = UnityEngine.Time.time,
        dispatch = "Server JobCast.Apply on host-owned and client-owned actors; not native client input.",
        isolation = "Caller supplied isolate and wideground at (0,3) on both peers; no asset edits.",
        count = checks.Count, checks, cases, failures, replicas,
        scope = "six added sword cards and reported gameplay regressions"
    }, Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText(log, json);
}
async System.Threading.Tasks.Task Wait(float seconds)
{
    float end = UnityEngine.Time.time + UnityEngine.Mathf.Max(0f, seconds);
    double watchdog = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds + 12d;
    while (UnityEngine.Time.time < end)
    {
        if (!UnityEditor.EditorApplication.isPlaying || game == null || UnityEngine.Time.realtimeSinceStartupAsDouble > watchdog)
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
    if (filter.Length > 0 && !filter.Split(',').Any(value => label.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)) return;
    stage = label;
    Save("running");
    try { await test(); checks.Add(stage + ": completed"); }
    catch (System.Exception error)
    {
        failures.Add(new { stage, error = error.ToString(), players = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(game.Players, p => Snap(p))), client = Peer() });
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
Newtonsoft.Json.Linq.JObject latestPeer = null;
Newtonsoft.Json.Linq.JObject Peer()
{
    try { latestPeer = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(clientPath)); }
    catch (System.IO.IOException) { }
    catch (Newtonsoft.Json.JsonReaderException) { }
    return latestPeer;
}
async System.Threading.Tasks.Task SendClient(string op, int value = 0, float x = 0f, float y = 0f)
{
    int seq = (int)Peer()["seq"] + 1;
    System.IO.File.WriteAllText(root + "/client.cmd.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { seq, op, value, x, y }));
    await Until(() => (int)Peer()["seq"] >= seq, 5f, "client command " + op);
}
void Phase(SSW.MatchPhase phase)
{
    var state = game.Match.State;
    state.Phase = phase;
    ((Unity.Netcode.NetworkVariable<SSW.MatchState>)Read(game.Match, "_state")).Value = state;
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
            await Case(peer + " ID86 parry cooldown", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 86);
                var p = Actor(owner); var t = Other(owner);
                await Dash(p);
                await Place(p, t);
                uint before = p.Cast.Weapon.Status.Skill;
                Check(Cast(p, SSW.CastKind.Parry, UnityEngine.Vector2.right), "parry accepted");
                Check(Cast(t, SSW.CastKind.Press, UnityEngine.Vector2.left), "incoming strike accepted");
                uint after = p.Cast.Weapon.Status.Skill;
                Check(Near((before - after) * UnityEngine.Time.fixedDeltaTime, 2f), "successful parry subtracts two seconds");
                await Replicate(p, "skillReady", after);
            });
            await Case(peer + " ID87 bleed", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 87);
                var p = Actor(owner); var t = Other(owner);
                await Place(p, t, 3f);
                float hp = t.Health.Current;
                await Dash(p);
                Check(Near(hp - t.Health.Current, p.Stats.DashDamage), "bleed waits for first interval");
                await Wait(0.95f);
                Check(Near(hp - t.Health.Current, p.Stats.DashDamage + 3f), "first bleed tick at one second");
                await Wait(1f);
                Check(Near(hp - t.Health.Current, p.Stats.DashDamage + 6f), "second bleed tick applies once");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID88 root", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 88);
                var p = Actor(owner); var t = Other(owner);
                await Place(p, t, 3f);
                await Dash(p);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 0f), "dash roots target");
                await Wait(0.5f);
                Check(Near(t.Motion.CurrentMoveSpeedMultiplier, 1f), "root expires");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " ID89 recovery", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 89);
                var p = Actor(owner); var t = Other(owner);
                SetHealth(p, p.Health.Max * 0.4f);
                float hp = p.Health.Current;
                await Hit(p, t);
                Check(Near(hp, p.Health.Current), "basic attack before dash does not heal");
                t.Health.Heal(t.Health.Max);
                await Place(p, t, 3f);
                float targetHp = t.Health.Current;
                await Dash(p);
                float scale = (float)Read(p.Cast.Weapon, "_dashDamage");
                Check(Near(scale, p.Stats.DashDamage), "first dash has baseline damage");
                Check(Near(p.Health.Current - hp, (targetHp - t.Health.Current) * 0.3f), "dash immediately heals thirty percent of actual damage");
                hp = p.Health.Current;
                await Place(p, t);
                await Hit(p, t);
                Check(Near(p.Health.Current, hp), "later basic attack does not repeat dash healing");
                var effects = Read(p.Cast.Weapon, "_effects");
                float bonus = (float)effects.GetType().GetProperty("DashScale", flags).GetValue(effects);
                Check(Near(bonus, 1.15f), "dash follow-up bonus is active");
                await Wait(4.1f);
                bonus = (float)effects.GetType().GetProperty("DashScale", flags).GetValue(effects);
                Check(Near(bonus, 1f), "dash follow-up bonus expires");
                await Replicate(p, "hp", p.Health.Current);
            });
            await Case(peer + " ID90 standing heal", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 90);
                var p = Actor(owner); var t = Other(owner);
                await Place(p, t, 4f);
                SetHealth(p, p.Health.Max * 0.5f);
                p.Drive.ApplyForce(UnityEngine.Vector2.right * 3f, UnityEngine.ForceMode2D.Impulse);
                await Until(() => p.Velocity.sqrMagnitude > 0.1f, 2f, "impulse applied on physics tick");
                await Until(() => p.Velocity.sqrMagnitude < 0.001f, 3f, "standing after motion");
                float hp = p.Health.Current;
                await Wait(0.75f);
                Check(Near(p.Health.Current, hp), "standing delay prevents early heal");
                await Wait(1.4f);
                Check(p.Health.Current >= hp + 2f && p.Health.Current <= hp + 3f, "standing produces timed healing");
                await Replicate(p, "hp", p.Health.Current);
            });
            await Case(peer + " ID91 sword growth", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, -1);
                var p = Actor(owner); var t = Other(owner);
                await Place(p, t, 3.2f);
                float hp = t.Health.Current;
                Check(Cast(p, SSW.CastKind.Press, UnityEngine.Vector2.right), "baseline attack accepted");
                await Wait(0.25f);
                Check(Near(t.Health.Current, hp), "baseline attack cannot reach distant target");
                p.Draft.Restore(new[] { 91 });
                await Wait(1.1f);
                Check(((SSW.SwordCast)p.Cast.Weapon).ReachScale > 1.09f, "growth advances during combat");
                await Until(() => Near(((SSW.SwordCast)p.Cast.Weapon).ReachScale, 3f), 23f, "growth reaches cap naturally");
                await Replicate(p, "swordScale", 3f);
                float damage = await Hit(p, t);
                Check(Near(damage, p.Stats.Damage), "grown melee reaches target without multiplying damage");
                await Wait(0.3f);
                Check(Near(((SSW.SwordCast)p.Cast.Weapon).ReachScale, 3f), "growth stays capped");
                await Reset(owner, SSW.PlayerJob.Swordsman, 91);
                Check(((SSW.SwordCast)Actor(owner).Cast.Weapon).ReachScale < 1.2f, "respawn restarts growth while retaining card");
            });
            await Case(peer + " recovery combo", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, 89);
                var p = Actor(owner); var t = Other(owner);
                p.Draft.Restore(new[] { 86, 56 });
                await Place(p, t, 3f);
                await Until(() => (uint)Client(p)["epoch"] == p.Epoch, 3f, "combo starting position reaches owner");
                await Dash(p);
                await Wait(0.15f);
                uint ready = p.Cast.Weapon.Status.Skill;
                Check(Cast(p, SSW.CastKind.Parry, UnityEngine.Vector2.left), "combo parry accepted");
                Check(Cast(t, SSW.CastKind.Press, UnityEngine.Vector2.right), "combo incoming strike accepted");
                await Until(() => (bool)Read(p.Cast.Weapon, "_boosted"), 1f, "incoming strike is actually parried");
                var blocked = SSW.CombatDamage.Deal(t.Cast.Weapon, p.Health, 5f, SSW.DamageTag.Projectile);
                Check(blocked.WasBlocked, "second projectile damage fixture is parried within same window");
                cases.Add(new { stage, dispatch = "second projectile impact injected through CombatDamage for overlapping hit combination" });
                Check(p.Cast.Weapon.Status.Skill < ready, "parry reduces active dash cooldown");
                t.Drive.Teleport(p.Body.position + new UnityEngine.Vector2(3f, 1f));
                await Ready(p, SSW.CastKind.Cycle);
                t.Health.Heal(t.Health.Max);
                float hp = t.Health.Current;
                var effects = Read(p.Cast.Weapon, "_effects");
                cases.Add(new { stage, at = "before combo", boost = Read(p.Cast.Weapon, "_boosted"), bonusAt = Read(effects, "_bonusAt"), bonusUntil = Read(effects, "_bonusUntil"), now = UnityEngine.Time.time });
                await Dash(p);
                cases.Add(new { stage, at = "after combo", damage = Read(p.Cast.Weapon, "_dashDamage"), hp, actual = t.Health.Current, now = UnityEngine.Time.time });
                Check(Near(hp - t.Health.Current, p.Stats.DashDamage * 2f * 1.15f), "parry and recovery bonuses combine on real dash hit");
                await Replicate(t, "hp", t.Health.Current);
            });
            await Case(peer + " gun draft pause", async () => {
                await Reset(owner, SSW.PlayerJob.Gunner, -1);
                var p = Actor(owner);
                p.Draft.Deal();
                Phase(SSW.MatchPhase.Draft);
                await Wait(0.3f);
                int ammo = p.Cast.Weapon.Status.Ammo;
                await Wait(p.Stats.Reload * 2f + 0.5f);
                Check(p.Cast.Weapon.Status.Ammo == ammo, "server does not reload during draft");
                Check((int)Client(p)["ammo"] == ammo, "client forecast does not reload during draft");
                foreach (var actor in game.Players) actor.Draft.Restore(System.Array.Empty<int>());
                Phase(SSW.MatchPhase.Playing);
                await Wait(0.15f);
                Check(p.Cast.Weapon.Status.Ammo == ammo, "resume does not catch up draft time");
                await Until(() => p.Cast.Weapon.Status.Ammo > ammo, p.Stats.Reload + 1f, "reload resumes in combat");
            });
            await Case(peer + " damage number anchor", async () => {
                await Reset(owner, SSW.PlayerJob.Swordsman, -1);
                var p = Actor(owner);
                var decoy = new UnityEngine.GameObject("Distant effect fixture");
                temporary.Add(decoy);
                decoy.transform.SetParent(p.transform);
                decoy.transform.localPosition = new UnityEngine.Vector3(-100f, 30f, 0f);
                var body = p.GetComponentInChildren<SSW.DinosaurVisualController>().GetComponent<UnityEngine.SpriteRenderer>();
                decoy.AddComponent<UnityEngine.SpriteRenderer>().sprite = body.sprite;
                await Wait(1f);
                p.Health.TakeDamage(5f);
                var numbers = UnityEngine.Object.FindObjectsByType<SSW.DamageNumberDisplay>();
                Check(numbers.Length > 0, "damage number spawned");
                Check(System.Linq.Enumerable.Any(numbers, n => UnityEngine.Mathf.Abs(n.transform.position.x - body.bounds.center.x) < 0.5f
                    && UnityEngine.Mathf.Abs(n.transform.position.y - body.bounds.max.y) < 0.5f), "number stays above body despite distant child renderer");
                await Until(() => System.Linq.Enumerable.Any(Peer()["damageNumbers"], n => UnityEngine.Mathf.Abs((float)n["x"] - p.View.position.x) < 1f), 2f, "client number stays by rendered body");
            });
        }
        await Case("new sword card selection", async () => {
            ulong owner = game.Manager.LocalClientId;
            await Reset(owner, SSW.PlayerJob.Swordsman, -1);
            var p = Actor(owner);
            var deck = UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");
            p.Draft.Restore(System.Linq.Enumerable.Range(0, deck.Count).Where(id => deck.At(id) != null && (deck.IsCommon(deck.At(id)) || id >= 53 && id <= 56)));
            foreach (var other in game.Players) if (other != p) other.Draft.Restore(System.Array.Empty<int>());
            for (int i = 0; i < 6; i++)
            {
                p.Draft.Deal(true);
                Phase(SSW.MatchPhase.Draft);
                await Until(() => p.Draft.HasView && !p.Draft.View.GetComponentsInChildren<SSW.AugmentCardUI>()[0].Locked, 4f, "card animation");
                int id = p.Draft.Offer.x;
                Check(id >= 86 && id <= 91, "offered new sword card");
                var card = p.Draft.View.GetComponentsInChildren<SSW.AugmentCardUI>()[0];
                card.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
                await Until(() => System.Linq.Enumerable.Contains(p.Draft.Owned, id), 4f, "actual card click grants");
                Check(((SSW.SwordCast)p.Cast.Weapon).Has(((SSW.SwordAugment)deck.At(id)).type), "selected card reaches combat receiver");
            }
            Phase(SSW.MatchPhase.Playing);
        });
        await Case("camera water maps", async () => {
            var rotation = game.GetComponent<SSW.MapRotation>();
            foreach (string name in new[] { "Map08", "Map12" })
            {
                int index = System.Linq.Enumerable.ToList(rotation.Prefabs).FindIndex(m => m.name == name);
                Set(rotation, "_index", index);
                rotation.Restart(false);
                await Until(() => game.Arena.Map.name.StartsWith(name) && (ulong)Peer()["mapObject"] == game.Arena.Map.NetworkObjectId, 8f, "map replication");
                await Wait(0.2f);
                Check(Near(game.Arena.View.transform.position.y, -1.5f), name + " host camera center");
                Check(Near((float)Peer()["cameraPosition"]["y"], -1.5f), name + " client camera center");
                foreach (var player in game.Players) player.Drive.Teleport(game.Arena.Map.Spawn(player.Side == 1 ? 0 : 1));
                await Wait(0.4f);
                string hostImage = root + "/" + name + "-host.png";
                UnityEngine.ScreenCapture.CaptureScreenshot(hostImage);
                System.DateTime captureAt = System.DateTime.UtcNow;
                await SendClient("capture");
                await Until(() => System.IO.File.Exists(hostImage) && System.IO.File.Exists(root + "/client.png")
                    && System.IO.File.GetLastWriteTimeUtc(root + "/client.png") >= captureAt, 5f, "both camera screenshots");
                System.IO.File.Copy(root + "/client.png", root + "/" + name + "-client.png");
                foreach (var shape in game.Arena.Map.GetComponentsInChildren<UnityEngine.Collider2D>(true)) shape.enabled = false;
                await SendClient("isolate");
            }
        });
        foreach (var saved in original)
        {
            ulong owner = saved.OwnerClientId;
            await Case("precision fan owner " + owner, async () => {
                await Reset(owner, SSW.PlayerJob.Witch, 19);
                var p = Actor(owner); var t = Other(owner);
                p.Drive.Teleport(new UnityEngine.Vector2(-5f, 5f));
                t.Drive.Teleport(new UnityEngine.Vector2(20f, 5f));
                await Until(() => {
                    var snapshot = Client(p);
                    return snapshot != null && (uint)snapshot["epoch"] == p.Epoch
                        && Near((float)snapshot["position"]["x"], -5f, 0.1f);
                }, 5f, "new position and input epoch reach client");
                await Until(() => p.Cast.Held >= 0 && (int)Client(p)["potion"] >= 0, 5f, "naturally brewed stock reaches client");
                cases.Add(new { stage, at = "before", client = Client(p), serverAction = p.Cast.Action, p.Cast.Shots, p.Cast.Rejections });
                if (p.IsOwner) p.Cast.Attack(true, UnityEngine.Vector2.right);
                else await SendClient("press", 0, 1f, 0f);
                SSW.NetPotion[] Potions() => game.Manager.SpawnManager.SpawnedObjects.Values.Select(o => o.GetComponent<SSW.NetPotion>()).Where(o => o != null && o.Caster == p.NetworkObjectId).ToArray();
                await Until(() => Potions().Length == 3, 3f, "actual throw produces three parts");
                var angles = Potions().Select(o => (UnityEngine.Vector2)Read(o, "_velocity")).Select(v => UnityEngine.Mathf.Atan2(v.y, v.x) * UnityEngine.Mathf.Rad2Deg).OrderBy(v => v).ToArray();
                Check(Near(angles[2] - angles[0], 24f, 0.1f) && Near(angles[1] - angles[0], 12f, 0.1f), "three velocities form symmetric twenty-four degree fan");
                await Until(() => Peer()["shots"].Count(s => (string)s["type"] == "potion" && (ulong)s["caster"] == p.NetworkObjectId) == 3, 2f, "three parts visible on client");
                await Wait(0.18f);
                var positions = Peer()["shots"].Where(s => (string)s["type"] == "potion" && (ulong)s["caster"] == p.NetworkObjectId).Select(s => (float)s["position"]["y"]).ToArray();
                Check(positions.Length == 3 && positions.Max() - positions.Min() > 0.4f, "client flight separates visibly");
                cases.Add(new { stage, angles, positions });
                ClearShots();
            });
        }
        await Case("client surrender", async () => {
            Phase(SSW.MatchPhase.Playing);
            int reports = 0;
            System.Action<SSW.MatchState> ended = _ => reports++;
            game.Ended += ended;
            try
            {
                await SendClient("surrender");
                await Until(() => game.State.Phase == SSW.MatchPhase.Finished && (string)Peer()["phase"] == "Finished", 5f, "both result screens");
                Check(game.State.Reason == SSW.MatchEnd.Surrender && game.State.Winner == game.Manager.LocalClientId, "server accepts client surrender as defeat");
                Check(game.Manager.ConnectedClientsIds.Count == 2 && (bool)Peer()["connected"], "surrender keeps result connection");
                Check((string)Peer()["title"] == "항복했습니다", "client sees surrender result");
                Check(reports == 0, "incomplete match does not submit knockout score");
            }
            finally { game.Ended -= ended; }
        });
    }
    catch (System.Exception error) { fatal = error.ToString(); }
    finally
    {
        try { foreach (var item in temporary) if (item != null) UnityEngine.Object.Destroy(item); }
        catch (System.Exception error) { fatal = (fatal ?? "") + "\nCleanup: " + error; }
        UnityEditor.SessionState.SetBool("Augments.Effects.Running", false);
        stage = "finished";
        Save(fatal == null && failures.Count == 0 ? "completed" : "failed", fatal ?? (failures.Count > 0 ? "Inspect failures." : null));
    }
}
_ = Run();
return new { running = true, log };
