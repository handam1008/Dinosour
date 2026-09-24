var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var checks = new System.Collections.Generic.List<string>();
const string output = "Logs/Jump24/checks.txt";
System.IO.Directory.CreateDirectory("Logs/Jump24");
System.IO.File.WriteAllText(output, "");
void Check(bool condition, string message)
{
    System.IO.File.AppendAllText(output, (condition ? "PASS " : "FAIL ") + message + "\n");
    if (!condition) throw new System.InvalidOperationException(message);
    checks.Add(message);
}
try
{
    var player = new UnityEngine.GameObject("Jump Test Player");
    var body = player.AddComponent<UnityEngine.Rigidbody2D>();
    body.bodyType = UnityEngine.RigidbodyType2D.Kinematic;
    var shape = player.AddComponent<UnityEngine.CapsuleCollider2D>();
    shape.size = new UnityEngine.Vector2(1.0625f, 1.1875f);
    var controller = player.AddComponent<SSW.PlayerController>();
    using var data = new UnityEditor.SerializedObject(controller);
    data.FindProperty("_whatIsGround").intValue = 1 << 8;
    data.ApplyModifiedPropertiesWithoutUndo();
    var probe = new SSW.GroundProbe(shape, body, 1 << 8);
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    typeof(SSW.PlayerController).GetField("_rb", flags).SetValue(controller, body);
    typeof(SSW.PlayerController).GetField("_col", flags).SetValue(controller, shape);
    typeof(SSW.PlayerController).GetField("_ground", flags).SetValue(controller, probe);
    var tile = new UnityEngine.GameObject("Jump Test Block");
    tile.layer = 8;
    tile.transform.position = new UnityEngine.Vector3(1000f, 0f, 0f);
    var box = tile.AddComponent<UnityEngine.BoxCollider2D>();
    box.size = new UnityEngine.Vector2(8f, 0.6f);
    var effector = tile.AddComponent<UnityEngine.PlatformEffector2D>();
    var motor = new SSW.MotionMotor(shape, controller, -29.43f);
    void Step(ref SSW.MotionState state, SSW.MotionFrame input, int count = 1, bool playing = true)
    {
        for (int i = 0; i < count; i++) motor.Step(ref state, input, 7f, 0.02f, playing);
    }
    SSW.MotionState Settle()
    {
        var state = new SSW.MotionState { Position = new UnityEngine.Vector2(1000f, 6f) };
        Step(ref state, default, 130);
        return state;
    }
    foreach (bool oneWay in new[] { false, true })
    foreach (float angle in new[] { 0f, 30f, 44.9f, 45f, 45.1f, -45f })
    {
        effector.enabled = effector.useOneWay = box.usedByEffector = oneWay;
        tile.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, angle);
        UnityEngine.Physics2D.SyncTransforms();
        var state = Settle();
        Check(state.Grounded && state.Position.y > 0.7f, $"{angle} degree {(oneWay ? "one-way" : "solid")} landing");
        player.transform.position = state.Position;
        body.linearVelocity = UnityEngine.Vector2.zero;
        UnityEngine.Physics2D.SyncTransforms();
        Check(probe.Read() == box, $"{angle} degree full collider ground probe");
        controller.Jump();
        Check(body.linearVelocity.y == controller.JumpSpeed, $"{angle} degree local jump");
        body.linearVelocity = UnityEngine.Vector2.up * 10f;
        controller.Jump();
        Check(body.linearVelocity.y == 10f, $"{angle} degree local jump cannot repeat in air");
        var input = new SSW.MotionFrame { Jump = 1 };
        Step(ref state, input);
        Check(state.Velocity.y > 10f && !state.Grounded && state.CoyoteTime == 0f, $"{angle} degree network jump consumes grace");
        input.Jump = 2;
        Step(ref state, input);
        Check(state.Velocity.y < 12f, $"{angle} degree network jump cannot repeat in air");
    }
    foreach (bool oneWay in new[] { false, true })
    foreach (float angle in new[] { 45f, -45f })
    foreach (float direction in new[] { -1f, 1f })
    {
        effector.enabled = effector.useOneWay = box.usedByEffector = oneWay;
        tile.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, angle);
        UnityEngine.Physics2D.SyncTransforms();
        var state = Settle();
        float startX = state.Position.x;
        var input = new SSW.MotionFrame { Move = new UnityEngine.Vector2(direction, 0f) };
        Step(ref state, input, 8);
        Check(state.Grounded && (state.Position.x - startX) * direction > 1f, $"walk {direction} on {angle} degree {(oneWay ? "one-way" : "solid")} slope {state.Position} velocity {state.Velocity} grounded {state.Grounded}");
        input.Jump = 1;
        Step(ref state, input);
        Check(state.Velocity.y > 10f && !state.Grounded, $"jump while walking {direction} on {angle} degree slope");
    }
    box.usedByEffector = effector.enabled = false;
    foreach (bool oneWay in new[] { false, true })
    {
        effector.enabled = effector.useOneWay = box.usedByEffector = oneWay;
        tile.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, 45f);
        UnityEngine.Physics2D.SyncTransforms();
        var state = Settle();
        state.DashTime = 0.1f;
        state.DashSpeed = 18f;
        Step(ref state, default, 5);
        Check(state.Grounded && state.Position.x > 1001.5f && state.Position.y > 2.5f, $"ground dash follows {(oneWay ? "one-way" : "solid")} slope {state.Position}");
        Step(ref state, new SSW.MotionFrame { Jump = 1 });
        Check(state.Velocity.y > 10f, "slope dash permits grounded jump");
    }
    box.usedByEffector = effector.enabled = false;
    var ledge = new UnityEngine.GameObject("Slope Exit");
    ledge.layer = 8;
    var ledgeShape = ledge.AddComponent<UnityEngine.BoxCollider2D>();
    ledgeShape.size = new UnityEngine.Vector2(6f, 0.6f);
    ledge.transform.position = new UnityEngine.Vector3(1005.616f, 2.74056f, 0f);
    UnityEngine.Physics2D.SyncTransforms();
    var exit = Settle();
    bool supported = true;
    for (int i = 0; i < 40; i++)
    {
        Step(ref exit, new SSW.MotionFrame { Move = UnityEngine.Vector2.right });
        supported &= exit.Grounded;
    }
    Check(supported && exit.Position.x > 1005f, $"slope to flat transition stays grounded {exit.Position}");
    var ceiling = Settle();
    ledgeShape.size = new UnityEngine.Vector2(2f, 0.3f);
    ledge.transform.position = new UnityEngine.Vector3(1001f, 2.35f, 0f);
    UnityEngine.Physics2D.SyncTransforms();
    Step(ref ceiling, new SSW.MotionFrame { Move = UnityEngine.Vector2.right }, 20);
    Check(ceiling.Position.y + shape.size.y * 0.5f <= 2.22f && ceiling.Position.x < 1000.7f && ceiling.Grounded, $"slope ceiling blocks without lifting off ground {ceiling.Position}");
    UnityEngine.Object.DestroyImmediate(ledge);
    effector.useOneWay = true;
    Check(!SSW.GroundProbe.IsPlatform(box), "disabled or unused effectors do not make a solid platform passable");
    tile.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, 60f);
    UnityEngine.Physics2D.SyncTransforms();
    var steep = Settle();
    Step(ref steep, new SSW.MotionFrame { Jump = 1 });
    Check(!steep.Grounded && steep.Velocity.y <= 0f, "steep wall does not grant a ground jump");
    player.transform.position = steep.Position;
    body.linearVelocity = UnityEngine.Vector2.zero;
    UnityEngine.Physics2D.SyncTransforms();
    Check(probe.Read() == null, "local probe rejects steep contact");
    tile.transform.rotation = UnityEngine.Quaternion.identity;
    box.size = new UnityEngine.Vector2(2f, 0.6f);
    UnityEngine.Physics2D.SyncTransforms();
    var edge = Settle();
    var walking = new SSW.MotionFrame { Move = UnityEngine.Vector2.right };
    for (int i = 0; i < 40 && edge.Grounded; i++) Step(ref edge, walking);
    Check(!edge.Grounded && edge.CoyoteTime > 0f, "walking off edge retains jump grace");
    foreach (int ticks in new[] { 1, 3, 5, 6, 8 })
    {
        var state = edge;
        var input = walking;
        Step(ref state, input, ticks - 1);
        input.Jump = 1;
        Step(ref state, input);
        bool expected = ticks < 6;
        Check((state.Velocity.y > 10f) == expected, $"edge jump at {ticks * 20}ms {(expected ? "accepted" : "expired")}");
    }
    var air = new SSW.MotionState { Position = new UnityEngine.Vector2(1010f, 6f) };
    Step(ref air, new SSW.MotionFrame { Jump = 1 });
    Check(air.Velocity.y < 0f && air.CoyoteTime == 0f, "airborne spawn grants no jump grace");
    typeof(SSW.PlayerController).GetField("_coyoteLeft", flags).SetValue(controller, 0.1f);
    controller.Teleport(new UnityEngine.Vector2(1010f, 6f));
    body.linearVelocity = UnityEngine.Vector2.zero;
    UnityEngine.Physics2D.SyncTransforms();
    controller.Jump();
    Check(body.linearVelocity.y == 0f, "local teleport clears jump grace");
    var blocked = edge;
    Step(ref blocked, default, 1, false);
    blocked.Position = new UnityEngine.Vector2(1010f, 6f);
    Step(ref blocked, new SSW.MotionFrame { Jump = 1 });
    Check(blocked.CoyoteTime == 0f && blocked.Velocity.y < 0f, "blocked movement clears old jump grace");
    var dash = edge;
    dash.Position = new UnityEngine.Vector2(1010f, 6f);
    dash.DashTime = 0.2f;
    dash.DashSpeed = 18f;
    Step(ref dash, default, 12);
    Step(ref dash, new SSW.MotionFrame { Jump = 1 });
    Check(dash.CoyoteTime == 0f && dash.Velocity.y < 0f, "air dash cannot extend jump grace");
    box.usedByEffector = effector.enabled = effector.useOneWay = true;
    UnityEngine.Physics2D.SyncTransforms();
    var drop = Settle();
    Step(ref drop, new SSW.MotionFrame { Jump = 1, Move = UnityEngine.Vector2.down });
    Check(drop.DropTime > 0f && drop.CoyoteTime == 0f && drop.Velocity.y < 0f, "down jump consumes grace and drops through");
    Step(ref drop, new SSW.MotionFrame { Jump = 2 });
    Check(drop.CoyoteTime == 0f && drop.Velocity.y < 0f, "drop through cannot turn into an air jump");
    box.usedByEffector = effector.enabled = false;
    UnityEngine.Physics2D.SyncTransforms();
    var saved = edge;
    saved.Epoch = 3;
    saved.Jump = 9;
    using (var writer = new Unity.Netcode.FastBufferWriter(160, Unity.Collections.Allocator.Temp))
    {
        writer.WriteNetworkSerializable(saved);
        using var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
        reader.ReadNetworkSerializable(out SSW.MotionState restored);
        Check(restored.CoyoteTime == saved.CoyoteTime && restored.Jump == saved.Jump && restored.Epoch == saved.Epoch, "snapshot serializes jump grace");
        for (uint tick = 1; tick <= 8; tick++)
        {
            var input = new SSW.MotionFrame { Tick = tick, Move = UnityEngine.Vector2.right, Jump = tick >= 3 ? 10u : 9u };
            Step(ref saved, input);
            Step(ref restored, input);
            Check(saved.Position == restored.Position && saved.Velocity == restored.Velocity && saved.CoyoteTime == restored.CoyoteTime, $"snapshot replay matches authority tick {tick}");
        }
    }
    System.IO.File.AppendAllText(output, "DONE\n");
    return new { passed = checks.Count, output };
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
