var obj = new UnityEngine.GameObject("Turn check");
var shot = obj.AddComponent<SSW.ShotSync>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var type = typeof(SSW.ShotSync);
var accept = type.GetMethod("Accept", flags);
var checks = new System.Collections.Generic.List<string>();
try
{
    foreach (float step in new[] { 1f / 30f, 1f / 60f, 1f / 144f })
    {
        foreach (bool bounce in new[] { true, false })
        {
            var shown = new UnityEngine.Vector2(0.1f, 0.1f);
            var old = new SSW.ShotPose
            {
                Time = 10d, Position = new UnityEngine.Vector2(0f, 0.1f),
                Velocity = new UnityEngine.Vector2(5f, bounce ? -5f : 0f), Terrain = true
            };
            var next = new SSW.ShotPose
            {
                Time = 10.02d, Position = new UnityEngine.Vector2(0.2f, 0.08f),
                Velocity = bounce ? new UnityEngine.Vector2(5f, 5f) : new UnityEngine.Vector2(-5f, 0f),
                Terrain = true, Turn = 1
            };
            obj.transform.position = shown;
            type.GetField("_pose", flags).SetValue(shot, old);
            type.GetField("_offset", flags).SetValue(shot, shown - old.Point(10.02d));
            accept.Invoke(shot, new object[] { next, 10.02d + step, 10.02d });
            var point = next.Point(10.02d + step) + (UnityEngine.Vector2)type.GetField("_offset", flags).GetValue(shot);
            var movement = point - shown;
            if (UnityEngine.Vector2.Dot(movement, next.Velocity) <= 0f)
                throw new System.Exception("A confirmed turn continues in the previous direction.");
            if (movement.magnitude > next.Velocity.magnitude * step + 0.001f)
                throw new System.Exception("A confirmed turn jumps farther than one frame of flight.");
            checks.Add((bounce ? "Bounce" : "Return") + " follows outgoing velocity at " + UnityEngine.Mathf.RoundToInt(1f / step) + " FPS.");
        }
    }
    return checks;
}
finally
{
    UnityEngine.Object.DestroyImmediate(obj);
}