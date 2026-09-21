using System;
using System.IO;
using SSW;
using UnityEngine;

public static class ClockChecks
{
    public static void Main()
    {
        foreach (int fps in new[] { 30, 60, 144 })
        {
            ViewClock clock = default;
            double target = 20;
            clock.Reset(target);
            float delta = 1f / fps;
            for (int frame = 0; frame < fps * 6; frame++)
            {
                target += delta;
                if (frame == fps) target -= 0.4;
                if (frame == fps * 3) target += 0.8;
                double before = clock.Time;
                double after = clock.Step(target, delta);
                double step = after - before;
                if (step < delta * 0.899 || step > delta * 1.101)
                    throw new InvalidOperationException("Network clock correction changed presentation continuity at " + fps);
            }
        }
        ShotPose pose = new ShotPose { Position = Vector2.zero, Velocity = Vector2.right * 12f };
        if (Mathf.Abs(pose.Point(0.7).x - 8.4f) > 0.001f)
            throw new InvalidOperationException("A high-latency shot stops before its prediction horizon");
        if (pose.Point(2) != pose.Point(3)) throw new InvalidOperationException("Missing snapshots extrapolate without a bound");
        File.WriteAllText("Logs/Move/clock-checks.txt", "PASS monotonic 30/60/144 FPS through backward/forward clock corrections; 700 ms flight prediction; bounded stale snapshots");
        Debug.Log("PASS clock continuity and prediction horizon");
    }
}
