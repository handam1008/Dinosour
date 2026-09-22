var checks = new System.Collections.Generic.List<string>();
foreach (int fps in new[] { 30, 60, 144 })
{
    var packets = new System.Collections.Generic.List<(double sent, double arrival)>();
    var random = new System.Random(17);
    for (int tick = 0; tick < 1000; tick++)
    {
        if (tick % 31 == 0) continue;
        double sent = tick * 0.02d;
        packets.Add((sent, sent + 0.03d + random.NextDouble() * 0.15d));
    }
    packets.Sort((a, b) => a.arrival.CompareTo(b.arrival));
    var buffer = new System.Collections.Generic.List<double>();
    SSW.SnapshotClock clock = default;
    int index = 0;
    double received = -1d;
    double previous = -1d;
    double pause = 0d;
    double longest = 0d;
    float delta = 1f / fps;
    for (int frame = 0; frame < fps * 19; frame++)
    {
        double now = frame / (double)fps;
        while (index < packets.Count && packets[index].arrival <= now)
        {
            var packet = packets[index++];
            if (packet.sent <= received) continue;
            received = packet.sent;
            clock.Observe(packet.sent, now - 0.08d, 0.02f);
            buffer.Add(packet.sent);
        }
        if (buffer.Count == 0) continue;
        double shown = clock.Step(now - 0.08d, buffer[0], buffer[buffer.Count - 1], delta);
        while (buffer.Count > 2 && buffer[1] <= shown) buffer.RemoveAt(0);
        if (previous >= 0d)
        {
            double step = shown - previous;
            if (step < -0.000001d || step > delta * 1.101d)
                throw new System.Exception("Snapshot arrival caused a playback jump at " + fps + " FPS.");
            pause = step < 0.000001d ? pause + delta : 0d;
            if (now > 2d) longest = System.Math.Max(longest, pause);
        }
        if (shown > received + 0.000001d)
            throw new System.Exception("Playback advanced beyond received state.");
        previous = shown;
    }
    if (longest > 0.12d || clock.Delay > 0.2f || received - previous > 0.3d)
        throw new System.Exception("Snapshot playback stalls or accumulates excessive delay at " + fps + " FPS.");
    checks.Add(fps + " FPS: bounded playback through jitter, reordered arrivals and missing packets.");
}
return checks;
