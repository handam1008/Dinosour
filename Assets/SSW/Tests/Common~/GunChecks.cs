var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var checks = new System.Collections.Generic.List<string>();
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
void Check(bool ok, string label)
{
    if (!ok) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
System.Reflection.FieldInfo Field(object target, string name)
{
    for (var type = target.GetType(); type != null; type = type.BaseType)
    {
        var field = type.GetField(name, flags);
        if (field != null) return field;
    }
    throw new System.MissingFieldException(name);
}
object Read(object target, string name) => Field(target, name).GetValue(target);
void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, flags).Invoke(target, args);
uint Ticks(float seconds) => (uint)UnityEngine.Mathf.CeilToInt(seconds / UnityEngine.Time.fixedDeltaTime);
bool Near(float left, float right) => UnityEngine.Mathf.Abs(left - right) < 0.001f;
try
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab");
    var obj = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    var player = obj.GetComponent<SSW.NetPlayer>();
    var gun = obj.GetComponent<SSW.GunCast>();
    var view = gun.View;
    var effects = gun.Effects;
    var pool = effects.Pool;
    Check(view != null && effects != null && pool != null, "gun presentation and explicit pool references exist");
    Check((int)Read(gun, "_capacity") == 9 && Near((float)Read(gun, "_reload"), 2.5f), "original nine round magazine and recharge rate");
    Check(gun.QuestHits == 2 && Near((float)Read(gun, "_shrinkCooldown"), 5f), "original evolution requirement and shrink cooldown");
    player.Cast.CooldownScale = 1f;
    SSW.WeaponState Advance(SSW.WeaponState state, uint tick)
    {
        object[] args = { state, tick };
        Call(gun, "Advance", args);
        return (SSW.WeaponState)args[0];
    }
    (bool Accepted, SSW.WeaponState State, SSW.BoltSpec Bolt) Plan(SSW.WeaponState state, uint tick, SSW.CastKind kind = SSW.CastKind.Press)
    {
        object[] args = { state, new SSW.CastInput { Kind = kind, Tick = tick, Direction = UnityEngine.Vector2.right }, default(SSW.BoltSpec) };
        bool accepted = (bool)Call(gun, "Plan", args);
        return (accepted, (SSW.WeaponState)args[0], (SSW.BoltSpec)args[2]);
    }
    var state = new SSW.WeaponState { Reload = Ticks(2.5f) };
    Check(!Plan(state, 0).Accepted, "empty magazine cannot fire");
    state = Advance(state, Ticks(2.5f));
    Check(state.Ammo == 1 && state.Loaded.Length == 1, "recharge stores each loaded round once");
    var ordinary = Plan(state, Ticks(2.5f));
    Check(ordinary.Accepted && Near(ordinary.Bolt.Damage, 15f) && !ordinary.Bolt.Charged, "ordinary round applies fifteen damage");
    Check(Near(ordinary.Bolt.Speed, 30f) && Near(ordinary.Bolt.Gravity, 2f) && Near(ordinary.Bolt.Scale, 1f), "original speed gravity and bullet scale");
    var chargedState = new SSW.WeaponState { Ammo = 1, Reload = Ticks(10f) };
    chargedState.Loaded.Add(0);
    var charged = Plan(chargedState, Ticks(3f));
    Check(charged.Accepted && charged.Bolt.Charged && Near(charged.Bolt.Damage, 30f), "charged round doubles basic damage");
    var released = Plan(chargedState, Ticks(3f), SSW.CastKind.Release);
    Check(released.Accepted && released.State.Ammo == 1 && Near(released.Bolt.Speed, 0f), "release neither fires nor consumes ammunition");
    Check(!Plan(charged.State, Ticks(3f)).Accepted, "second press cannot reuse consumed round");
    var rapidState = chargedState;
    rapidState.Ammo = 2;
    rapidState.Loaded.Add(0);
    var rapid = Plan(rapidState, Ticks(3f));
    Check(!Plan(rapid.State, Ticks(3f)).Accepted && Plan(rapid.State, Ticks(3f) + 1).Accepted, "rapid clicks are limited only by one physics tick");
    state = Advance(new SSW.WeaponState { Reload = Ticks(2.5f) }, Ticks(30f));
    Check(state.Ammo == 9 && state.Loaded.Length == 9, "long recharge never exceeds magazine capacity");
    var fullShot = Plan(state, Ticks(35f));
    state = Advance(fullShot.State, Ticks(35f));
    Check(state.Ammo == 9 && state.Loaded[state.Loaded.Length - 1] == Ticks(35f), "full magazine preserves accumulated recharge for one replacement");
    var evolved = Advance(new SSW.WeaponState { Progress = 2, Reload = 1 }, 1);
    Check(evolved.Reload == 1 + Ticks(1.25f), "evolution halves recharge interval");
    var restored = (SSW.WeaponState)Call(gun, "Initial");
    restored.Progress = 2;
    object[] progressArgs = { restored };
    Call(gun, "ProgressChanged", progressArgs);
    restored = (SSW.WeaponState)progressArgs[0];
    Check(restored.Reload == restored.Filled + Ticks(1.25f), "restored evolution applies to the first reload after respawn");
    Check(Advance(restored, restored.Reload).Ammo == 1, "restored evolved player loads first round on time");
    Check(Near(SSW.GunCast.Charge(100, 99), 0f), "future load tick cannot underflow charge display");
    Check(Near(SSW.GunCast.Charge(0, Ticks(3f)), 1f), "charge display reaches full with charged attack threshold");
    var displayed = new SSW.WeaponState { Ammo = 3 };
    uint displayTick = Ticks(6f);
    displayed.Loaded.Add(0);
    displayed.Loaded.Add(displayTick - Ticks(1.5f));
    displayed.Loaded.Add(displayTick);
    view.Draw(true, displayed, displayTick);
    Check(view.VisibleAmmo == 3 && view.ChargedAmmo == 1, "ammo display uses per round authoritative load times");
    view.Draw(false, displayed, displayTick);
    Check(view.VisibleAmmo == 0 && view.ChargedAmmo == 0, "ammo display disappears outside combat");
    view.Draw(true, state, Ticks(40f));
    Check(view.VisibleAmmo == 9 && view.ChargedAmmo == 9, "all nine loaded rounds are visible and charged");
    var muzzle = view.Muzzle(UnityEngine.Vector2.zero, UnityEngine.Vector2.right, 1f);
    var reversed = view.Muzzle(UnityEngine.Vector2.zero, UnityEngine.Vector2.left, 1f);
    Check(UnityEngine.Vector2.Distance(muzzle, new UnityEngine.Vector2(1f, 0.7f)) < 0.001f, "muzzle preserves original right facing offset");
    Check(UnityEngine.Vector2.Distance(reversed, new UnityEngine.Vector2(-1f, 0.7f)) < 0.001f, "muzzle mirrors above gun for left facing shots");
    Check(UnityEngine.Vector2.Distance(view.Muzzle(UnityEngine.Vector2.zero, UnityEngine.Vector2.right, 0.5f), muzzle * 0.5f) < 0.001f, "muzzle follows shrink scale");
    Set(player.Drive, "_state", new SSW.MotionState { Scale = 1f });
    var wall = new UnityEngine.GameObject("Gun wall");
    wall.layer = 8;
    wall.transform.position = new UnityEngine.Vector3(1000.5f, 0f);
    wall.AddComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(0.1f, 4f);
    var motionSo = new UnityEditor.SerializedObject(player.Motion);
    motionSo.FindProperty("_whatIsGround").intValue = 1 << 8;
    motionSo.ApplyModifiedPropertiesWithoutUndo();
    UnityEngine.Physics2D.SyncTransforms();
    var clipped = (UnityEngine.Vector2)Call(gun, "BoltOrigin", new UnityEngine.Vector2(1000f, 0f), UnityEngine.Vector2.right, 0.125f);
    Check(clipped.x < 1000.45f && clipped.x >= 1000f, "muzzle cannot place projectile through a nearby wall");
    var enemy = new UnityEngine.GameObject("Poison target").AddComponent<SSW.NetPlayer>();
    Call(gun, "ApplyPoison", enemy, 7f);
    var poisons = (System.Collections.IList)Read(gun, "_poison");
    object poison = poisons[0];
    float firstTick = (float)Read(poison, "Next");
    Set(poison, "Remaining", 2);
    Set(poison, "Next", firstTick - 0.25f);
    Call(gun, "ApplyPoison", enemy, 14f);
    Check(poisons.Count == 1 && Near((float)Read(poison, "Next"), firstTick - 0.25f), "repeated poison hit preserves next tick instead of postponing it");
    Check((int)Read(poison, "Remaining") == (int)Read(gun, "_poisonTicks") && Near((float)Read(poison, "Damage"), 14f), "repeated poison refreshes remaining ticks and latest charge damage");
    using (var writer = new Unity.Netcode.FastBufferWriter(256, Unity.Collections.Allocator.Temp))
    {
        writer.WriteNetworkSerializable(charged.Bolt);
        var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
        try
        {
            reader.ReadNetworkSerializable(out SSW.BoltSpec received);
            Check(received.Equals(charged.Bolt) && Near(received.Gravity, 2f), "charged ballistic specification survives network serialization");
        }
        finally { reader.Dispose(); }
    }
    using (var writer = new Unity.Netcode.FastBufferWriter(256, Unity.Collections.Allocator.Temp))
    {
        writer.WriteNetworkSerializable(state);
        var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
        try
        {
            reader.ReadNetworkSerializable(out SSW.WeaponState received);
            Check(received.Equals(state) && received.Loaded.Length == 9, "all nine charge timestamps survive network serialization");
        }
        finally { reader.Dispose(); }
    }
    var fxSo = new UnityEditor.SerializedObject(effects);
    var entries = fxSo.FindProperty("_effects");
    Check(entries.arraySize == 8, "all eight bullet effect mappings exist");
    for (int i = 0; i < entries.arraySize; i++)
    {
        var entry = entries.GetArrayElementAtIndex(i);
        foreach (var name in new[] { "Normal", "Charged", "Trail" })
        {
            var effect = (UnityEngine.GameObject)entry.FindPropertyRelative(name).objectReferenceValue;
            Check(effect != null && UnityEditor.AssetDatabase.GetAssetPath(effect).StartsWith("Assets/SSW/Resources/Network/Gun/"), "owned visual mapping " + i + " " + name);
            Check(effect.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true).Length == 0
                && effect.GetComponentsInChildren<UnityEngine.Collider2D>(true).Length == 0
                && effect.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true).Length == 0, "effect has no duplicate gameplay components " + i + " " + name);
        }
    }
    var visual = (UnityEngine.GameObject)entries.GetArrayElementAtIndex(0).FindPropertyRelative("Normal").objectReferenceValue;
    pool.Warm(visual, 1);
    int created = pool.CreatedCount;
    var first = pool.Play(visual, new UnityEngine.Vector3(1040f, 0f), 2f);
    Check(pool.ActiveCount == 1 && pool.CreatedCount == created, "effect play reuses prewarmed object");
    pool.transform.position += UnityEngine.Vector3.right * 5f;
    Call(pool, "Tick", UnityEngine.Time.time);
    Check(Near(first.transform.position.x, 1040f), "world impact stays fixed when pool owner moves");
    obj.transform.localScale = UnityEngine.Vector3.one * 0.5f;
    Call(pool, "Tick", UnityEngine.Time.time);
    Check(UnityEngine.Vector3.Distance(first.transform.lossyScale, visual.transform.localScale) < 0.001f, "impact world scale remains fixed when player shrinks");
    pool.Release(first);
    pool.Release(first);
    Check(pool.ActiveCount == 0 && pool.PooledCount == created, "effect release is idempotent");
    var reused = pool.Play(visual, new UnityEngine.Vector3(1050f, 0f), 1f);
    Check(reused == first && reused.activeSelf, "returned effect is reset and reused");
    Call(pool, "Tick", UnityEngine.Time.time + 2f);
    Check(pool.ActiveCount == 0 && !reused.activeSelf, "expired effect returns to pool");
    var anchor = new UnityEngine.GameObject("Effect anchor").transform;
    pool.Play(visual, anchor.position, 2f, anchor);
    UnityEngine.Object.DestroyImmediate(anchor.gameObject);
    Call(pool, "Tick", UnityEngine.Time.time);
    Check(pool.ActiveCount == 0, "destroyed effect target releases follower safely");
    var shot = new UnityEngine.GameObject("Trail source").AddComponent<UnityEngine.SpriteRenderer>();
    shot.enabled = false;
    pool.Trail(visual, shot);
    Call(pool, "Tick", UnityEngine.Time.time);
    Check(pool.ActiveCount == 1, "hidden arriving projectile waits before displaying trail");
    shot.enabled = true;
    Call(pool, "Tick", UnityEngine.Time.time);
    shot.enabled = false;
    Call(pool, "Tick", UnityEngine.Time.time);
    Call(pool, "Tick", UnityEngine.Time.time + 0.7f);
    Check(pool.ActiveCount == 0, "matched or hidden projectile stops and recycles its trail");
    var ghostPrefab = new UnityEngine.GameObject("Ghost prefab");
    ghostPrefab.AddComponent<UnityEngine.SpriteRenderer>();
    shot.transform.SetParent(obj.transform, false);
    pool.Image(ghostPrefab, shot, 1f, UnityEngine.Color.white);
    var active = (System.Collections.IList)Read(pool, "_active");
    var ghost = (UnityEngine.GameObject)Read(active[0], "Object");
    Check(UnityEngine.Vector3.Distance(ghost.transform.lossyScale, shot.transform.lossyScale) < 0.001f, "ghost preserves shrunk source world scale exactly once");
    obj.transform.localScale = UnityEngine.Vector3.one;
    Call(pool, "Tick", UnityEngine.Time.time);
    Check(Near(ghost.transform.lossyScale.x, 0.5f), "existing ghost keeps captured size when source grows back");
    pool.Release(ghost);
    var reusedGhost = pool.Play(ghostPrefab, UnityEngine.Vector3.zero, 1f);
    Check(Near(reusedGhost.transform.lossyScale.x, 1f), "reused ghost resets original prefab scale");
    pool.Clear();
    var flight = new UnityEngine.GameObject("Gun flight").AddComponent<SSW.ShotSync>();
    var pose = new SSW.ShotPose { Time = 10, Velocity = UnityEngine.Vector2.right * 30f, Gravity = new UnityEngine.Vector2(0f, -20f), Angle = 17f, Spin = 30f };
    flight.AlignVelocity = true;
    Check(Near((float)Call(flight, "Angle", pose, 10.5d), UnityEngine.Mathf.Atan2(-10f, 30f) * UnityEngine.Mathf.Rad2Deg), "remote ballistic rotation follows gravity during extrapolation");
    flight.AlignVelocity = false;
    Check(Near((float)Call(flight, "Angle", pose, 10.5d), 32f), "cards and spinning shots retain original rotation");
    pool.Play(visual, UnityEngine.Vector3.zero, 3f);
    pool.Play(visual, UnityEngine.Vector3.one, 3f);
    pool.Clear();
    Check(pool.ActiveCount == 0 && pool.PooledCount == pool.CreatedCount, "round cleanup returns every active effect");
    return new { passed = checks.Count, checks };
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
