var game = SSW.NetGame.Current;
string output = "D:/unity_project/Mushrooms/Logs/Sandbox/maps.txt";
System.IO.File.WriteAllText(output, "");
void Check(bool ok, string text)
{
    System.IO.File.AppendAllText(output, (ok ? "PASS " : "FAIL ") + text + "\n");
    if (!ok) throw new System.InvalidOperationException(text);
}
System.Collections.IEnumerator Loaded(SSW.Practice old)
{
    float end = UnityEngine.Time.realtimeSinceStartup + 10f;
    while (game.Practice == old || game.Practice == null || game.Local == null)
    {
        if (UnityEngine.Time.realtimeSinceStartup > end) { Check(false, "map timeout"); yield break; }
        yield return null;
    }
    yield return new UnityEngine.WaitForSecondsRealtime(0.4f);
}
System.Collections.IEnumerator Run()
{
    SSW.PlayerJobStorage.Save(SSW.PlayerJob.Swordsman);
    foreach (string scene in new[] { "Map_Basic", "Map_JumpPad", "Map_Teleport", "Map_Vanish" })
    {
        var old = game.Practice;
        SSW.MapTravel.LoadScene(scene);
        yield return Loaded(old);
        var player = game.Local;
        Check(player.Job == SSW.PlayerJob.Swordsman && player.CanAct && game.Players.Count == 2, scene + " sword job preserved");
        Check(UnityEngine.Vector2.Distance(player.Body.position, game.Practice.Spawn(0)) < 2f, scene + " correct spawn");
        if (scene == "Map_JumpPad")
        {
            var pad = UnityEngine.Object.FindFirstObjectByType<SSW.JumpPad>();
            var bounds = pad.GetComponent<UnityEngine.Collider2D>().bounds;
            player.Drive.Teleport(new UnityEngine.Vector2(bounds.center.x, bounds.max.y + 0.6f));
            yield return new UnityEngine.WaitForSecondsRealtime(0.3f);
            Check(player.Velocity.y > 1f || player.Body.position.y > bounds.max.y + 1.5f, "jump pad launches shared player");
        }
        if (scene == "Map_Teleport")
        {
            var portal = UnityEngine.Object.FindFirstObjectByType<SSW.Teleporter>();
            var destination = (UnityEngine.Transform)typeof(SSW.Teleporter).GetField("_destination", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(portal);
            player.Drive.Teleport(portal.GetComponent<UnityEngine.Collider2D>().bounds.center);
            yield return new UnityEngine.WaitForSecondsRealtime(0.2f);
            Check(UnityEngine.Vector2.Distance(player.Body.position, destination.position) < 1f, "portal updates motion state");
        }
        if (scene == "Map_Vanish")
        {
            var tile = UnityEngine.Object.FindFirstObjectByType<SSW.VanishingPlatform>();
            var shape = tile.GetComponent<UnityEngine.Collider2D>();
            bool visible = shape.enabled;
            float end = UnityEngine.Time.realtimeSinceStartup + 6f;
            while (shape.enabled == visible && UnityEngine.Time.realtimeSinceStartup < end) yield return null;
            Check(shape.enabled != visible, "vanish platform still cycles");
        }
    }
    var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.MapCatalog>("Assets/SSW/Maps/MapCatalog.asset");
    foreach (var map in catalog.Maps)
    {
        var old = game.Practice;
        SSW.MapTravel.Open(map);
        yield return Loaded(old);
        Check(game.Local.Job == SSW.PlayerJob.Swordsman && game.Local.CanAct, map.Title + " selected job preserved");
        Check(UnityEngine.Vector2.Distance(game.Practice.Spawn(0), map.Spawn) < 0.01f, map.Title + " layout spawn used");
        game.Practice.ResetMap();
        yield return null;
        Check(game.Players.Count == 2 && game.Local.Job == SSW.PlayerJob.Swordsman, map.Title + " reset");
    }
    SSW.MapTravel.LoadScene("MainMenu");
    yield return new UnityEngine.WaitForSecondsRealtime(0.6f);
    Check(!game.Connected && game.Practice == null, "map session released");
    System.IO.File.AppendAllText(output, "DONE\n");
}
game.StartCoroutine(Run());
return output;
