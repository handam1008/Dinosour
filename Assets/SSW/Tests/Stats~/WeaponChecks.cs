if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var random = UnityEngine.Random.state;
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var checks = new System.Collections.Generic.List<string>();
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
void Check(bool condition, string label)
{
    if (!condition) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
System.Reflection.FieldInfo Field(object target, string name)
{
    for (var type = target.GetType(); type != null; type = type.BaseType)
    {
        var field = type.GetField(name, flags);
        if (field != null) return field;
    }
    throw new System.MissingFieldException(target.GetType().Name, name);
}
object Read(object target, string name) => Field(target, name).GetValue(target);
void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, flags).Invoke(target, args);
bool Near(float left, float right) => UnityEngine.Mathf.Abs(left - right) < 0.001f;
bool Near2(UnityEngine.Vector2 left, UnityEngine.Vector2 right) => UnityEngine.Vector2.Distance(left, right) < 0.001f;
bool Near3(UnityEngine.Vector3 left, UnityEngine.Vector3 right) => UnityEngine.Vector3.Distance(left, right) < 0.001f;
uint Ticks(float seconds) => (uint)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(0.001f, seconds) / UnityEngine.Time.fixedDeltaTime);
SSW.WeaponState Advance(SSW.JobCast cast, SSW.WeaponState state, uint tick)
{
    object[] args = { state, tick };
    Call(cast, "Advance", args);
    return (SSW.WeaponState)args[0];
}
(bool Accepted, SSW.WeaponState State, SSW.BoltSpec Bolt) Plan(SSW.JobCast cast, SSW.WeaponState state, uint tick, SSW.CastKind kind = SSW.CastKind.Press)
{
    object[] args = { state, new SSW.CastInput { Action = tick, Tick = tick, Kind = kind, Direction = UnityEngine.Vector2.right }, default(SSW.BoltSpec) };
    bool accepted = (bool)Call(cast, "Plan", args);
    return (accepted, (SSW.WeaponState)args[0], (SSW.BoltSpec)args[2]);
}
void Flight(SSW.BoltSpec bolt, SSW.FlightStats expected, string label)
{
    Check(Near(bolt.Speed, expected.Speed) && Near(bolt.Gravity, expected.Gravity) && Near(bolt.Life, expected.Life)
        && Near(bolt.Scale, expected.Scale) && Near(bolt.Aspect, expected.Aspect) && Near(bolt.Spin, expected.Spin), label + " flight");
}
SSW.FighterStats Changed(SSW.FighterStats stats)
{
    stats.Damage = stats.Damage * 1.3f + 7.25f;
    stats.AttackInterval += 0.23f;
    stats.Flight.Speed += 4.125f;
    stats.Flight.Gravity += 0.35f;
    stats.Flight.Life += 1.75f;
    stats.Flight.Scale = stats.Flight.Scale * 0.85f + 0.13f;
    stats.Flight.Aspect += 0.2f;
    stats.Flight.Spin += 137.5f;
    stats.SkillDamage += 9.75f;
    stats.SkillCooldown += 0.47f;
    stats.DashSpeed += 2.5f;
    stats.DashTime += 0.12f;
    stats.DashCooldown += 0.39f;
    stats.DashDamage += 11f;
    stats.DashRadius += 0.21f;
    stats.ParryTime += 0.17f;
    stats.ParryCooldown += 0.51f;
    stats.ReflectSpeed += 3.5f;
    stats.Reload = stats.Reload * 0.7f + 0.15f;
    stats.ChargeTime = stats.ChargeTime * 0.65f + 0.25f;
    stats.ChargeDamage += 0.75f;
    stats.GravityDelay += 0.19f;
    stats.ExtraGravity += 6.25f;
    stats.RollInterval += 0.21f;
    stats.BrewTime += 0.63f;
    stats.ThrowLift += 1.25f;
    stats.Spread += 11f;
    if (stats.Job == SSW.PlayerJob.Gunner) stats.Capacity = stats.Capacity == 15 ? 12 : 15;
    if (stats.Job == SSW.PlayerJob.Gambler) stats.Capacity = stats.Capacity == 5 ? 7 : 5;
    return stats;
}
try
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab");
    var book = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.StatsBook>("Assets/SSW/Resources/Network/Stats.asset");
    Check(prefab != null && book != null, "player and baked stats assets exist");
    var obj = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    var player = obj.GetComponent<SSW.NetPlayer>();
    var gun = obj.GetComponent<SSW.GunCast>();
    var coin = obj.GetComponent<SSW.CoinCast>();
    var knife = obj.GetComponent<SSW.KnifeCast>();
    var sword = obj.GetComponent<SSW.SwordCast>();
    var cast = player.Cast;
    Set(cast.Cards, "_cardFeedback", null);
    void Profile(SSW.FighterStats stats)
    {
        SSW.StatCheck.Validate(stats);
        Set(Read(player, "_stats"), "m_InternalValue", stats);
        Set(Read(player, "_job"), "m_InternalValue", stats.Job);
        cast.CooldownScale = 1f;
    }
    void Preview(SSW.BoltSpec bolt, string label)
    {
        var style = new UnityEngine.GameObject("Bolt style").AddComponent<UnityEngine.SpriteRenderer>();
        var preview = new SSW.CastView(cast.Cards, 0, bolt.Radius, player, System.Array.Empty<SSW.NetPlayer>());
        preview.Bolt(1u, UnityEngine.Vector2.zero, UnityEngine.Vector2.right, style, null, bolt, 2.5f);
        object shot = ((System.Collections.IList)Read(preview, "_shots"))[0];
        var renderer = (UnityEngine.SpriteRenderer)Read(shot, "View");
        Check(Near2((UnityEngine.Vector2)Read(shot, "Velocity"), UnityEngine.Vector2.right * bolt.Speed)
            && Near2((UnityEngine.Vector2)Read(shot, "Gravity"), UnityEngine.Physics2D.gravity * bolt.Gravity)
            && Near((float)Read(shot, "Spin"), bolt.Spin), label + " prediction motion");
        Check(Near3(renderer.transform.localScale, new UnityEngine.Vector3(bolt.Scale, bolt.Scale * bolt.Aspect, 1f))
            && Near((float)Read(shot, "GravityDelay"), bolt.GravityDelay)
            && Near((float)Read(shot, "ExtraGravity"), bolt.ExtraGravity), label + " prediction dimensions and delayed gravity");
    }
    foreach (var job in new[] { SSW.PlayerJob.Gunner, SSW.PlayerJob.Gambler, SSW.PlayerJob.Assassin,
        SSW.PlayerJob.Swordsman, SSW.PlayerJob.Magician, SSW.PlayerJob.Witch })
    {
        SSW.FighterStats original = book.At(job);
        for (int mode = 0; mode < 2; mode++)
        {
            SSW.FighterStats stats = mode == 0 ? original : Changed(original);
            string label = job + (mode == 0 ? " original" : " changed");
            Profile(stats);
            Check(player.Stats.Equals(stats), label + " authoritative profile injection");
            using (var writer = new Unity.Netcode.FastBufferWriter(1024, Unity.Collections.Allocator.Temp))
            {
                writer.WriteNetworkSerializable(stats);
                using var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
                reader.ReadNetworkSerializable(out SSW.FighterStats restored);
                Check(restored.Equals(stats), label + " profile serialization");
            }
            const uint tick = 1000;
            if (job == SSW.PlayerJob.Gunner)
            {
                uint reload = Ticks(stats.Reload);
                uint charge = Ticks(stats.ChargeTime);
                var start = (SSW.WeaponState)Call(gun, "Initial");
                Check(start.Ammo == 0 && start.Reload == start.Filled + reload, label + " first recharge");
                Check(Advance(gun, start, start.Reload - 1).Ammo == 0, label + " recharge cannot finish early");
                var loaded = Advance(gun, start, start.Reload);
                Check(loaded.Ammo == 1 && loaded.Loaded.Length == 1 && loaded.Loaded[0] == start.Reload
                    && Advance(gun, loaded, start.Reload).Equals(loaded), label + " one authoritative load per tick");
                var ordinary = Plan(gun, loaded, start.Reload);
                Check(ordinary.Accepted && !ordinary.Bolt.Charged && Near(ordinary.Bolt.Damage, stats.Damage)
                    && ordinary.State.Ready == start.Reload + Ticks(stats.AttackInterval), label + " ordinary damage and interval");
                Flight(ordinary.Bolt, stats.Flight, label);
                var ready = new SSW.WeaponState { Ammo = 2, Reload = tick + charge + reload };
                ready.Loaded.Add(tick);
                ready.Loaded.Add(tick);
                var early = Plan(gun, ready, tick + charge - 1);
                var charged = Plan(gun, ready, tick + charge);
                Check(early.Accepted && !early.Bolt.Charged && charged.Accepted && charged.Bolt.Charged
                    && Near(charged.Bolt.Damage, stats.Damage * stats.ChargeDamage), label + " charge boundary and multiplier");
                Check(!Plan(gun, charged.State, charged.State.Ready - 1).Accepted
                    && Plan(gun, charged.State, charged.State.Ready).Accepted, label + " attack cooldown boundary");
                Check(Near(gun.Charge(tick, tick - 1), 0f) && gun.Charge(tick, tick + charge - 1) < 1f
                    && Near(gun.Charge(tick, tick + charge), 1f), label + " charge UI agrees with shot threshold");
                uint fullTick = start.Reload + (uint)(stats.Capacity + 2) * reload;
                var full = Advance(gun, start, fullTick);
                Check(gun.Capacity == stats.Capacity && full.Ammo == stats.Capacity && full.Loaded.Length == stats.Capacity
                    && Advance(gun, full, fullTick + reload).Equals(full), label + " capacity and recharge cap");
                var evolved = Advance(gun, new SSW.WeaponState { Reload = tick, Progress = gun.QuestHits }, tick);
                Check(evolved.Reload == tick + Ticks(stats.Reload * 0.5f), label + " evolution applies once");
                cast.CooldownScale = 0.65f;
                var scaled = Plan(gun, ready, tick + charge);
                var scaledStart = (SSW.WeaponState)Call(gun, "Initial");
                Check(scaled.State.Ready == tick + charge + Ticks(stats.AttackInterval * 0.65f)
                    && scaledStart.Reload == scaledStart.Filled + Ticks(stats.Reload * 0.65f)
                    && Near(scaled.Bolt.Damage, charged.Bolt.Damage), label + " common cooldown applies once without scaling damage");
                Preview(charged.Bolt, label);
            }
            else if (job == SSW.PlayerJob.Gambler)
            {
                Check(((SSW.WeaponState)Call(coin, "Initial")).Ammo == stats.Capacity, label + " initial magazine");
                foreach (uint seed in new[] { 0u, 1u, uint.MaxValue })
                {
                    var state = new SSW.WeaponState { Ammo = stats.Capacity, Filled = seed };
                    int roulette = 0;
                    uint at = tick;
                    bool valid = true;
                    for (int remaining = stats.Capacity; remaining > 0; remaining--)
                    {
                        var shot = Plan(coin, state, at);
                        bool gold = remaining == 1 + seed % (uint)stats.Capacity;
                        valid &= shot.Accepted && shot.State.Ammo == remaining - 1
                            && shot.State.Ready == at + Ticks(stats.AttackInterval)
                            && shot.State.Reload == at + Ticks(stats.Reload) && shot.Bolt.Style == (gold ? 2 : 1);
                        if (shot.Bolt.Style == 2) roulette++;
                        state = shot.State;
                        at = state.Ready;
                    }
                    Check(valid && roulette == 1 && !Plan(coin, state, at).Accepted, label + " exactly one roulette in magazine " + seed);
                    Check(Advance(coin, state, state.Reload - 1).Ammo == 0
                        && Advance(coin, state, state.Reload).Ammo == stats.Capacity, label + " reload boundary " + seed);
                }
                var first = Plan(coin, new SSW.WeaponState { Ammo = stats.Capacity, Filled = 0 }, tick);
                Flight(first.Bolt, stats.Flight, label);
                Check(Near(first.Bolt.Damage, stats.Damage) && Near(first.Bolt.GravityDelay, stats.GravityDelay)
                    && Near(first.Bolt.ExtraGravity, stats.ExtraGravity), label + " coin damage and delayed gravity");
                cast.CooldownScale = 0.65f;
                var scaled = Plan(coin, new SSW.WeaponState { Ammo = stats.Capacity }, tick);
                Check(scaled.State.Ready == tick + Ticks(stats.AttackInterval * 0.65f)
                    && scaled.State.Reload == tick + Ticks(stats.Reload * 0.65f)
                    && Near(scaled.Bolt.Damage, stats.Damage), label + " common cooldown applies once");
                Preview(first.Bolt, label);
            }
            else if (job == SSW.PlayerJob.Assassin)
            {
                var attack = Plan(knife, default, tick);
                Check(attack.Accepted && attack.State.Ready == tick + Ticks(stats.AttackInterval)
                    && !Plan(knife, attack.State, attack.State.Ready - 1).Accepted
                    && Plan(knife, attack.State, attack.State.Ready).Accepted, label + " melee interval boundary");
                var thrown = Plan(knife, default, tick, SSW.CastKind.Cycle);
                Flight(thrown.Bolt, stats.Flight, label);
                Check(thrown.Accepted && thrown.State.Ammo == 1 && thrown.State.Skill == tick + Ticks(stats.SkillCooldown)
                    && Near(thrown.Bolt.Damage, stats.SkillDamage) && thrown.Bolt.Stick && thrown.Bolt.CanPenetrate, label + " throw damage and cooldown");
                var recall = Plan(knife, thrown.State, tick, SSW.CastKind.Cycle);
                Check(recall.Accepted && recall.State.Ammo == 0 && recall.State.Skill == thrown.State.Skill && Near(recall.Bolt.Speed, 0f), label + " recall never creates a second knife");
                Check(!Plan(knife, recall.State, thrown.State.Skill - 1, SSW.CastKind.Cycle).Accepted
                    && Plan(knife, recall.State, thrown.State.Skill, SSW.CastKind.Cycle).Accepted, label + " recalled knife retains throw cooldown");
                cast.CooldownScale = 0.65f;
                Check(Plan(knife, default, tick).State.Ready == tick + Ticks(stats.AttackInterval * 0.65f)
                    && Plan(knife, default, tick, SSW.CastKind.Cycle).State.Skill == tick + Ticks(stats.SkillCooldown * 0.65f), label + " common cooldown applies once");
                Preview(thrown.Bolt, label);
            }
            else if (job == SSW.PlayerJob.Swordsman)
            {
                foreach (var kind in new[] { SSW.CastKind.Press, SSW.CastKind.Cycle, SSW.CastKind.Parry })
                {
                    float seconds = kind == SSW.CastKind.Press ? stats.AttackInterval : kind == SSW.CastKind.Cycle ? stats.DashCooldown : stats.ParryCooldown;
                    var result = Plan(sword, default, tick, kind);
                    uint ready = kind == SSW.CastKind.Press ? result.State.Ready : kind == SSW.CastKind.Cycle ? result.State.Skill : result.State.Parry;
                    Check(result.Accepted && ready == tick + Ticks(seconds)
                        && !Plan(sword, result.State, ready - 1, kind).Accepted
                        && Plan(sword, result.State, ready, kind).Accepted, label + " cooldown boundary " + kind);
                    cast.CooldownScale = 0.65f;
                    var scaled = Plan(sword, default, tick, kind);
                    uint scaledReady = kind == SSW.CastKind.Press ? scaled.State.Ready : kind == SSW.CastKind.Cycle ? scaled.State.Skill : scaled.State.Parry;
                    Check(scaledReady == tick + Ticks(seconds * 0.65f), label + " common cooldown applies once " + kind);
                    cast.CooldownScale = 1f;
                }
                Check(Near(sword.ReflectSpeed, stats.ReflectSpeed), label + " reflection speed");
            }
            else if (job == SSW.PlayerJob.Magician)
            {
                var cooldown = cast.GetType().GetProperty("CardCooldown", flags);
                Check(Near((float)cooldown.GetValue(cast), stats.AttackInterval), label + " card cooldown");
                cast.CooldownScale = 0.65f;
                Check(Near((float)cooldown.GetValue(cast), stats.AttackInterval * 0.65f), label + " card cooldown scale once");
                uint interval = (uint)System.Math.Ceiling((double)stats.RollInterval / UnityEngine.Time.fixedDeltaTime);
                uint first = (uint)Call(cast, "Draw", tick, tick, 113u);
                Check(first == (uint)Call(cast, "Draw", tick + interval - 1, tick, 113u)
                    && first != (uint)Call(cast, "Draw", tick + interval, tick, 113u), label + " card roll interval boundary");
                var source = (SSW.NetCard)Read(cast, "_cardPrefab");
                var cardObj = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source.gameObject, scene);
                var card = cardObj.GetComponent<SSW.NetCard>();
                card.Init(player, (SSW.Suit)0, 3, UnityEngine.Vector2.left, 1f, false, false);
                Check(Near2((UnityEngine.Vector2)Read(card, "_velocity"), UnityEngine.Vector2.left * stats.Flight.Speed)
                    && Near((float)Read(card, "_spin"), -stats.Flight.Spin), label + " server card launch");
                var preview = new SSW.CastView(cast.Cards, 0, source.PreviewRadius(stats.Flight.Scale), player, System.Array.Empty<SSW.NetPlayer>());
                preview.Card(1u, UnityEngine.Vector2.zero, UnityEngine.Vector2.left, (SSW.Suit)0, 3, 1f, stats.Flight.Gravity, 2.5f);
                object shot = ((System.Collections.IList)Read(preview, "_shots"))[0];
                var renderer = (UnityEngine.SpriteRenderer)Read(shot, "View");
                Check(Near2((UnityEngine.Vector2)Read(shot, "Velocity"), (UnityEngine.Vector2)Read(card, "_velocity"))
                    && Near((float)Read(shot, "Spin"), (float)Read(card, "_spin"))
                    && Near3(renderer.transform.localScale, new UnityEngine.Vector3(stats.Flight.Scale, stats.Flight.Scale * stats.Flight.Aspect, 1f)), label + " client card follows server flight profile");
            }
            else
            {
                var potion = (SSW.NetPotion)Read(cast, "_potionPrefab");
                var collider = (UnityEngine.CapsuleCollider2D)Read(potion, "_collider");
                var expected = UnityEngine.Vector2.Scale(collider.size, new UnityEngine.Vector2(stats.Flight.Scale, stats.Flight.Scale * stats.Flight.Aspect));
                Check(Near2(potion.PreviewSize(stats.Flight), expected), label + " potion collision preview dimensions");
                var preview = new SSW.CastView(cast.Cards, 0, expected.x * 0.5f, player, System.Array.Empty<SSW.NetPlayer>());
                var velocity = UnityEngine.Vector2.right * stats.Flight.Speed + UnityEngine.Vector2.up * stats.ThrowLift;
                preview.Potion(1u, 0, UnityEngine.Vector2.zero, velocity, potion.Style, null, stats.Flight.Gravity, 2.5f, expected);
                object shot = ((System.Collections.IList)Read(preview, "_shots"))[0];
                var renderer = (UnityEngine.SpriteRenderer)Read(shot, "View");
                Check(Near2((UnityEngine.Vector2)Read(shot, "Velocity"), velocity)
                    && Near2((UnityEngine.Vector2)Read(shot, "Gravity"), UnityEngine.Physics2D.gravity * stats.Flight.Gravity)
                    && Near((float)Read(shot, "Spin"), stats.Flight.Spin)
                    && Near2((UnityEngine.Vector2)Read(shot, "Size"), expected)
                    && Near3(renderer.transform.localScale, new UnityEngine.Vector3(stats.Flight.Scale, stats.Flight.Scale * stats.Flight.Aspect, 1f)), label + " potion prediction flight and shape");
            }
        }
        Check(book.At(job).Equals(original), job + " baked asset remains unchanged");
    }
    var view = gun.View;
    var slots = (System.Array)Read(view, "_slots");
    Check(slots.Length == 9, "original nine slot UI template is preserved");
    var roots = new UnityEngine.GameObject[9];
    var fills = new UnityEngine.Transform[9];
    var positions = new UnityEngine.Vector3[9];
    for (int i = 0; i < 9; i++)
    {
        roots[i] = (UnityEngine.GameObject)Read(slots.GetValue(i), "Root");
        fills[i] = (UnityEngine.Transform)Read(slots.GetValue(i), "Fill");
        positions[i] = roots[i].transform.localPosition;
    }
    var uiStats = book.At(SSW.PlayerJob.Gunner);
    uiStats.Capacity = 15;
    uiStats.ChargeTime += 0.37f;
    Profile(uiStats);
    var fullUi = new SSW.WeaponState { Ammo = 15 };
    for (int i = 0; i < 15; i++) fullUi.Loaded.Add(10u);
    uint uiTick = 10u + Ticks(uiStats.ChargeTime);
    var parent = roots[0].transform.parent;
    int count = parent.childCount;
    view.Draw(true, fullUi, uiTick);
    slots = (System.Array)Read(view, "_slots");
    Check(slots.Length == 15 && parent.childCount == count + 6 && view.VisibleAmmo == 15 && view.ChargedAmmo == 15, "fifteen rounds create only six extra slots");
    var unique = new System.Collections.Generic.HashSet<UnityEngine.GameObject>();
    for (int i = 0; i < 15; i++)
    {
        var root = (UnityEngine.GameObject)Read(slots.GetValue(i), "Root");
        var fill = (UnityEngine.Transform)Read(slots.GetValue(i), "Fill");
        Check(unique.Add(root) && fill.IsChildOf(root.transform) && root.transform.parent == parent, "ammo slot owns its local fill " + i);
        if (i < 9)
            Check(root == roots[i] && fill == fills[i] && Near3(root.transform.localPosition, positions[i]), "original slot reference and position " + i);
        else
            Check(Near3(root.transform.localPosition, positions[i % 3] + (positions[3] - positions[0]) * (i / 3)), "extended ammo row spacing " + i);
    }
    view.Draw(true, fullUi, uiTick);
    Check(parent.childCount == count + 6, "repeated draw cannot duplicate expanded slots");
    view.Draw(false, fullUi, uiTick);
    Check(view.VisibleAmmo == 0 && view.ChargedAmmo == 0, "expanded ammo hides outside combat");
    uiStats.Capacity = 9;
    Profile(uiStats);
    view.Draw(true, fullUi, uiTick);
    Check(view.VisibleAmmo == 9 && parent.childCount == count + 6, "smaller capacity hides extras without replacing original slots");
    Check(((System.Array)Read(prefab.GetComponent<SSW.GunCast>().View, "_slots")).Length == 9, "UI expansion does not modify prefab asset");
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
    UnityEngine.Random.state = random;
}
if (checks.Count > 0) return new { passed = checks.Count, checks };
