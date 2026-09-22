var checks = new System.Collections.Generic.List<string>();
float step = 0.02f;
foreach (int fps in new[] { 20, 30, 144 })
{
    SSW.InputBudget budget = default;
    budget.Reset(0d);
    double now = 0d;
    double physics = 0d;
    double hitchAt = 3d;
    int processed = 0;
    int hitches = 0;
    int largest = 0;
    while (now < 18d)
    {
        double elapsed = 1d / fps;
        if (now >= hitchAt && hitches < 4)
        {
            elapsed += 0.9d;
            hitchAt += 3d;
            hitches++;
        }
        now += elapsed;
        physics += System.Math.Min(elapsed, 1d / 3d);
        int received = (int)(now / step);
        largest = System.Math.Max(largest, received - processed);
        while (physics >= step)
        {
            physics -= step;
            budget.Advance(now, step);
            int steps = received > processed + 3 ? 2 : 1;
            for (int i = 0; i < steps && processed < received && budget.Ready(step); i++)
            {
                budget.Spend(step);
                processed++;
            }
        }
    }
    int remaining = (int)(now / step) - processed;
    if (remaining > 4 || hitches != 4 || largest < 40)
        throw new System.Exception("Host hitches left pending input at " + fps + " FPS: " + remaining);
    checks.Add(fps + " FPS: four 900 ms hitches recover to " + remaining + " pending inputs.");
}
SSW.InputBudget limited = default;
limited.Reset(1d);
limited.Advance(1.02d, step);
limited.Spend(step);
for (int i = 0; i < 200; i++) limited.Advance(1.02d, step);
if (limited.Ready(step)) throw new System.Exception("Repeated fixed steps created input credit without elapsed time.");
limited.Advance(100d, step);
int allowed = 0;
while (limited.Ready(step) && allowed < 1000)
{
    limited.Spend(step);
    allowed++;
}
if (allowed != SSW.MotionHistory.Capacity) throw new System.Exception("Recovery exceeded the bounded input history.");
limited.Seed(6, step);
limited.Reset(100d);
if (limited.Ready(step)) throw new System.Exception("Teleport retained old input credit.");
checks.Add("Duplicate timestamps cannot accelerate input; long stalls are capped; teleport clears credit.");
return checks;
