var game = SSW.NetGame.Current;
if (!UnityEditor.EditorApplication.isPlaying || !game.Manager.IsServer || !game.CanFight || game.Players.Count != 2)
    throw new System.InvalidOperationException("A playing host and client are required");
string path = "@ROOT@/Water@NUMBER@.json";
var lift = game.Arena.Map.GetComponentInChildren<SSW.WaterLift>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
float delay = (float)typeof(SSW.WaterLift).GetField("_delay", flags).GetValue(lift);
float duration = (float)typeof(SSW.WaterLift).GetField("_duration", flags).GetValue(lift);
var effects = (UnityEngine.ParticleSystem[])typeof(SSW.WaterLift).GetField("_effects", flags).GetValue(lift);
var started = (Unity.Netcode.NetworkVariable<double>)typeof(SSW.WaterLift).GetField("_startedAt", flags).GetValue(lift);
var stateField = typeof(SSW.MotionView).GetField("_state", flags);
var samples = new System.Collections.Generic.List<object>();
var players = System.Linq.Enumerable.ToArray(game.Players);
var bases = new System.Collections.Generic.Dictionary<ulong, float>();
var peaks = new System.Collections.Generic.Dictionary<ulong, float>();
foreach (var player in players)
{
    player.Drive.Teleport(game.Arena.Spawn(player.Side == 1 ? 0 : 1));
    player.Drive.Freeze(40f);
}
started.Value = game.Manager.ServerTime.Time;
double start = started.Value;
async System.Threading.Tasks.Task Run()
{
    string error = null;
    bool placed = false;
    try
    {
        while (game.Manager.ServerTime.Time - start < (duration > 0f ? delay + duration + 0.4f : 1.4f))
        {
            await UnityEngine.Awaitable.FixedUpdateAsync();
            double age = game.Manager.ServerTime.Time - start;
            if (lift.Active && !placed)
            {
                var shape = lift.GetComponent<UnityEngine.Collider2D>();
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    var state = (SSW.MotionState)stateField.GetValue(player.Drive);
                    state.FreezeTime = 0f;
                    stateField.SetValue(player.Drive, state);
                    float foot = player.Body.position.y - player.Collider.bounds.min.y;
                    var point = new UnityEngine.Vector2(shape.bounds.center.x + (i == 0 ? -0.2f : 0.2f), shape.bounds.min.y + foot + 0.4f);
                    player.Drive.Teleport(point);
                    bases[player.OwnerClientId] = point.y;
                    peaks[player.OwnerClientId] = point.y;
                }
                placed = true;
            }
            if (placed)
                foreach (var player in players) peaks[player.OwnerClientId] = UnityEngine.Mathf.Max(peaks[player.OwnerClientId], player.Body.position.y);
            samples.Add(new
            {
                age,
                active = lift.Active,
                effects = System.Linq.Enumerable.Count(effects, effect => effect.isEmitting),
                players = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(players, player => new { id = player.OwnerClientId, y = player.Body.position.y }))
            });
        }
    }
    catch (System.Exception exception) { error = exception.ToString(); }
    finally
    {
        foreach (var player in players)
        {
            player.Drive.Teleport(game.Arena.Spawn(player.Side == 1 ? 0 : 1));
            player.Drive.Freeze(40f);
        }
        var result = new { error, delay, duration, effectCount = effects.Length, bases, peaks, samples };
        System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
    }
}
_ = Run();
return new { path, delay, duration, start };
