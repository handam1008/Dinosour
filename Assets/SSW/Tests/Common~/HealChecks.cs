var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
int checks = 0;
void Check(bool value, string name)
{
    if (!value) throw new System.InvalidOperationException(name);
    checks++;
}
try
{
    var actor = new UnityEngine.GameObject("Heal check");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor, scene);
    var health = actor.AddComponent<SSW.Health>();
    typeof(SSW.Health).GetField("_damageNumberResourceName", flags).SetValue(health, "");
    var restored = new System.Collections.Generic.List<float>();
    health.OnHealed += restored.Add;
    void Set(float value) => typeof(SSW.Health).GetField("current", flags).SetValue(health, value);
    Set(60f);
    health.Heal(7.5f);
    Check(health.Current == 67.5f && restored.Count == 1 && restored[0] == 7.5f, "fractional actual recovery");
    Set(97f);
    health.Heal(20f);
    Check(health.Current == 100f && restored.Count == 2 && restored[1] == 3f, "overheal is capped");
    health.Heal(20f);
    Check(restored.Count == 2, "full health has no healing event");
    foreach (float amount in new[] { 0f, -5f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
    {
        health.Heal(amount);
        Check(restored.Count == 2 && health.Current == 100f, "invalid amount ignored");
    }
    typeof(SSW.Health).GetMethod("SetMax", flags).Invoke(health, new object[] { 200f });
    Check(restored.Count == 2 && health.Current == 200f, "maximum change is not a heal");
    typeof(SSW.Health).GetMethod("ApplyNetworkState", flags).Invoke(health, new object[] { 190f, 200f });
    typeof(SSW.Health).GetMethod("ApplyNetworkState", flags).Invoke(health, new object[] { 200f, 200f });
    Check(restored.Count == 2, "network snapshots are not healing events");
    var authority = actor.AddComponent<SSW.NetHealth>();
    typeof(SSW.Health).GetField("_authority", flags).SetValue(health, authority);
    Set(50f);
    health.Heal(20f);
    Check(restored.Count == 2 && health.Current == 50f, "unspawned client cannot heal");
    Set(0f);
    health.Heal(20f);
    Check(restored.Count == 2 && health.Current == 0f, "dead network player cannot heal");
    return new { passed = checks };
}
finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
