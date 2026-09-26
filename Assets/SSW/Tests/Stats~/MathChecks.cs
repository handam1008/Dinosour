var checks = new System.Collections.Generic.List<string>();
void Check(bool condition, string label)
{
    if (!condition) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
try
{
    var first = new UnityEngine.GameObject("Box Probe");
    var second = new UnityEngine.GameObject("Capsule Probe");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(first, scene);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(second, scene);
    var box = first.AddComponent<UnityEngine.BoxCollider2D>();
    var capsule = second.AddComponent<UnityEngine.CapsuleCollider2D>();
    box.size = new UnityEngine.Vector2(1.4f, 0.8f);
    capsule.size = new UnityEngine.Vector2(0.6f, 1.2f);
    foreach (float angle in new[] { 0f, 30f, 45f, 90f, 165f })
    {
        box.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, angle);
        for (int x = -6; x <= 6; x++)
            for (int y = -6; y <= 6; y++)
            {
                UnityEngine.Vector2 center = new UnityEngine.Vector2(x * 0.23f, y * 0.19f);
                capsule.transform.position = center;
                UnityEngine.Physics2D.SyncTransforms();
                var distance = box.Distance(capsule);
                bool expected = distance.isOverlapped;
                bool actual = SSW.ShotQuery.BoxCapsule(UnityEngine.Vector2.zero, box.transform.right, box.size,
                    center - UnityEngine.Vector2.up * 0.3f, center + UnityEngine.Vector2.up * 0.3f, 0.3f, out var point);
                if (UnityEngine.Mathf.Abs(distance.distance) > UnityEngine.Mathf.Max(0.002f, UnityEngine.Physics2D.defaultContactOffset * 1.1f))
                    Check(actual == expected, "box capsule " + angle + " " + x + " " + y + " actual=" + actual + " expected=" + expected + " distance=" + distance.distance);
                if (actual) Check(float.IsFinite(point.x) && float.IsFinite(point.y), "finite contact");
            }
    }
    float step = UnityEngine.Time.fixedDeltaTime;
    var pose = new SSW.ShotPose
    {
        Time = 10d, Position = new UnityEngine.Vector2(2f, 5f), Velocity = UnityEngine.Vector2.right * 20f,
        Gravity = UnityEngine.Vector2.down * 9.81f, GravityDelay = 0.15f, ExtraGravity = 30f
    };
    var pointNow = pose.Position;
    var velocity = pose.Velocity;
    for (int tick = 1; tick <= 30; tick++)
    {
        float age = tick * step;
        float falling = UnityEngine.Mathf.Clamp(age - pose.GravityDelay, 0f, step);
        velocity += pose.Gravity * step + UnityEngine.Vector2.down * (pose.ExtraGravity * falling);
        pointNow += velocity * step;
        Check(UnityEngine.Vector2.Distance(pointNow, pose.Point(10d + age)) < 0.006f, "delayed gravity position " + tick);
        Check(UnityEngine.Vector2.Distance(velocity, pose.VelocityAt(10d + age)) < 0.001f, "delayed gravity velocity " + tick);
    }
    using var writer = new Unity.Netcode.FastBufferWriter(1024, Unity.Collections.Allocator.Temp);
    writer.WriteValueSafe(pose);
    using var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
    reader.ReadValueSafe(out SSW.ShotPose restored);
    Check(pose.Equals(restored), "shot pose delayed gravity roundtrip");
    return new { count = checks.Count, checks };
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
