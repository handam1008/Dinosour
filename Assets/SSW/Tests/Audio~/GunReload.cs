var game = SSW.NetGame.Current;
var player = game.Players[0];
var gun = player.GetComponent<SSW.GunCast>();
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var stateProperty = typeof(SSW.JobCast).GetProperty("State", flags);
var phaseMethod = typeof(SSW.NetMatch).GetMethod("SetPhase", flags);
var checks = new System.Collections.Generic.List<string>();
int reloads = 0;
void Heard(DevLib.SoundSystem.Runtime.SoundClipSO cue) { if (cue.name == "KDH_ReloadSound") reloads++; }
void Check(bool value, string label) { if (!value) throw new System.InvalidOperationException(label); checks.Add(label); }
void Fill(SSW.NetPlayer p)
{
    var cast = p.GetComponent<SSW.GunCast>();
    var state = new SSW.WeaponState { Ammo = cast.Capacity, Reload = p.InputSequence + 100000, Filled = p.InputSequence };
    for (int i = 0; i < cast.Capacity; i++) state.Loaded.Add(p.InputSequence);
    stateProperty.SetValue(cast, state);
}
async System.Threading.Tasks.Task Run()
{
    string error = null;
    var audio = SSW.GameAudio.GetOrCreate();
    audio.Played += Heard;
    try
    {
        foreach (var p in game.Players) Fill(p);
        await System.Threading.Tasks.Task.Delay(500);
        Check(reloads == 0, "full magazines stay silent");
        stateProperty.SetValue(gun, new SSW.WeaponState { Reload = player.InputSequence - 1 });
        var forecast = typeof(SSW.JobCast).GetMethod("Forecast", flags);
        bool forecastsLoaded = true;
        for (int i = 0; i < 100; i++)
        {
            var predicted = (SSW.WeaponState)forecast.Invoke(gun, null);
            forecastsLoaded &= predicted.Ammo > 0;
        }
        Check(forecastsLoaded, "one hundred forecasts advance ammo");
        Check(reloads == 0 && ((SSW.WeaponState)stateProperty.GetValue(gun)).Ammo == 0, "forecast emits no sound and changes no authoritative ammo");
        phaseMethod.Invoke(game.Match, new object[] { SSW.MatchPhase.Waiting });
        stateProperty.SetValue(gun, new SSW.WeaponState { Reload = player.InputSequence + 25, Filled = player.InputSequence });
        await System.Threading.Tasks.Task.Delay(1500);
        Check(gun.Status.Ammo == 0 && reloads == 0, "paused fight does not reload or emit sound");
        phaseMethod.Invoke(game.Match, new object[] { SSW.MatchPhase.Playing });
        await System.Threading.Tasks.Task.Delay(180);
        Check(gun.Status.Ammo == 0 && reloads == 0, "resuming preserves remaining reload delay");
        float until = UnityEngine.Time.unscaledTime + 5f;
        while (gun.Status.Ammo == 0 && UnityEngine.Time.unscaledTime < until) await System.Threading.Tasks.Task.Delay(10);
        Check(gun.Status.Ammo == 1 && reloads == 1, "one authoritative reload emits exactly one sound");
        Fill(player);
        await System.Threading.Tasks.Task.Delay(300);
        Check(reloads == 1, "full state prevents repeated reload sound");
        stateProperty.SetValue(gun, new SSW.WeaponState { Reload = player.InputSequence, Filled = player.InputSequence });
        bool accepted = gun.Apply(new SSW.CastInput { Kind = SSW.CastKind.Press, Tick = player.InputSequence, Action = 900000, Direction = UnityEngine.Vector2.up }, player.Body.position, 0d);
        Check(accepted && gun.Status.Ammo == 0 && reloads == 2, "reload consumed by same server attack still emits once");
        Fill(player);
    }
    catch (System.Exception failure) { error = failure.ToString(); }
    finally
    {
        audio.Played -= Heard;
        if (game.Match.State.Phase != SSW.MatchPhase.Playing) phaseMethod.Invoke(game.Match, new object[] { SSW.MatchPhase.Playing });
        foreach (var p in game.Players) Fill(p);
        System.IO.File.WriteAllText("@REPORT@", Newtonsoft.Json.JsonConvert.SerializeObject(new { success = error == null, error, count = checks.Count, checks, reloads }, Newtonsoft.Json.Formatting.Indented));
    }
}
_ = Run();
return new { running = true };
