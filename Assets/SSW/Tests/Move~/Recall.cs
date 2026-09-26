var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var checks = new System.Collections.Generic.List<string>();
const string output = "Logs/Blade26/Recall.txt";
System.IO.File.WriteAllText(output, "");
void Check(bool ok, string label)
{
    System.IO.File.AppendAllText(output, (ok ? "PASS " : "FAIL ") + label + "\n");
    if (!ok) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
void Field(object target, string name, object value)
{
    target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
}
try
{
    var obj = new UnityEngine.GameObject("Recall Player");
    var owner = obj.AddComponent<SSW.NetPlayer>();
    var motion = obj.AddComponent<SSW.PlayerController>();
    Field(owner, "_motion", motion);
    using (var data = new UnityEditor.SerializedObject(motion))
    {
        data.FindProperty("_whatIsGround").intValue = 1 << 8;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    var knife = new UnityEngine.GameObject("Recall Knife");
    var body = knife.AddComponent<UnityEngine.Rigidbody2D>();
    body.bodyType = UnityEngine.RigidbodyType2D.Kinematic;
    var bolt = knife.AddComponent<SSW.NetBolt>();
    var spec = new SSW.BoltSpec { Speed = 25f, Radius = 0.12f, Stick = true };
    Field(bolt, "_body", body);
    Field(bolt, "_spec", new Unity.Netcode.NetworkVariable<SSW.BoltSpec>(spec));
    bolt.Init(owner, null, 1, UnityEngine.Vector2.right, spec, 0d);
    body.position = new UnityEngine.Vector2(1000f, 10f);
    body.linearVelocity = UnityEngine.Vector2.right * 25f;
    Field(bolt, "_age", 0.5f);
    void Point(UnityEngine.Vector2 request, float window, UnityEngine.Vector2 expected, string label)
    {
        bolt.Recall(request, window, out var point, out _);
        Check(UnityEngine.Vector2.Distance(point, expected) < 0.001f, label);
    }
    Point(new UnityEngine.Vector2(1003f, 10f), 0.2f, new UnityEngine.Vector2(1003f, 10f), "forward request within latency window");
    Point(new UnityEngine.Vector2(1003f, 12f), 0.2f, new UnityEngine.Vector2(1003f, 10f), "lateral request projects to server flight");
    Point(new UnityEngine.Vector2(1005.1f, 10f), 0.2f, body.position, "forward request outside latency window rejected");
    Point(new UnityEngine.Vector2(998f, 10f), 0.2f, new UnityEngine.Vector2(998f, 10f), "backward request within latency window");
    Point(new UnityEngine.Vector2(994f, 10f), 0.2f, body.position, "backward request outside latency window rejected");
    Point(new UnityEngine.Vector2(float.NaN, 10f), 0.2f, body.position, "NaN request rejected");
    Point(new UnityEngine.Vector2(float.PositiveInfinity, 10f), 0.2f, body.position, "infinite request rejected");
    Point(new UnityEngine.Vector2(1009f, 10f), 5f, body.position, "large RTT remains capped at 350 ms");
    Field(bolt, "_age", 0.02f);
    Point(new UnityEngine.Vector2(999f, 10f), 0.2f, body.position, "request before launch origin rejected");
    Field(bolt, "_age", 0.5f);
    Field(bolt, "_stuck", true);
    Field(bolt, "_normal", UnityEngine.Vector2.up);
    bolt.Recall(new UnityEngine.Vector2(1003f, 10f), 0.2f, out var stuck, out var normal);
    Check(stuck == body.position && normal == UnityEngine.Vector2.up, "stuck blade preserves impact point and normal");
    Field(bolt, "_stuck", false);
    Field(bolt, "_normal", UnityEngine.Vector2.zero);
    Field(bolt, "_bounces", -1);
    Point(new UnityEngine.Vector2(1003f, 10f), 0.2f, body.position, "redirected flight uses server point");
    Field(bolt, "_bounces", 0);
    var block = new UnityEngine.GameObject("Recall Wall");
    block.layer = 8;
    block.transform.position = new UnityEngine.Vector3(1002f, 10f);
    var box = block.AddComponent<UnityEngine.BoxCollider2D>();
    box.size = new UnityEngine.Vector2(0.1f, 8f);
    UnityEngine.Physics2D.SyncTransforms();
    bolt.Recall(new UnityEngine.Vector2(1004f, 10f), 0.2f, out var wall, out normal);
    Check(wall.x < 1001.9f && normal.x < -0.9f, "thin wall clips recall on knife travel segment");
    box.size = new UnityEngine.Vector2(8f, 0.4f);
    block.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, 45f);
    UnityEngine.Physics2D.SyncTransforms();
    bolt.Recall(new UnityEngine.Vector2(1004f, 10f), 0.2f, out wall, out normal);
    Check(wall.x < 1002f && normal.x < -0.6f && normal.y > 0.6f, "45 degree impact uses surface normal");
    var style = knife.AddComponent<UnityEngine.SpriteRenderer>();
    var preview = new SSW.CastView(null, 1 << 8, 0.12f, owner, System.Array.Empty<SSW.NetPlayer>());
    preview.Bolt(7, new UnityEngine.Vector2(1010f, 14f), UnityEngine.Vector2.right, style, null, spec, 3f);
    preview.Tick(0.1f);
    Check(preview.Point(7, out var start), "pending blade can be selected before spawn");
    preview.Hold(7, true);
    preview.Tick(0.2f);
    preview.Point(7, out var held);
    Check(held == start, "recall preview holds during network wait");
    preview.Hold(7, false);
    preview.Tick(0.1f);
    preview.Point(7, out var resumed);
    Check(resumed.x > held.x + 2f, "rejected recall can resume preview");
    preview.Hold(7, true);
    var synced = new UnityEngine.GameObject("Recall Sync").AddComponent<SSW.ShotSync>();
    Check(preview.Match(7, 0, synced), "held preview matches arriving network shot");
    Check((bool)typeof(SSW.ShotSync).GetField("_held", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(synced), "hold transfers to spawned shot");
    synced.Hold(false);
    Check(!(bool)typeof(SSW.ShotSync).GetField("_held", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(synced), "spawned shot releases hold on rejection");
    preview.Bolt(8, new UnityEngine.Vector2(1010f, 14f), UnityEngine.Vector2.right, style, null, spec, 0.1f);
    preview.Hold(8, true);
    preview.Tick(0.2f);
    Check(preview.Count == 0, "held preview still expires");
    foreach (SSW.CastKind kind in System.Enum.GetValues(typeof(SSW.CastKind)))
    {
        var input = new SSW.CastInput { Action = 9, Epoch = 2, Tick = 234, Stock = 17, Kind = kind, Direction = UnityEngine.Vector2.right, ViewTime = 12.5d,
            Recall = kind == SSW.CastKind.Cycle ? 7u : 0u, Point = kind == SSW.CastKind.Cycle ? new UnityEngine.Vector2(1002f, 10f) : default };
        using var writer = new Unity.Netcode.FastBufferWriter(128, Unity.Collections.Allocator.Temp);
        writer.WriteNetworkSerializable(input);
        using var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
        reader.ReadNetworkSerializable(out SSW.CastInput restored);
        Check(restored.Action == input.Action && restored.Epoch == input.Epoch && restored.Tick == input.Tick && restored.Stock == input.Stock && restored.Kind == input.Kind
            && restored.Direction == input.Direction && restored.ViewTime == input.ViewTime && restored.Recall == input.Recall && restored.Point == input.Point, $"{kind} input round trip retains all fields");
    }
    return new { passed = checks.Count, output };
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
