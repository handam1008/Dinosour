var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var checks = new System.Collections.Generic.List<string>();
const string output = "Logs/Blade26/Placement.txt";
System.IO.File.WriteAllText(output, "");
void Check(bool ok, string label)
{
    System.IO.File.AppendAllText(output, (ok ? "PASS " : "FAIL ") + label + "\n");
    if (!ok) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
try
{
    var player = new UnityEngine.GameObject("Placement Player");
    var body = player.AddComponent<UnityEngine.Rigidbody2D>();
    body.bodyType = UnityEngine.RigidbodyType2D.Kinematic;
    var shape = player.AddComponent<UnityEngine.CapsuleCollider2D>();
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab");
    var source = prefab.GetComponent<UnityEngine.CapsuleCollider2D>();
    shape.size = source.size;
    shape.offset = source.offset;
    shape.direction = source.direction;
    player.transform.localScale = prefab.transform.localScale;
    var motion = player.AddComponent<SSW.PlayerController>();
    using (var data = new UnityEditor.SerializedObject(motion))
    {
        data.FindProperty("_whatIsGround").intValue = 1 << 8;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    var block = new UnityEngine.GameObject("Placement Block");
    block.layer = 8;
    var box = block.AddComponent<UnityEngine.BoxCollider2D>();
    var effector = block.AddComponent<UnityEngine.PlatformEffector2D>();
    effector.enabled = false;
    var cast = new SSW.MotionCast(shape, 1 << 8);
    var motor = new SSW.MotionMotor(shape, motion, -29.43f);
    block.transform.position = new UnityEngine.Vector3(1002f, 1f);
    box.size = new UnityEngine.Vector2(0.4f, 6f);
    UnityEngine.Physics2D.SyncTransforms();
    var target = new UnityEngine.Vector2(1004f, 1f);
    Check(cast.TryPlace(target, UnityEngine.Vector2.zero, 0.12f, out var placed) && placed == target, "clear destination across wall stays at knife position");
    var old = new UnityEngine.Vector2(1000f, 1f);
    var travel = target - old;
    cast.Move(ref old, ref travel, 1f, false);
    Check(UnityEngine.Vector2.Distance(old, target) > 2f, "previous path-based blink reproduces short destination");
    foreach (bool platform in new[] { false, true })
    foreach (float angle in new[] { 0f, 45f, -45f, 90f, 180f })
    {
        box.usedByEffector = effector.enabled = effector.useOneWay = platform;
        block.transform.position = new UnityEngine.Vector3(1004f, 0f);
        block.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, angle);
        box.size = new UnityEngine.Vector2(4f, 0.4f);
        UnityEngine.Physics2D.SyncTransforms();
        UnityEngine.Vector2 normal = block.transform.up;
        UnityEngine.Vector2 surface = block.transform.TransformPoint(new UnityEngine.Vector3(0f, 0.2f));
        target = surface + normal * 0.12f;
        Check(cast.TryPlace(target, normal, 0.12f, out placed), $"{angle} {platform} blade landing found");
        body.position = placed;
        var distance = shape.Distance(box);
        Check(!distance.isOverlapped || distance.distance >= -0.001f, $"{angle} {platform} blade landing does not overlap {distance.distance}");
        Check(UnityEngine.Vector2.Dot(placed - target, normal) >= 0f && UnityEngine.Vector2.Distance(placed, target) < 1f, $"{angle} {platform} landing remains on impact side");
    }
    box.usedByEffector = effector.enabled = false;
    foreach (float scale in new[] { 1f, 0.5f })
    foreach (float angle in new[] { 45f, -45f })
    {
        block.transform.position = new UnityEngine.Vector3(1000f, 0f);
        block.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, angle);
        box.size = new UnityEngine.Vector2(2f, 0.6f);
        UnityEngine.Physics2D.SyncTransforms();
        var state = new SSW.MotionState { Position = new UnityEngine.Vector2(1000f, 4f), SmallTime = scale < 1f ? 20f : 0f };
        for (int i = 0; i < 80; i++) motor.Step(ref state, default, 7f, 0.02f, true);
        var start = state.Position;
        for (int i = 0; i < 150; i++) motor.Step(ref state, default, 7f, 0.02f, true);
        Check(state.Grounded && UnityEngine.Vector2.Distance(state.Position, start) < 0.003f, $"{angle} scale {scale} short slope stays still {UnityEngine.Vector2.Distance(state.Position, start)}");
        var overlap = state;
        overlap.Position -= (UnityEngine.Vector2)block.transform.up * 0.02f;
        overlap.Velocity = UnityEngine.Vector2.right * (UnityEngine.Mathf.Sign(angle) * 7f);
        motor.Step(ref overlap, new SSW.MotionFrame { Jump = 1, Move = UnityEngine.Vector2.right * UnityEngine.Mathf.Sign(angle) }, 7f, 0.02f, true);
        Check(overlap.Velocity.y > 10f, $"{angle} scale {scale} jump while recovering uphill contact {overlap.Velocity}");
        motor.Step(ref state, new SSW.MotionFrame { Jump = 1 }, 7f, 0.02f, true);
        Check(state.Velocity.y > 10f && !state.Grounded, $"{angle} scale {scale} jump after standing on short slope");
    }
    player.transform.localScale = prefab.transform.localScale;
    block.transform.rotation = UnityEngine.Quaternion.identity;
    block.transform.position = new UnityEngine.Vector3(1000f, 0f);
    box.size = new UnityEngine.Vector2(4f, 0.6f);
    var roof = new UnityEngine.GameObject("Narrow Gap");
    roof.layer = 8;
    roof.transform.position = new UnityEngine.Vector3(1000f, 1f);
    roof.AddComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(4f, 0.6f);
    UnityEngine.Physics2D.SyncTransforms();
    Check(!cast.TryPlace(new UnityEngine.Vector2(1000f, 0.5f), UnityEngine.Vector2.zero, 0.12f, out placed), "space smaller than player rejects teleport");
    return new { passed = checks.Count, output };
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
