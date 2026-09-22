var game = SSW.NetGame.Current;
if (!UnityEngine.Application.isPlaying || game == null || !game.Connected || !game.Manager.IsServer || !game.CanFight)
    throw new System.InvalidOperationException("Run in a playing host match.");
var caster = game.Players[0];
var wall = new UnityEngine.GameObject("Sweep Fixture");
SSW.NetPotion shot = null;
var checks = new System.Collections.Generic.List<string>();
try
{
    int mask = caster.GroundMask.value;
    if (mask == 0) throw new System.InvalidOperationException("The caster has no terrain layer.");
    int layer = 0;
    while ((mask & (1 << layer)) == 0) layer++;
    wall.layer = layer;
    wall.transform.position = new UnityEngine.Vector3(1000f, 1000f);
    var shape = wall.AddComponent<UnityEngine.BoxCollider2D>();
    shape.size = new UnityEngine.Vector2(0.02f, 4f);
    var prefab = UnityEngine.Resources.Load<SSW.NetPotion>("Network/Potion");
    var step = typeof(SSW.NetPotion).GetMethod("FixedUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    foreach (int bounces in new[] { 0, 1 })
    {
        shot = UnityEngine.Object.Instantiate(prefab, new UnityEngine.Vector3(999f, 1000f), UnityEngine.Quaternion.identity);
        var mods = new RYU._01.Script.Potions.PotionModifiers(1f, 1f, 1f, false, bounces);
        shot.Init(caster, 0, UnityEngine.Vector2.right * 100f, mods);
        shot.NetworkObject.Spawn();
        var body = shot.GetComponent<UnityEngine.Rigidbody2D>();
        body.position = new UnityEngine.Vector2(1001f, 1000f);
        UnityEngine.Physics2D.SyncTransforms();
        if (shot.GetComponent<UnityEngine.Collider2D>().Distance(shape).isOverlapped)
            throw new System.Exception("Fixture must cross the wall without ending inside it.");
        step.Invoke(shot, null);
        if (bounces == 0)
        {
            if (shot.IsSpawned) throw new System.Exception("A potion crossed thin terrain without an impact.");
            checks.Add("Swept terrain contact ends the potion without a trigger overlap.");
        }
        else
        {
            if (!shot.IsSpawned || body.linearVelocity.x >= 0f || body.position.x >= 1000f)
                throw new System.Exception("A bouncing potion did not reflect at the first terrain contact.");
            checks.Add("Swept terrain contact reflects a bouncing potion on the near side.");
        }
        if (shot != null && shot.IsSpawned) shot.NetworkObject.Despawn();
        shot = null;
    }
    return checks;
}
finally
{
    if (shot != null && shot.IsSpawned) shot.NetworkObject.Despawn();
    UnityEngine.Object.Destroy(wall);
}
