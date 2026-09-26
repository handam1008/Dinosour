if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play mode required");
var game = SSW.NetGame.Current;
if (game == null || game.Practice == null || !game.Manager.IsServer) throw new System.InvalidOperationException("Gunner practice host required");
var player = game.Practice.Player;
var target = game.Practice.Target;
if (player.Job != SSW.PlayerJob.Gunner) throw new System.InvalidOperationException("Gunner required");
var gun = player.GetComponent<SSW.GunCast>();
var balance = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunBalance>("Assets/SSW/Resources/Network/GunBalance.asset");
const string log = "Logs/Balance26/RuntimeChecks.json";
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
System.Reflection.FieldInfo Field(object item, string name)
{
    for (var type = item.GetType(); type != null; type = type.BaseType)
    {
        var field = type.GetField(name, flags);
        if (field != null) return field;
    }
    throw new System.MissingFieldException(name);
}
object Read(object item, string name) => Field(item, name).GetValue(item);
void Set(object item, string name, object value) => Field(item, name).SetValue(item, value);
bool injectedBinding = (SSW.GunBalance)Read(gun, "_balance") == null;
bool savedPrefabBinding = (SSW.GunBalance)Read(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab").GetComponent<SSW.GunCast>(), "_balance") == balance;
if (injectedBinding) Set(gun, "_balance", balance);
string original = UnityEngine.JsonUtility.ToJson(balance);
var checks = new System.Collections.Generic.List<string>();
var cases = new System.Collections.Generic.List<object>();
var hits = new System.Collections.Generic.List<float>();
var times = new System.Collections.Generic.List<float>();
var types = new[] { KDH.Scripts.Arguments.GunnerAugmentType.FireBullet, KDH.Scripts.Arguments.GunnerAugmentType.GravityBullet,
    KDH.Scripts.Arguments.GunnerAugmentType.IceBullet, KDH.Scripts.Arguments.GunnerAugmentType.PoisonBullet, KDH.Scripts.Arguments.GunnerAugmentType.ShurikenBullet };
var augments = (System.Collections.Generic.HashSet<KDH.Scripts.Arguments.GunnerAugmentType>)Read(gun, "_augments");
var savedAugments = System.Linq.Enumerable.ToArray(augments);
var poison = (System.Collections.IList)Read(gun, "_poison");
var temporary = new System.Collections.Generic.List<UnityEngine.GameObject>();
var map = game.Arena.Map != null ? game.Arena.Map.gameObject : (UnityEngine.GameObject)new UnityEditor.SerializedObject(game.Arena).FindProperty("_baseMap").objectReferenceValue;
var mapShapes = map.GetComponentsInChildren<UnityEngine.Collider2D>(true);
var enabled = System.Array.ConvertAll(mapShapes, shape => shape.enabled);
void Record(float amount, bool critical) { hits.Add(amount); times.Add(UnityEngine.Time.time); }
void Save(string status, string error = null)
{
    System.IO.File.WriteAllText(log, Newtonsoft.Json.JsonConvert.SerializeObject(new { status, error, count = checks.Count, checks, cases,
        host = "single Unity Editor practice host with spawned dummy", injectedBinding, savedPrefabBinding,
        caseMethod = "direct authoritative Hit with injected BoltSpec/augment; final case uses Cast.Attack and actual projectile collision" }, Newtonsoft.Json.Formatting.Indented));
}
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
bool Near(float a, float b, float tolerance = 0.002f) => UnityEngine.Mathf.Abs(a - b) <= tolerance;
async System.Threading.Tasks.Task Wait(float seconds)
{
    float finish = UnityEngine.Time.time + seconds;
    double end = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds + 10d;
    while (UnityEngine.Time.time < finish)
    {
        if (!UnityEditor.EditorApplication.isPlaying || UnityEngine.Time.realtimeSinceStartupAsDouble > end) throw new System.TimeoutException("Runtime wait");
        await System.Threading.Tasks.Task.Delay(16);
    }
}
async System.Threading.Tasks.Task Run()
{
    string error = null;
    target.Health.OnDamageDealt += Record;
    try
    {
        if (target.TryGetComponent<SSW.DummyRegen>(out var regen)) UnityEngine.Object.Destroy(regen);
        foreach (var shape in mapShapes) shape.enabled = false;
        foreach (var effect in map.GetComponentsInChildren<UnityEngine.ParticleSystem>(true)) effect.Stop(true, UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
        var floor = new UnityEngine.GameObject("Balance test floor");
        temporary.Add(floor);
        floor.layer = 8;
        floor.transform.position = new UnityEngine.Vector3(0f, 0f);
        floor.AddComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(100f, 1f);
        typeof(SSW.Health).GetMethod("SetMax", flags).Invoke(target.Health, new object[] { 1000f });
        target.Health.Heal(1000f);
        player.Drive.Teleport(new UnityEngine.Vector2(-3f, 2f));
        target.Drive.Teleport(new UnityEngine.Vector2(3f, 2f));
        await Wait(0.8f);
        Check(player.IsServer && target.IsServer && player.IsSpawned && target.IsSpawned && player.CanAct && target.CanAct, "actual spawned server actors can fight");
        var boltObject = new UnityEngine.GameObject("Balance hit specification");
        temporary.Add(boltObject);
        boltObject.AddComponent<Unity.Netcode.NetworkObject>();
        var bolt = boltObject.AddComponent<SSW.NetBolt>();
        for (int profile = 0; profile < 2; profile++)
        {
            if (profile == 1) balance.Replace(4f, 13f, 0.6f, 3f, 5f);
            for (int chargedIndex = 0; chargedIndex < (profile == 0 ? 2 : 1); chargedIndex++)
            {
                bool charged = chargedIndex == 1;
                float scale = charged ? player.Stats.ChargeDamage : 1f;
                foreach (var type in types)
                {
                    gun.StopAllCoroutines();
                    poison.Clear();
                    augments.Clear();
                    augments.Add(type);
                    target.Health.Heal(1000f);
                    player.Drive.Teleport(new UnityEngine.Vector2(-3f, 2f));
                    target.Drive.Teleport(new UnityEngine.Vector2(3f, 2f));
                    await Wait(0.05f);
                    float distance = UnityEngine.Vector2.Distance(player.Body.position, target.Body.position);
                    var direction = (target.Body.position - player.Body.position).normalized;
                    float before = target.Health.Current;
                    var spec = new SSW.BoltSpec { Damage = player.Stats.Damage * scale, Charged = charged, Scale = 1f, Aspect = 1f };
                    Set(Read(bolt, "_spec"), "m_InternalValue", spec);
                    hits.Clear(); times.Clear();
                    float at = UnityEngine.Time.time;
                    gun.Hit(target, bolt);
                    string label = "profile=" + profile + " " + type + " charged=" + charged;
                    float extra = 0f;
                    int count = 1;
                    if (type == KDH.Scripts.Arguments.GunnerAugmentType.FireBullet)
                    {
                        extra = balance.FireDamage * scale * 6f;
                        count += 6;
                        await Wait(3.35f);
                        for (int i = 1; i < hits.Count; i++) Check(Near(hits[i], balance.FireDamage * scale), label + " fire tick " + i);
                        Check(times.Count == 7 && times[1] - at >= 0.48f && times[6] - at >= 2.98f, label + " original six delayed ticks");
                    }
                    else if (type == KDH.Scripts.Arguments.GunnerAugmentType.GravityBullet)
                    {
                        var state = (SSW.MotionState)Read(target.Drive, "_state");
                        Check(Near(state.External, direction.x * balance.GravityForce * scale / target.Body.mass), label + " exact impulse per mass");
                        await Wait(1.5f);
                        var settled = (SSW.MotionState)Read(target.Drive, "_state");
                        Check(UnityEngine.Mathf.Abs(settled.External) < UnityEngine.Mathf.Abs(state.External), label + " force decays");
                    }
                    else if (type == KDH.Scripts.Arguments.GunnerAugmentType.IceBullet)
                    {
                        Check(Near(target.Motion.CurrentMoveSpeedMultiplier, UnityEngine.Mathf.Clamp01(1f - balance.IceSlow * scale)), label + " exact slow multiplier");
                        await Wait(1.5f * scale + 0.15f);
                        Check(Near(target.Motion.CurrentMoveSpeedMultiplier, 1f), label + " slow expires and restores");
                    }
                    else if (type == KDH.Scripts.Arguments.GunnerAugmentType.PoisonBullet)
                    {
                        extra = balance.PoisonDamage * scale * 8f;
                        count += 8;
                        await Wait(4.35f);
                        for (int i = 1; i < hits.Count; i++) Check(Near(hits[i], balance.PoisonDamage * scale), label + " poison tick " + i);
                        Check(poison.Count == 0 && times.Count == 9 && times[8] - at >= 3.98f, label + " original eight ticks end");
                    }
                    else if (type == KDH.Scripts.Arguments.GunnerAugmentType.ShurikenBullet)
                    {
                        extra = distance * balance.ShurikenDamage * scale;
                        count++;
                    }
                    Check(hits.Count == count, label + " damage event count=" + hits.Count);
                    Check(Near(before - target.Health.Current, spec.Damage + extra, 0.02f), label + " total damage=" + (before - target.Health.Current));
                    cases.Add(new { profile, type = type.ToString(), charged, scale, distance, damage = before - target.Health.Current, hits = hits.ToArray(), times = times.ToArray() });
                    Save("running");
                }
            }
        }
        UnityEngine.JsonUtility.FromJsonOverwrite(original, balance);
        augments.Clear(); augments.Add(KDH.Scripts.Arguments.GunnerAugmentType.FireBullet);
        target.Health.Heal(1000f);
        player.Drive.Teleport(new UnityEngine.Vector2(-3f, 2f));
        target.Drive.Teleport(new UnityEngine.Vector2(3f, 2f));
        await Wait(0.8f);
        var stateForInput = new SSW.WeaponState { Ammo = 1, Reload = player.InputSequence + 10000, Filled = player.InputSequence };
        stateForInput.Loaded.Add(player.InputSequence);
        Set(Read(gun, "_state"), "m_InternalValue", stateForInput);
        hits.Clear(); times.Clear();
        float initial = target.Health.Current;
        UnityEngine.Vector2 aim = ((UnityEngine.Vector2)target.Collider.bounds.center - gun.View.Muzzle(player.Body.position, UnityEngine.Vector2.right, player.Drive.Scale)).normalized;
        player.Cast.Attack(true, aim);
        player.Cast.Attack(false, aim);
        await Wait(4f);
        Check(hits.Count == 7 && Near(initial - target.Health.Current, player.Stats.Damage + 6f, 0.02f), "Cast.Attack spawns projectile whose real collision applies fire once");
        cases.Add(new { profile = 0, type = "actual projectile", charged = false, damage = initial - target.Health.Current, hits = hits.ToArray() });
    }
    catch (System.Exception failure) { error = failure.ToString(); }
    finally
    {
        target.Health.OnDamageDealt -= Record;
        gun.StopAllCoroutines();
        poison.Clear();
        augments.Clear(); foreach (var type in savedAugments) augments.Add(type);
        UnityEngine.JsonUtility.FromJsonOverwrite(original, balance);
        for (int i = 0; i < mapShapes.Length; i++) if (mapShapes[i] != null) mapShapes[i].enabled = enabled[i];
        foreach (var item in temporary) if (item != null) UnityEngine.Object.Destroy(item);
        Save(error == null ? "passed" : "failed", error);
    }
}
_ = Run();
return new { running = true, log, injectedBinding };
