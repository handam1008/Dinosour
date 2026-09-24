var game = SSW.NetGame.Current;
string path = "D:/unity_project/Mushrooms/Logs/Sandbox/cases.txt";
System.IO.File.WriteAllText(path, "");
void Check(bool ok, string text)
{
    System.IO.File.AppendAllText(path, (ok ? "PASS " : "FAIL ") + text + "\n");
    if (!ok) throw new System.InvalidOperationException(text);
}
System.Collections.IEnumerator Run()
{
    yield return new UnityEngine.WaitForSecondsRealtime(0.4f);
    var player = game.Local;
    Check(player.Job == SSW.PlayerJob.Witch, "witch loaded");
    var pause = UnityEngine.Object.FindFirstObjectByType<SSW.PauseMenu>();
    player.Effects.Hide(1f);
    player.Effects.Immune(1f);
    player.Effects.Blind(1f);
    pause.Open();
    yield return null;
    double clock = game.ServerTime;
    double physics = game.PhysicsTime;
    var field = typeof(SSW.NetCast).GetField("_potions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var stock = (Unity.Netcode.NetworkVariable<SSW.NetCast.PotionState>)field.GetValue(player.Cast);
    uint revision = stock.Value.Revision;
    yield return new UnityEngine.WaitForSecondsRealtime(2f);
    Check(game.ServerTime == clock && game.PhysicsTime == physics, "pause freezes combat clock");
    Check(stock.Value.Revision == revision, "pause freezes potion brewing");
    Check(player.Effects.Hidden && player.Effects.ImmuneActive && player.Effects.BlindActive, "pause preserves timed effects");
    pause.Resume();
    yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
    Check(player.Effects.Hidden, "effect duration preserved after resume");
    yield return new UnityEngine.WaitForSecondsRealtime(1.1f);
    Check(!player.Effects.Hidden && !player.Effects.ImmuneActive && !player.Effects.BlindActive, "effects expire after resumed duration");
    var previous = game.Practice;
    SSW.PlayerJobStorage.Save(SSW.PlayerJob.Assassin);
    SSW.MapTravel.LoadScene("SuperUltraLegendScene");
    while (game.Practice == previous || game.Practice == null || game.Local == null) yield return null;
    yield return new UnityEngine.WaitForSecondsRealtime(0.2f);
    var target = game.Practice.Target;
    target.Health.ReceiveDamage(new SSW.DamageRequest(null, 10000f, SSW.DamageTag.IgnoreDefense));
    player = game.Local;
    player.Cast.Cycle(true, UnityEngine.Vector2.up);
    yield return new UnityEngine.WaitForSecondsRealtime(0.15f);
    var bolt = UnityEngine.Object.FindFirstObjectByType<SSW.NetBolt>();
    Check(bolt != null && bolt.IsSpawned, "dagger exists before dummy respawn");
    yield return new UnityEngine.WaitForSecondsRealtime(0.8f);
    Check(game.Practice.Target != target && bolt != null && bolt.IsSpawned, "dummy respawn preserves live dagger");
    game.Practice.ResetMap();
    yield return null;
    Check(UnityEngine.Object.FindObjectsByType<SSW.NetBolt>(UnityEngine.FindObjectsSortMode.None).Length == 0, "manual reset clears projectiles");
    System.IO.File.AppendAllText(path, "DONE\n");
}
game.StartCoroutine(Run());
return path;
