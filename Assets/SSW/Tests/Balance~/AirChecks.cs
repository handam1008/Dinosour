if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play mode required");
var game = SSW.NetGame.Current;
if (game == null || game.Practice == null || !game.Manager.IsServer) throw new System.InvalidOperationException("Gunner practice host required");
var player = game.Practice.Player;
var target = game.Practice.Target;
if (player.Job != SSW.PlayerJob.Gunner) throw new System.InvalidOperationException("Gunner required");
var gun = player.GetComponent<SSW.GunCast>();
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
var balance = (SSW.GunBalance)Read(gun, "_balance");
var augments = (System.Collections.Generic.HashSet<KDH.Scripts.Arguments.GunnerAugmentType>)Read(gun, "_augments");
var savedAugments = System.Linq.Enumerable.ToArray(augments);
var savedMotion = (SSW.MotionState)Read(target.Drive, "_state");
float savedMass = target.Body.mass;
float savedHealth = target.Health.Current;
var checks = new System.Collections.Generic.List<string>();
var cases = new System.Collections.Generic.List<object>();
var temporaryBalance = UnityEngine.Object.Instantiate(balance);
var boltObject = new UnityEngine.GameObject("Air hit specification");
boltObject.AddComponent<Unity.Netcode.NetworkObject>();
var bolt = boltObject.AddComponent<SSW.NetBolt>();
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
bool Near(float a, float b) => UnityEngine.Mathf.Abs(a - b) < 0.002f;
string error = null;
try
{
    Check(player.IsServer && target.IsServer && player.CanAct && target.CanAct, "spawned server actors can fight");
    Check(balance == UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunBalance>("Assets/SSW/Resources/Network/GunBalance.asset"), "spawned gun uses saved balance reference");
    Check(Near(balance.AirForce, 10f), "saved air force is ten");
    var inputs = new[]
    {
        (Grounded: false, Charged: false, HasAir: true, Mass: 1f, Force: 10f),
        (Grounded: false, Charged: true, HasAir: true, Mass: 1f, Force: 10f),
        (Grounded: true, Charged: false, HasAir: true, Mass: 1f, Force: 10f),
        (Grounded: true, Charged: true, HasAir: true, Mass: 1f, Force: 10f),
        (Grounded: false, Charged: false, HasAir: false, Mass: 1f, Force: 10f),
        (Grounded: false, Charged: false, HasAir: true, Mass: 2f, Force: 10f),
        (Grounded: false, Charged: false, HasAir: true, Mass: 1f, Force: 7f)
    };
    foreach (var input in inputs)
    {
        augments.Clear();
        if (input.HasAir) augments.Add(KDH.Scripts.Arguments.GunnerAugmentType.AirBullet);
        target.Body.mass = input.Mass;
        target.Health.Heal(target.Health.Max);
        var state = savedMotion;
        state.Velocity = new UnityEngine.Vector2(2f, -3f);
        state.Grounded = input.Grounded;
        Set(target.Drive, "_state", state);
        temporaryBalance.Replace(balance.FireDamage, balance.GravityForce, balance.IceSlow, balance.PoisonDamage, balance.ShurikenDamage, input.Force);
        Set(gun, "_balance", input.Force == 10f ? balance : temporaryBalance);
        float scale = input.Charged ? player.Stats.ChargeDamage : 1f;
        var spec = new SSW.BoltSpec { Damage = player.Stats.Damage * scale, Charged = input.Charged, Scale = 1f, Aspect = 1f };
        Set(Read(bolt, "_spec"), "m_InternalValue", spec);
        float before = target.Health.Current;
        gun.Hit(target, bolt);
        state = (SSW.MotionState)Read(target.Drive, "_state");
        float expected = input.HasAir && !input.Grounded ? input.Force * scale / input.Mass : -3f;
        string label = "case " + cases.Count;
        Check(Near(state.Velocity.y, expected), label + " correct launch or no launch");
        Check(Near(state.Velocity.x, 2f), label + " horizontal velocity preserved");
        Check(Near(before - target.Health.Current, spec.Damage), label + " original shot damage preserved");
        Check(state.Grounded == input.Grounded, label + " grounded condition preserved");
        cases.Add(new { input.Grounded, input.Charged, input.HasAir, input.Mass, input.Force, scale, expected, actual = state.Velocity.y, damage = before - target.Health.Current });
    }
}
catch (System.Exception failure) { error = failure.ToString(); }
finally
{
    Set(gun, "_balance", balance);
    Set(target.Drive, "_state", savedMotion);
    target.Body.mass = savedMass;
    target.Health.Heal(UnityEngine.Mathf.Max(0f, savedHealth - target.Health.Current));
    augments.Clear();
    foreach (var type in savedAugments) augments.Add(type);
    UnityEngine.Object.Destroy(boltObject);
    UnityEngine.Object.Destroy(temporaryBalance);
}
const string log = "Logs/Air27/Runtime.json";
System.IO.Directory.CreateDirectory("Logs/Air27");
var result = new { success = error == null, error, count = checks.Count, checks, cases, method = "Unity Editor practice server GunCast.Hit with injected BoltSpec, motion, and augment states" };
System.IO.File.WriteAllText(log, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
