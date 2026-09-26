var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var checks = new System.Collections.Generic.List<string>();
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
bool Near(float left, float right) => UnityEngine.Mathf.Abs(left - right) < 0.001f;
try
{
    var obj = new UnityEngine.GameObject("Common motion player");
    var body = obj.AddComponent<UnityEngine.Rigidbody2D>();
    body.bodyType = UnityEngine.RigidbodyType2D.Kinematic;
    var shape = obj.AddComponent<UnityEngine.CapsuleCollider2D>();
    shape.size = new UnityEngine.Vector2(1.0625f, 1.1875f);
    var control = obj.AddComponent<SSW.PlayerController>();
    using (var data = new UnityEditor.SerializedObject(control))
    {
        data.FindProperty("_whatIsGround").intValue = 1 << 8;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    var floor = new UnityEngine.GameObject("Common motion floor");
    floor.layer = 8;
    floor.transform.position = new UnityEngine.Vector3(1000f, 0f, 0f);
    var box = floor.AddComponent<UnityEngine.BoxCollider2D>();
    box.size = new UnityEngine.Vector2(12f, 0.6f);
    var platform = floor.AddComponent<UnityEngine.PlatformEffector2D>();
    platform.enabled = false;
    var motor = new SSW.MotionMotor(shape, control, -29.43f);
    void Step(ref SSW.MotionState state, SSW.MotionFrame input = default, int count = 1, bool playing = true)
    {
        for (int i = 0; i < count; i++) motor.Step(ref state, input, 7f, 0.02f, playing);
    }
    SSW.MotionState Settle()
    {
        UnityEngine.Physics2D.SyncTransforms();
        var state = new SSW.MotionState { Position = new UnityEngine.Vector2(1000f, 6f), BodyScale = 1f, AirJumps = 1, AirJumpRatio = 0.9f };
        Step(ref state, default, 150);
        Check(state.Grounded, "fixture settles on ground");
        return state;
    }
    var jumping = Settle();
    Step(ref jumping, new SSW.MotionFrame { Jump = 1 });
    Check(!jumping.Grounded && jumping.AirUsed == 0 && jumping.Velocity.y > 12f, "ground jump preserves air jump charge");
    Step(ref jumping, new SSW.MotionFrame { Jump = 2 });
    float airSpeed = jumping.Velocity.y;
    Check(jumping.AirUsed == 1 && Near(airSpeed, control.JumpSpeed * 0.9f - 29.43f * 0.02f), "air jump uses 90 percent jump speed once");
    Step(ref jumping, new SSW.MotionFrame { Jump = 2 });
    Check(jumping.AirUsed == 1 && jumping.Velocity.y < airSpeed, "repeated input cannot spend a second air jump");
    float beforeThird = jumping.Velocity.y;
    Step(ref jumping, new SSW.MotionFrame { Jump = 3 });
    Check(jumping.AirUsed == 1 && jumping.Velocity.y < beforeThird, "fresh third jump denied until landing");
    Step(ref jumping, new SSW.MotionFrame { Jump = 3 }, 150);
    Check(jumping.Grounded && jumping.AirUsed == 0, "landing recharges air jump");
    Step(ref jumping, new SSW.MotionFrame { Jump = 4 });
    Step(ref jumping, new SSW.MotionFrame { Jump = 5 });
    Check(jumping.AirUsed == 1 && jumping.Velocity.y > 11f, "next landing cycle permits air jump again");
    var noPerk = new SSW.MotionState { Position = new UnityEngine.Vector2(1015f, 6f), BodyScale = 1f, AirJumpRatio = 0.9f };
    Step(ref noPerk, new SSW.MotionFrame { Jump = 1 });
    Check(noPerk.Velocity.y < 0f && noPerk.AirUsed == 0, "no common perk means no airborne jump");
    box.size = new UnityEngine.Vector2(2f, 0.6f);
    var edge = Settle();
    var walking = new SSW.MotionFrame { Move = UnityEngine.Vector2.right, Aim = UnityEngine.Vector2.up };
    for (int i = 0; i < 40 && edge.Grounded; i++) Step(ref edge, walking);
    Check(!edge.Grounded && edge.CoyoteTime > 0f && edge.AirUsed == 0, "walking off ledge preserves grace and air charge");
    var grace = edge;
    Step(ref grace, new SSW.MotionFrame { Jump = 1, Move = UnityEngine.Vector2.right });
    Check(grace.Velocity.y > 12f && grace.AirUsed == 0, "coyote jump uses normal ground jump");
    Step(ref grace, new SSW.MotionFrame { Jump = 2, Move = UnityEngine.Vector2.right });
    Check(grace.Velocity.y > 11f && grace.AirUsed == 1, "air jump still available after coyote jump");
    var expired = edge;
    Step(ref expired, walking, 10);
    Step(ref expired, new SSW.MotionFrame { Jump = 1, Move = UnityEngine.Vector2.right });
    Check(expired.AirUsed == 1 && Near(expired.Velocity.y, airSpeed), "after grace expires only the air jump remains");
    box.usedByEffector = platform.enabled = platform.useOneWay = true;
    var drop = Settle();
    Step(ref drop, new SSW.MotionFrame { Jump = 1, Move = UnityEngine.Vector2.down });
    Step(ref drop, new SSW.MotionFrame { Jump = 2 });
    Check(drop.DropTime > 0f && drop.AirUsed == 0 && drop.Velocity.y < 0f, "drop through input cannot immediately air jump");
    box.usedByEffector = platform.enabled = false;
    var frozen = new SSW.MotionState
    {
        Position = new UnityEngine.Vector2(1015f, 6f), BodyScale = 1f, AirJumps = 1, AirJumpRatio = 0.9f,
        FreezeTime = 0.06f, Velocity = new UnityEngine.Vector2(8f, 6f), External = 5f, DashTime = 0.1f, BurstTime = 0.1f
    };
    UnityEngine.Vector2 anchor = frozen.Position;
    var frozenInput = new SSW.MotionFrame { Jump = 1, Move = UnityEngine.Vector2.right, Aim = UnityEngine.Vector2.up };
    int locked = 0;
    while (frozen.FreezeTime > 0f && locked++ < 8) Step(ref frozen, frozenInput);
    Check(locked <= 4 && frozen.FreezeTime == 0f && frozen.Position == anchor, "freeze holds position and expires in simulation time");
    Check(frozen.Velocity == UnityEngine.Vector2.zero && frozen.External == 0f && frozen.DashTime == 0f && frozen.BurstTime == 0f, "freeze clears force dash and burst movement");
    Step(ref frozen, frozenInput);
    Check(frozen.Position.x > anchor.x && frozen.Velocity.y < 0f && frozen.AirUsed == 0, "movement resumes without replaying jump pressed while frozen");
    Step(ref frozen, new SSW.MotionFrame { Jump = 2, Move = UnityEngine.Vector2.right });
    Check(frozen.AirUsed == 1 && frozen.Velocity.y > 11f, "fresh jump after thaw works");
    var wall = new UnityEngine.GameObject("Common burst wall");
    wall.layer = 8;
    wall.transform.position = new UnityEngine.Vector3(1003f, 6f);
    var wallBox = wall.AddComponent<UnityEngine.BoxCollider2D>();
    wallBox.size = new UnityEngine.Vector2(0.2f, 15f);
    UnityEngine.Physics2D.SyncTransforms();
    var burst = new SSW.MotionState
    {
        Position = new UnityEngine.Vector2(1000f, 6f), BodyScale = 1f, BurstVelocity = UnityEngine.Vector2.right * 100f, BurstTime = 0.2f
    };
    Step(ref burst, default, 10);
    Check(burst.Position.x > 1002f && burst.Position.x < 1002.38f && Near(burst.Position.y, 6f), "high speed burst stops against a thin wall");
    UnityEngine.Object.DestroyImmediate(wall);
    UnityEngine.Physics2D.SyncTransforms();
    burst = new SSW.MotionState
    {
        Position = new UnityEngine.Vector2(1000f, 6f), BodyScale = 1f, BurstVelocity = UnityEngine.Vector2.right * (4f / 0.15f), BurstTime = 0.15f
    };
    Step(ref burst, default, 8);
    Check(Near(burst.Position.x, 1004f) && Near(burst.Position.y, 6f) && burst.BurstTime == 0f, "burst moves configured distance without overshooting last partial tick");
    var small = new SSW.MotionState
    {
        Position = new UnityEngine.Vector2(1015f, 6f), BodyScale = 0.75f, Scale = 1f, SmallTime = 0.04f, FreezeTime = 1f
    };
    float foot = small.Position.y - shape.size.y * 0.5f;
    Step(ref small);
    Check(Near(small.Scale, 0.375f) && Near(obj.transform.localScale.x, 0.375f), "permanent shrink multiplies temporary shrink");
    Check(Near(small.Position.y - shape.size.y * small.Scale * 0.5f, foot), "combined shrink preserves foot position");
    Step(ref small, default, 5);
    Check(Near(small.Scale, 0.75f) && small.SmallTime == 0f && Near(small.BodyScale, 0.75f), "temporary shrink expiry preserves permanent body scale");
    Check(Near(small.Position.y - shape.size.y * small.Scale * 0.5f, foot), "shrink expiry preserves foot position");
    var saved = new SSW.MotionState
    {
        Epoch = 7, Aim = new UnityEngine.Vector2(0.6f, 0.8f), Position = new UnityEngine.Vector2(1015f, 6f), Velocity = new UnityEngine.Vector2(2f, 3f),
        Surface = new UnityEngine.Vector2(0f, 1f), External = 1f, DropTime = 0.1f, DropTop = 0.4f, Jump = 8, Pulse = 9,
        CoyoteTime = 0.06f, Grounded = false, Pad = true, DashTime = 0.04f, DashSpeed = 20f, DashAction = 11,
        Scale = 0.55f, SmallTime = 0.04f, BodyScale = 1.1f, AirJumps = 1, AirUsed = 1, AirJumpRatio = 0.9f,
        FreezeTime = 0.04f, BurstVelocity = new UnityEngine.Vector2(16f, 4f), BurstTime = 0.1f
    };
    using (var writer = new Unity.Netcode.FastBufferWriter(256, Unity.Collections.Allocator.Temp))
    {
        writer.WriteNetworkSerializable(saved);
        using var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
        reader.ReadNetworkSerializable(out SSW.MotionState restored);
        Check(restored.Aim == saved.Aim, "snapshot retains remote aim");
        Check(restored.BodyScale == saved.BodyScale && restored.Scale == saved.Scale && restored.SmallTime == saved.SmallTime, "snapshot retains permanent and temporary scales");
        Check(restored.AirJumps == saved.AirJumps && restored.AirUsed == saved.AirUsed && restored.AirJumpRatio == saved.AirJumpRatio, "snapshot retains air jump charges and ratio");
        Check(restored.FreezeTime == saved.FreezeTime && restored.BurstVelocity == saved.BurstVelocity && restored.BurstTime == saved.BurstTime, "snapshot retains freeze and directional burst");
        Check(restored.Jump == saved.Jump && restored.Pulse == saved.Pulse && restored.Epoch == saved.Epoch, "snapshot retains input sequences");
        for (uint tick = 1; tick <= 20; tick++)
        {
            var input = new SSW.MotionFrame { Tick = tick, Jump = tick >= 5 ? 9u : 8u, Move = UnityEngine.Vector2.right, Aim = UnityEngine.Vector2.up };
            Step(ref saved, input);
            Step(ref restored, input);
            Check(saved.Aim == UnityEngine.Vector2.up && saved.Aim == restored.Aim && saved.Position == restored.Position && saved.Velocity == restored.Velocity && saved.Scale == restored.Scale
                && saved.BodyScale == restored.BodyScale && saved.AirUsed == restored.AirUsed && saved.CoyoteTime == restored.CoyoteTime
                && saved.FreezeTime == restored.FreezeTime && saved.BurstTime == restored.BurstTime && saved.Jump == restored.Jump,
                "authoritative snapshot replay remains identical at tick " + tick);
        }
    }
    return new { passed = checks.Count, checks };
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
