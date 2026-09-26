if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play mode required");
var game = SSW.NetGame.Current;
if (game == null || !game.Manager.IsServer || !game.CanFight || game.Practice != null || game.Players.Count != 2)
    throw new System.InvalidOperationException("A playing Editor host and one connected client are required");
string root = System.IO.Path.GetFullPath(@"@ROOT@");
string allowed = System.IO.Path.GetFullPath(UnityEngine.Application.dataPath + "/../Logs/Boundary") + System.IO.Path.DirectorySeparatorChar;
if (!root.StartsWith(allowed, System.StringComparison.OrdinalIgnoreCase)) throw new System.InvalidOperationException("Use a fresh Logs/Boundary run directory");
System.IO.Directory.CreateDirectory(root);
string log = System.IO.Path.Combine(root, "MagicEffects.json");
if (System.IO.File.Exists(log)) throw new System.InvalidOperationException("Use a fresh run directory");
const string runKey = "augments.magicEffects.running";
if (UnityEditor.SessionState.GetBool(runKey, false)) throw new System.InvalidOperationException("MagicEffects already running");
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
System.Reflection.FieldInfo Field(object item, string name)
{
    for (var type = item.GetType(); type != null; type = type.BaseType)
    {
        var field = type.GetField(name, flags);
        if (field != null) return field;
    }
    throw new System.MissingFieldException(item.GetType().Name, name);
}
T Read<T>(object item, string name) => (T)Field(item, name).GetValue(item);
void Set(object item, string name, object value) => Field(item, name).SetValue(item, value);
object Call(object item, string name, params object[] args) => item.GetType().GetMethod(name, flags).Invoke(item, args);
var saved = game.Players.OrderBy(p => p.OwnerClientId).Select(p => new
{
    p.OwnerClientId, p.Side, p.Info, p.Job, position = p.Body.position, owned = p.Draft.Owned.ToArray(),
    health = p.Health.Current, maximum = p.Health.Max, blocked = Read<bool>(p, "_blocked")
}).ToArray();
var prefab = Read<SSW.NetPlayer>(game, "_playerPrefab");
var stock = UnityEngine.Resources.Load<SSW.NetStock>("Network/Potions");
var potions = Read<AbstractPotion[]>(stock, "_potions");
int Kind(string type) => System.Array.FindIndex(potions, p => p.GetType().Name == type);
int damageKind = Kind("DamagePotion"), healKind = Kind("HealthPotion"), slowKind = Kind("SlowPotion"), speedKind = Kind("SpeedPotion");
int poisonKind = Kind("PoisonPotion"), regenKind = Kind("RegenPotion");
if (new[] { damageKind, healKind, slowKind, speedKind, poisonKind, regenKind }.Any(i => i < 0)) throw new System.InvalidOperationException("Expected potion sources missing");
var books = new System.Collections.Generic.List<SSW.StatsBook>();
var cases = new System.Collections.Generic.List<object>();
var checks = new System.Collections.Generic.List<object>();
var events = new System.Collections.Generic.List<(float time, ulong owner, string kind, float amount, float health)>();
var failures = new System.Collections.Generic.List<string>();
SSW.NetPlayer actor = null, target = null;
UnityEngine.GameObject wall = null, marker = null;
int currentId = -1, ordinal = 0, count = 0;
string currentName = "";
uint action = 100000;
float grantedAt = 0f;
double started = UnityEngine.Time.realtimeSinceStartupAsDouble;
void Save(string status, string error = null)
{
    var data = new
    {
        status, error, count, currentId, currentName, seconds = UnityEngine.Time.realtimeSinceStartupAsDouble - started,
        protocol = SSW.NetGame.Protocol, build = UnityEngine.Application.buildGUID, cases, failures,
        scope = "Authoritative server effects with alternating host/client ownership. No client input, received-client-state or cooldown-HUD assertions.",
        fixture = "Runtime StatsBook clones: health=1000, player gravity=0. Native card knockback neutralized. Accepted server arguments and potion stock injected. Parent must isolate terrain and add wideground.",
        priorEvidence = new[] { "Choices4 selection/ownership", "CombatSpecialFinal01/02 multishot handoff", "Peer23 return/bounce trajectories" }
    };
    System.IO.File.WriteAllText(log, Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
}
void Check(bool condition, string label, object actual = null, object expected = null)
{
    checks.Add(new { pass = condition, label, actual, expected });
    count++;
    if (!condition) throw new System.InvalidOperationException(label);
}
bool Near(float a, float b, float tolerance = 0.025f) => UnityEngine.Mathf.Abs(a - b) <= tolerance;
void Equal(float actual, float expected, string label, float tolerance = 0.025f) => Check(Near(actual, expected, tolerance), label, actual, expected);
async System.Threading.Tasks.Task Wait(float seconds)
{
    float end = UnityEngine.Time.time + seconds;
    double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds * 3d + 10d;
    while (UnityEngine.Time.time < end)
    {
        if (!UnityEditor.EditorApplication.isPlaying || !game.Connected || !game.CanFight || UnityEngine.Time.realtimeSinceStartupAsDouble > deadline)
            throw new System.TimeoutException("Effect fixture stopped or timed out");
        await System.Threading.Tasks.Task.Delay(12);
    }
}
async System.Threading.Tasks.Task Until(System.Func<bool> condition, float seconds, string label)
{
    float end = UnityEngine.Time.time + seconds;
    while (!condition())
    {
        if (UnityEngine.Time.time >= end) throw new System.TimeoutException(label);
        await Wait(0.01f);
    }
}
async System.Threading.Tasks.Task At(float time) => await Wait(UnityEngine.Mathf.Max(0f, time - UnityEngine.Time.time));
T[] Spawned<T>() where T : UnityEngine.Component => game.Manager.SpawnManager.SpawnedObjects.Values
    .Select(o => o.GetComponent<T>()).Where(o => o != null).ToArray();
void Clear()
{
    Call(game, "ClearShots");
    foreach (var p in game.Players.ToArray()) p.NetworkObject.Despawn();
    foreach (var book in books) if (book != null) UnityEngine.Object.Destroy(book);
    books.Clear();
}
void Grant(int id)
{
    actor.Draft.Restore(new[] { id });
    grantedAt = UnityEngine.Time.time;
}
async System.Threading.Tasks.Task Pair(SSW.PlayerJob job)
{
    Clear();
    await Wait(0.05f);
    ulong owner = saved[ordinal++ % saved.Length].OwnerClientId;
    foreach (var previous in saved)
    {
        var p = UnityEngine.Object.Instantiate(prefab);
        var book = UnityEngine.Object.Instantiate(Read<SSW.StatsBook>(p, "_statsBook"));
        books.Add(book);
        var stats = book.At(previous.OwnerClientId == owner ? job : SSW.PlayerJob.Swordsman);
        stats.Health = 1000f;
        stats.Gravity = 0f;
        book.Replace(new[] { stats });
        Set(p, "_statsBook", book);
        p.Init(stats.Job, previous.Side, previous.Info);
        p.NetworkObject.SpawnAsPlayerObject(previous.OwnerClientId, true);
        p.Draft.Restore(System.Array.Empty<int>());
        p.Block(true);
        p.Drive.Teleport(new UnityEngine.Vector2(previous.OwnerClientId == owner ? -8f : 3f, 7f));
        p.Health.OnDamageDealt += (amount, critical) => events.Add((UnityEngine.Time.time, p.OwnerClientId, "damage", amount, p.Health.Current));
        p.Health.OnHealed += amount => events.Add((UnityEngine.Time.time, p.OwnerClientId, "heal", amount, p.Health.Current));
        if (previous.OwnerClientId == owner) actor = p; else target = p;
    }
    UnityEngine.Physics2D.IgnoreCollision(actor.Collider, target.Collider);
    await Wait(0.25f);
    Call(actor.Cast, "SyncEpoch");
    Set(actor.Cast, "_brewAt", game.ServerTime + 10000d);
    UnityEngine.Physics2D.SyncTransforms();
    if (!actor.CanAttack || !target.CanAct) throw new System.InvalidOperationException("Spawned effect actors cannot fight");
}
float Magic(string name) => Read<float>(actor.GetComponent<SSW.MagicianAugmentController>(), name);
float CardValue(string name) => Read<float>(Read<SSW.NetCard>(actor.Cast, "_cardPrefab").GetComponent<SSW.FlyingCard>(), name);
float BaseCard(SSW.Suit suit, int rank) => actor.Stats.Damage + (suit == SSW.Suit.Spade ? rank * CardValue("_spadeDamagePerNumber")
    : suit == SSW.Suit.Diamond ? rank * CardValue("_diamondDamagePerNumber") : 0f);
SSW.NetCard MakeCard(SSW.Suit suit, int rank)
{
    uint id = ++action;
    Call(actor.Cast, "SpawnCard", suit, rank, UnityEngine.Vector2.right, 1f, false, false, id, actor.Body.position, 0d);
    var card = Spawned<SSW.NetCard>().Single(c => c.State.Caster == actor.NetworkObjectId && c.State.Action == id);
    Set(card.GetComponent<SSW.FlyingCard>(), "_knockbackForce", 0f);
    return card;
}
float TouchCard(SSW.NetCard card, UnityEngine.Collider2D contact = null, UnityEngine.Vector2? point = null)
{
    float before = target.Health.Current;
    var position = point ?? (UnityEngine.Vector2)target.Collider.bounds.center;
    card.transform.position = position;
    card.GetComponent<UnityEngine.Rigidbody2D>().position = position;
    Set(card.GetComponent<SSW.FlyingCard>(), "_knockbackForce", 0f);
    UnityEngine.Physics2D.SyncTransforms();
    card.GetComponent<SSW.FlyingCard>().Contact(contact != null ? contact : target.Collider, position);
    return before - target.Health.Current;
}
float Hit(SSW.Suit suit, int rank) => TouchCard(MakeCard(suit, rank));
async System.Threading.Tasks.Task<SSW.NetCard> Fire(SSW.Suit suit, int rank)
{
    uint id = ++action;
    var input = new SSW.CastInput { Action = id, Epoch = actor.Epoch, Tick = actor.CastTick, Direction = UnityEngine.Vector2.right, ViewTime = game.PhysicsTime };
    actor.Cast.StartCoroutine((System.Collections.IEnumerator)Call(actor.Cast, "Fire", input, actor.Body.position, suit, rank));
    await Until(() => Spawned<SSW.NetCard>().Any(c => c.State.Caster == actor.NetworkObjectId && c.State.Action == id), 2f, "Native Fire did not spawn a card");
    return Spawned<SSW.NetCard>().Single(c => c.State.Caster == actor.NetworkObjectId && c.State.Action == id);
}
SSW.NetPotion MakePotion(int kind)
{
    var bag = Read<SSW.CastStock>(actor.Cast, "_inventory");
    for (int i = 0; i < 3 && bag.Held.Id != 0; i++)
        if (!bag.Take(bag.Held.Id, actor.Epoch, game.ServerTime, out _)) throw new System.InvalidOperationException("Fixture stock epoch mismatch");
    bag.Add(kind, game.ServerTime, 1.25d);
    uint id = ++action;
    var input = new SSW.CastInput
    {
        Action = id, Epoch = actor.Epoch, Tick = System.Math.Max(actor.CastTick, Read<uint>(actor.Cast, "_throwTick")),
        Stock = bag.Held.Id, Kind = SSW.CastKind.Press, Direction = UnityEngine.Vector2.right, ViewTime = game.PhysicsTime
    };
    if (!(bool)Call(actor.Cast, "Throw", input, actor.Body.position)) throw new System.InvalidOperationException("Injected authoritative throw rejected");
    var shots = Spawned<SSW.NetPotion>().Where(p => p.Caster == actor.NetworkObjectId && p.Action == id).ToArray();
    if (shots.Length == 0) throw new System.InvalidOperationException("Throw spawned no potion");
    foreach (var shot in shots.Skip(1)) shot.NetworkObject.Despawn();
    return shots[0];
}
float TouchPotion(SSW.NetPotion potion, UnityEngine.Vector2 point, SSW.IDamageable direct)
{
    float before = target.Health.Current;
    potion.transform.position = point;
    potion.GetComponent<UnityEngine.Rigidbody2D>().position = point;
    UnityEngine.Physics2D.SyncTransforms();
    Call(potion, "Contact", point, UnityEngine.Vector2.up, direct);
    return before - target.Health.Current;
}
float Potion(int kind) => TouchPotion(MakePotion(kind), target.Collider.bounds.center, target.Health);
UnityEngine.Vector2 Edge(float gap) => (UnityEngine.Vector2)target.Collider.bounds.center
    - UnityEngine.Vector2.right * (target.Collider.bounds.extents.x + gap);
string potionMethod = "Native NetCast.Throw with injected server stock and accepted tick; one native part receives Contact. Extra parts removed to isolate potency, not a multishot/trajectory/input test.";
async System.Threading.Tasks.Task Case(int id, string name, SSW.PlayerJob job, string method, System.Func<System.Threading.Tasks.Task> body)
{
    currentId = id; currentName = name;
    checks.Clear(); events.Clear();
    string error = null;
    float start = UnityEngine.Time.time;
    Save("running");
    try
    {
        await Pair(job);
        events.Clear();
        await body();
    }
    catch (System.Exception failure)
    {
        error = failure.ToString();
        failures.Add(id + " " + name + ": " + failure.GetBaseException().Message);
    }
    cases.Add(new
    {
        id, name, method, status = error == null ? "passed" : "failed", error,
        owner = actor != null ? actor.OwnerClientId : ulong.MaxValue,
        ownership = actor != null && actor.IsOwner ? "host-owned" : "client-owned, executed on server",
        duration = UnityEngine.Time.time - start, checks = checks.ToArray(),
        events = events.Select(e => new { e.time, e.owner, e.kind, e.amount, e.health }).ToArray()
    });
    Save("running");
}
async System.Threading.Tasks.Task TickPotion(int kind, bool healing, int count, float interval)
{
    if (healing) target.Health.TakeDamage(100f);
    events.Clear();
    float before = target.Health.Current, at = UnityEngine.Time.time;
    Potion(kind);
    await Wait(interval * 0.7f);
    Check(!events.Any(e => e.owner == target.OwnerClientId && e.kind == (healing ? "heal" : "damage")), "no early potion tick");
    await At(at + count * interval + 0.2f);
    var ticks = events.Where(e => e.owner == target.OwnerClientId && e.kind == (healing ? "heal" : "damage")).ToArray();
    Check(ticks.Length == count, "exact potion tick count", ticks.Length, count);
    foreach (var tick in ticks) Equal(tick.amount, stock.At(kind).amount, "potion tick magnitude");
    for (int i = 0; i < ticks.Length; i++)
        Check(ticks[i].time - at >= (i + 1) * interval - 0.04f, "potion ticks are delayed", ticks[i].time - at, (i + 1) * interval);
    Equal(healing ? target.Health.Current - before : before - target.Health.Current, count * stock.At(kind).amount, "potion total HP change");
    await Wait(interval + 0.1f);
    Check(events.Count(e => e.owner == target.OwnerClientId && e.kind == (healing ? "heal" : "damage")) == count, "potion effect stops after final tick");
}
async System.Threading.Tasks.Task<float> BrewInterval()
{
    var state = Read<Unity.Netcode.NetworkVariable<SSW.NetCast.PotionState>>(actor.Cast, "_potions");
    uint revision = state.Value.Revision;
    Set(actor.Cast, "_brewAt", game.ServerTime + 0.05d);
    await Until(() => state.Value.Revision != revision, 3f, "First natural brew missing");
    float first = UnityEngine.Time.time;
    revision = state.Value.Revision;
    await Until(() => state.Value.Revision != revision, actor.Stats.BrewTime + 2f, "Second natural brew missing");
    float interval = UnityEngine.Time.time - first;
    Set(actor.Cast, "_brewAt", game.ServerTime + 10000d);
    return interval;
}
async System.Threading.Tasks.Task Run()
{
    string fatal = null;
    UnityEditor.SessionState.SetBool(runKey, true);
    try
    {
        wall = new UnityEngine.GameObject("Magic effect wall contact");
        wall.transform.position = new UnityEngine.Vector3(100f, 100f);
        wall.AddComponent<UnityEngine.BoxCollider2D>();
        marker = new UnityEngine.GameObject("Magic effect area contact");
        marker.transform.position = new UnityEngine.Vector3(110f, 100f);
        marker.AddComponent<UnityEngine.BoxCollider2D>();
        var markerHealth = marker.AddComponent<SSW.Health>();
        Call(markerHealth, "SetMax", 1000f);
        Save("running");
        await Case(5, "카드 연계", SSW.PlayerJob.Magician, "SpawnCard and injected native target/wall Contact", async () =>
        {
            Grant(5);
            float basic = BaseCard(SSW.Suit.Spade, 4);
            Equal(Hit(SSW.Suit.Spade, 4), basic, "first suit hit");
            Equal(Hit(SSW.Suit.Spade, 4), basic, "second suit hit");
            Equal(Hit(SSW.Suit.Spade, 4), basic * Magic("_chainBonusMultiplier"), "third same-suit hit gains chain damage");
            Hit(SSW.Suit.Heart, 4);
            Equal(Hit(SSW.Suit.Spade, 4), basic, "changing suit resets chain");
            Hit(SSW.Suit.Spade, 4);
            var miss = MakeCard(SSW.Suit.Spade, 4);
            miss.GetComponent<SSW.FlyingCard>().Contact(wall.GetComponent<UnityEngine.Collider2D>(), miss.transform.position);
            Equal(Hit(SSW.Suit.Spade, 4), basic, "wall miss resets chain");
            await Wait(0.03f);
        });
        await Case(6, "더블 드로우", SSW.PlayerJob.Magician, "DrawRank at one controlled tick, then native Fire and target Contact; not player input", async () =>
        {
            uint start = Read<uint>(actor.Cast, "_rankStart"), tick = 0;
            int first = 0, second = 0;
            for (uint i = 0; i < 64; i++)
            {
                tick = start + i * 127u;
                first = 1 + (int)((uint)Call(actor.Cast, "Draw", tick, start, 113u) % 10u);
                second = 1 + (int)((uint)Call(actor.Cast, "Draw", tick, start, 211u) % 10u);
                if (second > first) break;
            }
            Check(second > first, "controlled tick distinguishes second draw", new { tick, first, second });
            Equal((int)Call(actor.Cast, "DrawRank", tick), first, "unaugmented rank control");
            Grant(6);
            int rank = (int)Call(actor.Cast, "DrawRank", tick);
            Equal(rank, second, "higher second rank is used");
            var card = await Fire(SSW.Suit.Spade, rank);
            Equal(card.State.Rank, second, "native card receives winning rank");
            Equal(TouchCard(card), BaseCard(SSW.Suit.Spade, second), "winning rank changes real damage");
        });
        await Case(7, "응급 마술", SSW.PlayerJob.Magician, "Native Heart Contact, observed healing and natural cooldown; no HUD assertion", async () =>
        {
            Grant(7); actor.Health.TakeDamage(200f);
            float at = UnityEngine.Time.time, baseHeal = 4f * CardValue("_heartHealPerNumber"), hp = actor.Health.Current;
            Hit(SSW.Suit.Heart, 4);
            Equal(actor.Health.Current - hp, baseHeal + Magic("_emergencyHealBonus"), "first Heart receives bonus heal");
            hp = actor.Health.Current;
            Hit(SSW.Suit.Heart, 4);
            Equal(actor.Health.Current - hp, baseHeal, "bonus blocked during cooldown");
            await At(at + Magic("_emergencyCooldown") + 0.08f);
            hp = actor.Health.Current;
            Hit(SSW.Suit.Heart, 4);
            Equal(actor.Health.Current - hp, baseHeal + Magic("_emergencyHealBonus"), "bonus returns after natural cooldown");
        });
        await Case(8, "조커 카드", SSW.PlayerJob.Magician, "Natural Joker charge and native Fire/Contact; observe whichever additional suit is randomly chosen", async () =>
        {
            Grant(8); actor.Health.TakeDamage(200f);
            var early = await Fire(SSW.Suit.Spade, 2);
            Check(!early.State.Joker && early.State.Rank == 2, "no Joker immediately after acquisition");
            TouchCard(early);
            await At(grantedAt + Magic("_jokerInterval") + 0.1f);
            var joker = await Fire(SSW.Suit.Spade, 2);
            Check(joker.State.Joker && joker.State.Rank == 10, "natural charge produces rank ten Joker", new { joker.State.Joker, joker.State.Rank });
            float hp = actor.Health.Current;
            float damage = TouchCard(joker), heal = actor.Health.Current - hp, speed = target.Motion.CurrentMoveSpeedMultiplier;
            float basic = BaseCard(SSW.Suit.Spade, 10);
            bool heart = Near(damage, basic) && Near(heal, 10f * CardValue("_heartHealPerNumber")) && Near(speed, 1f);
            bool diamond = Near(damage, basic + 10f * CardValue("_diamondDamagePerNumber")) && Near(heal, 0f) && Near(speed, 1f);
            bool clover = Near(damage, basic) && Near(heal, 0f) && Near(speed, 1f - 10f * CardValue("_cloverSlowPerNumber"));
            Check((heart ? 1 : 0) + (diamond ? 1 : 0) + (clover ? 1 : 0) == 1, "Joker applies exactly one other suit effect", new { damage, heal, speed, heart, diamond, clover });
            var spent = await Fire(SSW.Suit.Spade, 2);
            Check(!spent.State.Joker && spent.State.Rank == 2, "next fire cannot reuse consumed Joker");
            TouchCard(spent);
        });
        await Case(9, "행운의 클로버", SSW.PlayerJob.Magician, "Native Clover Contact, then actual outgoing damage during/after natural weaken", async () =>
        {
            Grant(9);
            float at = UnityEngine.Time.time, slowTime = CardValue("_cloverSlowDuration");
            Hit(SSW.Suit.Clover, 2);
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f - 2f * CardValue("_cloverSlowPerNumber"), "base Clover slow applies");
            Equal(target.Motion.CurrentOutgoingDamageMultiplier, 1f, "weaken does not start before base slow ends");
            await At(at + slowTime + 0.06f);
            float before = actor.Health.Current;
            SSW.CombatDamage.Deal(target, actor.Health, 10f);
            Equal(before - actor.Health.Current, 10f * (1f - Magic("_cloverWeakenAmount")), "Clover weakens actual outgoing damage");
            await At(at + slowTime + Magic("_cloverWeakenDuration") + 0.15f);
            before = actor.Health.Current;
            SSW.CombatDamage.Deal(target, actor.Health, 10f);
            Equal(before - actor.Health.Current, 10f, "outgoing damage restores after weaken");
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f, "base slow expires");
        });
        await Case(10, "미러 카드", SSW.PlayerJob.Magician, "Native primary Contact schedules native Mirror; mirrored Contact damage and natural cooldown", async () =>
        {
            Grant(10);
            float at = UnityEngine.Time.time;
            Hit(SSW.Suit.Spade, 4);
            Check(!Spawned<SSW.NetCard>().Any(c => c.State.Mirror), "mirror is not immediate");
            await Until(() => Spawned<SSW.NetCard>().Any(c => c.State.Mirror), Magic("_mirrorDelay") + 1f, "Mirror did not spawn");
            float elapsed = UnityEngine.Time.time - at;
            Check(elapsed >= Magic("_mirrorDelay") - 0.03f && elapsed < Magic("_mirrorDelay") + 0.25f, "mirror delayed spawn time", elapsed, Magic("_mirrorDelay"));
            var mirror = Spawned<SSW.NetCard>().Single(c => c.State.Mirror);
            Equal(TouchCard(mirror), BaseCard(SSW.Suit.Spade, 4) * Magic("_mirrorEffectMultiplier"), "mirror real damage multiplier");
            Hit(SSW.Suit.Spade, 4);
            await Wait(Magic("_mirrorDelay") + 0.12f);
            Check(!Spawned<SSW.NetCard>().Any(c => c.State.Mirror), "extra original hit cannot bypass mirror cooldown");
            await At(at + Magic("_mirrorCooldown") + 0.08f);
            Hit(SSW.Suit.Spade, 4);
            await Until(() => Spawned<SSW.NetCard>().Any(c => c.State.Mirror), 1.5f, "Mirror cooldown did not recover");
            Equal(TouchCard(Spawned<SSW.NetCard>().Single(c => c.State.Mirror)), BaseCard(SSW.Suit.Spade, 4) * Magic("_mirrorEffectMultiplier"), "mirror reusable after natural cooldown");
        });
        await Case(11, "빠른 셔플", SSW.PlayerJob.Magician, "Natural server Tick with rolling state injected; Fire coroutine measured until native spawn", async () =>
        {
            Grant(11);
            Set(actor.Cast, "_rollingRank", true);
            Set(actor.Cast, "_tickAt", game.ServerTime + 0.05d);
            double scheduled = Read<double>(actor.Cast, "_tickAt");
            await Until(() => Read<double>(actor.Cast, "_tickAt") != scheduled, 2f, "First shuffle missing");
            float first = UnityEngine.Time.time;
            scheduled = Read<double>(actor.Cast, "_tickAt");
            await Until(() => Read<double>(actor.Cast, "_tickAt") != scheduled, 2f, "Second shuffle missing");
            Equal(UnityEngine.Time.time - first, actor.Stats.RollInterval * Magic("_quickShuffleTickMultiplier"), "natural shuffle interval", 0.06f);
            Set(actor.Cast, "_rollingRank", false);
            float at = UnityEngine.Time.time;
            var card = await Fire(SSW.Suit.Spade, 3);
            float delay = UnityEngine.Time.time - at;
            Check(delay >= Magic("_quickShuffleFireDelay") - 0.03f && delay < Magic("_quickShuffleFireDelay") + 0.25f, "native delayed fire time", delay, Magic("_quickShuffleFireDelay"));
            Equal(TouchCard(card), BaseCard(SSW.Suit.Spade, 3), "delayed card still applies normal damage");
        });
        await Case(12, "리턴 카드", SSW.PlayerJob.Magician, "Native wall Contact enters return mode, then target Contact; trajectory already covered by Peer23", async () =>
        {
            Grant(12);
            float normal = Hit(SSW.Suit.Spade, 4);
            var card = MakeCard(SSW.Suit.Spade, 4);
            card.transform.position = new UnityEngine.Vector3(-2f, 7f);
            card.GetComponent<UnityEngine.Rigidbody2D>().position = card.transform.position;
            card.GetComponent<SSW.FlyingCard>().Contact(wall.GetComponent<UnityEngine.Collider2D>(), card.transform.position);
            await Wait(Magic("_returnDelay") + 0.08f);
            Check(card != null && card.GetComponent<SSW.FlyingCard>().Returning, "native card remains in return mode");
            Equal(TouchCard(card), normal * Magic("_returnDamageMultiplier"), "return hit scales actual damage");
        });
        await Case(13, "날카로운 카드", SSW.PlayerJob.Magician, "Native Spade Contact and observed delayed damage events", async () =>
        {
            Grant(13);
            float before = target.Health.Current, at = UnityEngine.Time.time;
            Equal(Hit(SSW.Suit.Spade, 4), BaseCard(SSW.Suit.Spade, 4), "normal Spade damage remains immediate");
            await Wait(Magic("_sharpCardDelay") * 0.7f);
            Equal(before - target.Health.Current, BaseCard(SSW.Suit.Spade, 4), "sharp bonus is not early");
            await At(at + Magic("_sharpCardDelay") + 0.15f);
            var ticks = events.Where(e => e.owner == target.OwnerClientId && e.kind == "damage").ToArray();
            Check(ticks.Length == 2, "exactly one delayed bonus hit", ticks.Length, 2);
            Equal(ticks[1].amount, Magic("_sharpCardBonusDamage"), "sharp bonus magnitude");
            Check(ticks[1].time - at >= Magic("_sharpCardDelay") - 0.03f, "sharp bonus delay", ticks[1].time - at, Magic("_sharpCardDelay"));
            Equal(before - target.Health.Current, BaseCard(SSW.Suit.Spade, 4) + Magic("_sharpCardBonusDamage"), "sharp total damage");
        });
        await Case(14, "반짝이는 다이아", SSW.PlayerJob.Magician, "Native Contact against local marker; actual network target on overlap-circle boundary", async () =>
        {
            float radius = CardValue("_diamondRadius"), boosted = radius * Magic("_diamondRadiusMultiplier");
            var edge = Edge((radius + boosted) * 0.5f);
            Equal(TouchCard(MakeCard(SSW.Suit.Diamond, 4), marker.GetComponent<UnityEngine.Collider2D>(), edge), 0f, "base diamond misses expanded-only area");
            Grant(14);
            Equal(TouchCard(MakeCard(SSW.Suit.Diamond, 4), marker.GetComponent<UnityEngine.Collider2D>(), edge), 4f * CardValue("_diamondDamagePerNumber"), "expanded area applies damage once");
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f - Magic("_diamondSlowAmount"), "expanded area also slows");
            await Wait(Magic("_diamondSlowDuration") + 0.12f);
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f, "diamond slow expires");
            Equal(TouchCard(MakeCard(SSW.Suit.Diamond, 4), marker.GetComponent<UnityEngine.Collider2D>(), Edge(boosted + 0.2f)), 0f, "outside boosted radius stays unaffected");
            Equal(Hit(SSW.Suit.Diamond, 4), BaseCard(SSW.Suit.Diamond, 4), "direct diamond damage not duplicated by area query");
        });
        await Case(15, "마녀의 흡정", SSW.PlayerJob.Witch, potionMethod + " Rejected-damage listener hook only as negative control.", async () =>
        {
            Grant(15); actor.Health.TakeDamage(100f);
            float before = actor.Health.Current;
            float damage = Potion(damageKind);
            Equal(damage, stock.At(damageKind).amount, "damage potion applies authoritative damage");
            Equal(actor.Health.Current - before, damage * Read<float>(actor.GetComponent<RYU._01.Script.Argument.WitchLifesteal>(), "_healRate"), "lifesteal uses applied damage");
            before = actor.Health.Current;
            actor.GetComponent<RYU._01.Script.Argument.WitchLifesteal>().OnDamageDealt(new SSW.DamageRequest(actor, 20f, SSW.DamageTag.None), new SSW.DamageResult(20f, 0f, false));
            Equal(actor.Health.Current, before, "rejected damage does not heal");
            actor.Health.Heal(1000f); actor.Health.TakeDamage(1f);
            Potion(damageKind);
            Equal(actor.Health.Current, actor.Health.Max, "lifesteal respects max HP");
            await Wait(0.03f);
        });
        await Case(17, "넓은 살포", SSW.PlayerJob.Witch, potionMethod + " Contact centers injected at exact capsule-edge distances.", async () =>
        {
            float radius = actor.Stats.Splash;
            float multiplier = Read<float>(actor.GetComponent<RYU._01.Script.Argument.WitchAugmentController>(), "_wideSpray");
            var edge = Edge(radius * (1f + multiplier) * 0.5f);
            Equal(TouchPotion(MakePotion(damageKind), edge, null), 0f, "base splash misses expanded-only area");
            Grant(17);
            Equal(TouchPotion(MakePotion(damageKind), edge, null), stock.At(damageKind).amount, "expanded splash applies damage once");
            Equal(TouchPotion(MakePotion(damageKind), Edge(radius * multiplier + 0.2f), null), 0f, "outside boosted splash stays unaffected");
            await Wait(0.03f);
        });
        await Case(18, "진한 농도", SSW.PlayerJob.Witch, potionMethod + " Poison/regen kinds injected without unlock cards to isolate this modifier.", async () =>
        {
            float baseDuration = Read<float>(stock.At(slowKind), "duration");
            float start = UnityEngine.Time.time;
            Potion(slowKind);
            await At(start + baseDuration + 0.12f);
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f, "base slow duration control");
            Grant(18);
            float multiplier = Read<float>(actor.GetComponent<RYU._01.Script.Argument.WitchAugmentController>(), "_thickBrew");
            start = UnityEngine.Time.time;
            Potion(slowKind);
            await At(start + baseDuration + 0.06f);
            Check(target.Motion.CurrentMoveSpeedMultiplier < 1f, "augmented slow outlasts base duration");
            await At(start + baseDuration * multiplier + 0.12f);
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f, "augmented slow expires at multiplied duration");
            target.Health.TakeDamage(100f); events.Clear();
            int poisonCount = UnityEngine.Mathf.RoundToInt(Read<int>(stock.At(poisonKind), "damageCount") * multiplier);
            int regenCount = UnityEngine.Mathf.RoundToInt(Read<int>(stock.At(regenKind), "healCount") * multiplier);
            float poisonTime = Read<float>(stock.At(poisonKind), "damageTime"), regenTime = Read<float>(stock.At(regenKind), "healTime");
            Potion(poisonKind); Potion(regenKind);
            await Wait(UnityEngine.Mathf.Max(poisonCount * poisonTime, regenCount * regenTime) + 0.2f);
            var damage = events.Where(e => e.owner == target.OwnerClientId && e.kind == "damage").ToArray();
            var heals = events.Where(e => e.owner == target.OwnerClientId && e.kind == "heal").ToArray();
            Check(damage.Length == poisonCount && heals.Length == regenCount, "thick brew increases actual poison and regeneration tick counts", new { damage = damage.Length, heals = heals.Length }, new { poisonCount, regenCount });
            foreach (var e in damage) Equal(e.amount, stock.At(poisonKind).amount, "thick poison keeps per-tick power");
            foreach (var e in heals) Equal(e.amount, stock.At(regenKind).amount, "thick regeneration keeps per-tick power");
        });
        await Case(19, "정밀 조제", SSW.PlayerJob.Witch, potionMethod, async () =>
        {
            float normal = Potion(damageKind);
            Grant(19);
            float power = Read<float>(actor.GetComponent<RYU._01.Script.Argument.WitchAugmentController>(), "_precisionPower");
            Equal(Potion(damageKind), normal * power, "single precise part scales damage");
            target.Health.TakeDamage(100f);
            float before = target.Health.Current;
            Potion(healKind);
            Equal(target.Health.Current - before, stock.At(healKind).amount * power, "single precise part scales healing");
            Potion(slowKind);
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f - stock.At(slowKind).amount * power, "single precise part scales slow strength");
            Potion(speedKind);
            Equal(target.Motion.CurrentMoveSpeedMultiplier, (1f - stock.At(slowKind).amount * power) * (1f + stock.At(speedKind).amount * power), "single precise part scales haste strength");
            await Wait(UnityEngine.Mathf.Max(Read<float>(stock.At(slowKind), "duration"), Read<float>(stock.At(speedKind), "duration")) + 0.15f);
            Equal(target.Motion.CurrentMoveSpeedMultiplier, 1f, "precise slow and haste expire normally");
        });
        await Case(20, "잔류형 포션", SSW.PlayerJob.Witch, potionMethod + " Native spawned NetZone applies natural timed effects.", async () =>
        {
            Grant(20); events.Clear();
            float before = target.Health.Current, casterBefore = actor.Health.Current, at = UnityEngine.Time.time;
            Equal(Potion(damageKind), stock.At(damageKind).amount, "initial potion burst");
            Check(Spawned<SSW.NetZone>().Length == 1, "native lingering zone actually spawns");
            await Wait(0.35f);
            Equal(before - target.Health.Current, stock.At(damageKind).amount, "zone has no premature tick");
            await At(at + 3.3f);
            var hits = events.Where(e => e.owner == target.OwnerClientId && e.kind == "damage").ToArray();
            Check(hits.Length == 6, "one burst plus five zone ticks", hits.Length, 6);
            foreach (var tick in hits.Skip(1)) Equal(tick.amount, stock.At(damageKind).amount * 0.35f, "zone potency");
            for (int i = 1; i < hits.Length; i++) Check(hits[i].time - at >= i * 0.5f - 0.04f, "zone tick timing", hits[i].time - at, i * 0.5f);
            Check(Spawned<SSW.NetZone>().Length == 0, "zone despawns after three seconds");
            Equal(before - target.Health.Current, stock.At(damageKind).amount * (1f + 5f * 0.35f), "zone total real damage");
            Equal(actor.Health.Current, casterBefore, "actor outside zone is unaffected");
        });
        await Case(21, "깨진 유리병", SSW.PlayerJob.Witch, potionMethod + " Damage/end conditions after native Contacts, no bounce trajectory assertion.", async () =>
        {
            Grant(21); events.Clear();
            var potion = MakePotion(damageKind);
            Equal(TouchPotion(potion, new UnityEngine.Vector2(0f, 11f), null), 0f, "empty first impact causes no target damage");
            Check(potion != null && potion.IsSpawned, "first empty impact preserves potion for next contact");
            Equal(TouchPotion(potion, target.Collider.bounds.center, target.Health), stock.At(damageKind).amount, "next target contact damages exactly once");
            Check(potion == null || !potion.IsSpawned, "target contact terminates bounced potion");
            var direct = MakePotion(damageKind);
            Equal(TouchPotion(direct, target.Collider.bounds.center, target.Health), stock.At(damageKind).amount, "direct first hit damages once");
            Check(direct == null || !direct.IsSpawned, "direct target hit cannot bounce through player");
            await Wait(0.15f);
            Check(events.Count(e => e.owner == target.OwnerClientId && e.kind == "damage") == 2, "no extra duplicate impact damage");
        });
        await Case(82, "독의 해금", SSW.PlayerJob.Witch, potionMethod + " Kind taken from this sole unlock card's runtime controller.", async () =>
        {
            Grant(82);
            var potion = actor.GetComponent<RYU._01.Script.Argument.WitchAugmentController>().Unlocked.Single(p => p.GetType().Name == "PoisonPotion");
            await TickPotion(stock.IndexOf(potion), false, Read<int>(potion, "damageCount"), Read<float>(potion, "damageTime"));
        });
        await Case(83, "재생의 해금", SSW.PlayerJob.Witch, potionMethod + " Kind taken from this sole unlock card's runtime controller.", async () =>
        {
            Grant(83);
            var potion = actor.GetComponent<RYU._01.Script.Argument.WitchAugmentController>().Unlocked.Single(p => p.GetType().Name == "RegenPotion");
            await TickPotion(stock.IndexOf(potion), true, Read<int>(potion, "healCount"), Read<float>(potion, "healTime"));
        });
        await Case(84, "여러 포션을 해금합니다", SSW.PlayerJob.Witch, "Natural brew interval before/after sole AllUnlock; native poison/regen first ticks, later tick completion covered by 82/83", async () =>
        {
            float normal = await BrewInterval();
            Equal(normal, actor.Stats.BrewTime, "unaugmented natural brew interval", 0.08f);
            Grant(84);
            float faster = await BrewInterval();
            float multiplier = Read<float>(actor.GetComponent<RYU._01.Script.Argument.WitchAugmentController>(), "_cycleSpeedUp");
            Equal(faster, actor.Stats.BrewTime * multiplier, "AllUnlock changes actual brew interval", 0.08f);
            var unlocked = actor.GetComponent<RYU._01.Script.Argument.WitchAugmentController>().Unlocked;
            var poison = unlocked.Single(p => p.GetType().Name == "PoisonPotion");
            var regen = unlocked.Single(p => p.GetType().Name == "RegenPotion");
            target.Health.TakeDamage(100f); events.Clear();
            Potion(stock.IndexOf(poison)); Potion(stock.IndexOf(regen));
            await Wait(UnityEngine.Mathf.Max(Read<float>(poison, "damageTime"), Read<float>(regen, "healTime")) + 0.15f);
            Check(events.Any(e => e.owner == target.OwnerClientId && e.kind == "damage" && Near(e.amount, poison.amount)), "AllUnlock alone reaches real poison damage");
            Check(events.Any(e => e.owner == target.OwnerClientId && e.kind == "heal" && Near(e.amount, regen.amount)), "AllUnlock alone reaches real regeneration healing");
        });
    }
    catch (System.Exception failure) { fatal = failure.ToString(); failures.Add(failure.GetBaseException().Message); }
    finally
    {
        try
        {
            if (game != null && game.Connected && game.Manager.IsServer)
            {
                Clear();
                foreach (var previous in saved)
                {
                    var p = UnityEngine.Object.Instantiate(prefab);
                    p.Init(previous.Job, previous.Side, previous.Info);
                    p.NetworkObject.SpawnAsPlayerObject(previous.OwnerClientId, true);
                    p.Draft.Restore(previous.owned);
                    p.Drive.Teleport(previous.position);
                    Call(p.Health, "ApplyNetworkState", previous.health, previous.maximum);
                    p.Block(previous.blocked);
                }
            }
        }
        catch (System.Exception failure) { fatal = (fatal ?? "") + "\nRestore: " + failure; failures.Add("fixture restoration failed"); }
        if (wall != null) UnityEngine.Object.Destroy(wall);
        if (marker != null) UnityEngine.Object.Destroy(marker);
        UnityEditor.SessionState.SetBool(runKey, false);
        currentId = -1; currentName = "";
        Save(failures.Count == 0 && cases.Count == 19 ? "completed" : "failed", fatal);
    }
}
_ = Run();
return new { running = true, log, expectedCases = 19, inputValidation = false, clientStateValidation = false };
