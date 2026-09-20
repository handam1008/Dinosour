var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var method = typeof(SSW.MotionView).GetMethod("Ease", flags);
if (typeof(SSW.MotionView).GetField("_shown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null) throw new System.Exception("presentation baseline is not compiled");
UnityEngine.Vector2 Ease(UnityEngine.Vector2 offset, UnityEngine.Vector2 velocity, float speed, float delta) => (UnityEngine.Vector2)method.Invoke(null, new object[] { offset, velocity, speed, delta, velocity * delta });
var checks = new System.Collections.Generic.List<string>();
foreach (int fps in new[] { 30, 60, 144 })
{
    float dt = 1f / fps;
    var offset = new UnityEngine.Vector2(-0.7f, 0f);
    var velocity = new UnityEngine.Vector2(-3.5f, 0f);
    for (int i = 0; i < fps; i++)
    {
        var next = Ease(offset, velocity, 3.5f, dt);
        if (velocity.x * dt + next.x - offset.x > 0.00001f) throw new System.Exception("slow reversed at " + fps);
        if (next.sqrMagnitude > offset.sqrMagnitude + 0.00001f) throw new System.Exception("offset grew at " + fps);
        offset = next;
    }
    if (offset.sqrMagnitude > 0.00001f) throw new System.Exception("slow did not settle at " + fps);
    checks.Add("slow stays in direction and settles at " + fps);
    offset = new UnityEngine.Vector2(1.61f, 0f);
    for (int i = 0; i < fps / 4; i++) offset = Ease(offset, new UnityEngine.Vector2(-10.5f, 0f), 10.5f, dt);
    if (offset.sqrMagnitude > 0.00001f) throw new System.Exception("haste did not settle at " + fps);
    checks.Add("haste settles within 250 ms at " + fps);
    if (Ease(UnityEngine.Vector2.zero, velocity, 7f, dt) != UnityEngine.Vector2.zero) throw new System.Exception("zero drift at " + fps);
    checks.Add("zero remains zero at " + fps);
}
var held = (UnityEngine.Vector2)method.Invoke(null, new object[] { new UnityEngine.Vector2(-0.7f, 0f), new UnityEngine.Vector2(-7f, 0f), 7f, 1f/144f, UnityEngine.Vector2.zero });
if (held.x != -0.7f) throw new System.Exception("stationary interpolation frame reversed");
checks.Add("stationary interpolation frame does not reverse");
var before = new UnityEngine.Vector2(-0.7f, 0f);
var movement = new UnityEngine.Vector2(-3.5f/144f, 0f);
var after = (UnityEngine.Vector2)method.Invoke(null, new object[] { before, new UnityEngine.Vector2(-7f, 0f), 7f, 1f/144f, movement });
if ((movement+after-before).x > 0f) throw new System.Exception("slow expiration reversed the view");
checks.Add("slow expiration respects actual interpolation movement");
return checks;
