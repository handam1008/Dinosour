var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var checks = new System.Collections.Generic.List<string>();
var assets = new System.Collections.Generic.List<UnityEngine.Object>();
const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Check(bool ok, string label)
{
    if (!ok) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
void Set(object target, string name, object value) => target.GetType().GetField(name, fields).SetValue(target, value);
object Read(object target, string name) => target.GetType().GetField(name, fields).GetValue(target);
object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, fields).Invoke(target, args);
bool Near(float left, float right) => UnityEngine.Mathf.Abs(left - right) < 0.001f;
try
{
    var obj = new UnityEngine.GameObject("Health checks");
    var player = obj.AddComponent<SSW.NetPlayer>();
    var health = obj.AddComponent<SSW.Health>();
    var source = obj.AddComponent<SSW.AugmentDrafter>();
    var buff = obj.AddComponent<SSW.BuffHealth>();
    Set(player, "_health", health);
    Set(health, "current", 100f);
    Set(health, "_disableOnDeath", false);
    Set(health, "_healthBarResourceName", "");
    Set(health, "_damageNumberResourceName", "");
    Set(buff, "_player", player);
    Set(buff, "_source", source);
    typeof(Unity.Netcode.NetworkBehaviour).GetProperty("IsServer").SetValue(player, true);
    Call(buff, "Awake");
    void Reset()
    {
        ((System.Collections.Generic.HashSet<SSW.CommonAugmentType>)Read(buff, "_owned")).Clear();
        ((System.Collections.Generic.List<SSW.Augment>)Read(source, "_owned")).Clear();
        Call(buff, "ResetLife");
        health.maxHealth = 100f;
        Set(health, "current", 100f);
        buff.MaxScale = 1f;
    }
    void Grant(SSW.CommonAugmentType type)
    {
        var augment = UnityEngine.ScriptableObject.CreateInstance<SSW.CommonAugment>();
        augment.type = type;
        assets.Add(augment);
        Call(source, "ApplyGrant", augment);
    }
    int Pending() => ((System.Collections.IList)Read(buff, "_pending")).Count;
    void Tick()
    {
        var pending = (System.Collections.IList)Read(buff, "_pending");
        for (int i = 0; i < pending.Count; i++)
        {
            object entry = pending[i];
            entry.GetType().GetField("Next").SetValue(entry, UnityEngine.Time.time - 1f);
            pending[i] = entry;
        }
        Call(buff, "TickDamage");
    }
    Grant(SSW.CommonAugmentType.Giant);
    Check(Near(health.Max, 155f) && Near(health.Current, 155f) && Near(buff.Scale, 1.1f), "giant grants 55 percent health and 10 percent size");
    Grant(SSW.CommonAugmentType.Giant);
    Check(Near(health.Max, 155f) && Near(buff.Scale, 1.1f), "distinct assets of the same type cannot stack");
    Grant(SSW.CommonAugmentType.GlassCannon);
    Grant(SSW.CommonAugmentType.Phoenix);
    Grant(SSW.CommonAugmentType.DeathWaltz);
    Grant(SSW.CommonAugmentType.Versatile);
    Check(Near(health.Max, 116.36625f) && Near(buff.Scale, 0.9108f), "combined health and size use latest controller settings");
    Check(Near(buff.ModifyOutgoingDamage(10f), 18f), "glass cannon uses 1.8 damage multiplier");
    Check(Near((float)Read(buff, "_phoenixImmune"), 2.5f) && Near((float)Read(buff, "_phoenixLock"), 1f), "phoenix uses latest invulnerability and lock durations");
    var types = new[] { SSW.CommonAugmentType.Giant, SSW.CommonAugmentType.GlassCannon, SSW.CommonAugmentType.Phoenix, SSW.CommonAugmentType.DeathWaltz, SSW.CommonAugmentType.Versatile, SSW.CommonAugmentType.TenLives };
    for (int offset = 0; offset < types.Length; offset++)
    {
        Reset();
        for (int i = 0; i < types.Length; i++) Grant(types[(i + offset) % types.Length]);
        Check(Near(health.Max, 10f) && Near(health.Current, 10f), "ten lives restore order " + offset);
        buff.MaxScale = 2f;
        Check(Near(health.Max, 10f), "ten lives overrides jackpot health " + offset);
    }
    Reset();
    Grant(SSW.CommonAugmentType.Giant);
    Set(health, "current", 77.5f);
    buff.MaxScale = 2f;
    Check(Near(health.Max, 310f) && Near(health.Current, 155f), "job health multiplier preserves current health ratio");
    buff.MaxScale = 1f;
    Check(Near(health.Max, 155f) && Near(health.Current, 77.5f), "job multiplier expiry restores augmented health");
    Reset();
    Grant(SSW.CommonAugmentType.Berserker);
    Set(health, "current", 75f);
    Check(buff.Berserker && Near(buff.SpeedScale, 1.45f) && Near(buff.ModifyOutgoingDamage(10f), 16f), "berserker activates at 75 percent health");
    Set(health, "current", 75.01f);
    Check(!buff.Berserker && Near(buff.SpeedScale, 1f), "berserker stops above threshold");
    Set(buff, "_confidenceUntil", UnityEngine.Time.time + 2f);
    Check(Near(buff.SpeedScale, 1.3f), "confidence adds 30 percent movement speed");
    Set(health, "current", 70f);
    Check(Near(buff.SpeedScale, 1.885f), "confidence combines with berserker");
    Reset();
    Grant(SSW.CommonAugmentType.Multiscale);
    var hit = new SSW.DamageRequest(null, 20f, SSW.DamageTag.BasicAttack);
    var first = health.ReceiveDamage(hit);
    var second = health.ReceiveDamage(hit);
    Check(Near(first.AppliedAmount, 10f) && Near(second.AppliedAmount, 20f), "multiscale halves only full health hit");
    Reset();
    Grant(SSW.CommonAugmentType.TenLives);
    var fixedHit = health.ReceiveDamage(hit);
    var bypass = health.ReceiveDamage(new SSW.DamageRequest(null, 3f, SSW.DamageTag.IgnoreDefense));
    Check(Near(fixedHit.AppliedAmount, 1f) && Near(bypass.AppliedAmount, 3f), "ten lives fixes external hits and preserves bypass damage");
    Reset();
    Grant(SSW.CommonAugmentType.Phoenix);
    Grant(SSW.CommonAugmentType.DeathWaltz);
    Check(Near(buff.ModifyIncomingDamage(hit, 0f), 0f) && !(bool)Read(buff, "_phoenixUsed") && Pending() == 0, "blocked zero damage neither revives nor queues damage");
    Set(buff, "_immuneUntil", UnityEngine.Time.time + 2.5f);
    Check(Near(buff.ModifyIncomingDamage(new SSW.DamageRequest(null, 100f, SSW.DamageTag.IgnoreDefense), 100f), 0f), "phoenix protects from deferred bypass ticks");
    Reset();
    Grant(SSW.CommonAugmentType.DeathWaltz);
    var deferred = health.ReceiveDamage(new SSW.DamageRequest(null, 50f, SSW.DamageTag.BasicAttack));
    Check(deferred.WasDeferred && deferred.WasAccepted && !deferred.WasBlocked && !deferred.WasApplied && Near(health.Current, 130f) && Pending() == 1, "death waltz defers incoming damage");
    for (int i = 0; i < 5; i++) Tick();
    Check(Near(health.Current, 80f) && Pending() == 0, "five deferred ticks apply original damage once without requeue");
    Set(health, "current", 4f);
    bool died = false;
    health.OnDied += () => died = true;
    health.ReceiveDamage(new SSW.DamageRequest(null, 25f, SSW.DamageTag.BasicAttack));
    Tick();
    Check(died && Near(health.Current, 0f), "deferred damage remains lethal");
    Reset();
    Grant(SSW.CommonAugmentType.DeathWaltz);
    health.ReceiveDamage(hit);
    Set(buff, "_regenTime", 0.9f);
    Set(buff, "_confidenceUntil", UnityEngine.Time.time + 2f);
    Call(buff, "ClearTimed");
    Check(Pending() == 0 && Near((float)Read(buff, "_regenTime"), 0f) && Near(buff.SpeedScale, 1f), "inactive phase clears deferred damage and timers");
    Reset();
    Grant(SSW.CommonAugmentType.Regeneration);
    Set(health, "current", 50f);
    Set(buff, "_regenTime", 1f);
    Call(buff, "TickRegen");
    Check(Near(health.Current, 51f), "regeneration restores one health per tick");
    Grant(SSW.CommonAugmentType.DeathWaltz);
    typeof(Unity.Netcode.NetworkBehaviour).GetProperty("IsServer").SetValue(player, false);
    Check(Near(buff.ModifyIncomingDamage(hit, 20f), 20f) && !buff.TryDefer(hit, 20f) && Pending() == 0, "client cannot defer or alter incoming damage");
    Grant(SSW.CommonAugmentType.Giant);
    Check(Near(health.Max, 130f), "client card replay cannot change authoritative health");
    return new { passed = checks.Count, checks };
}
finally
{
    foreach (UnityEngine.Object asset in assets) UnityEngine.Object.DestroyImmediate(asset);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
