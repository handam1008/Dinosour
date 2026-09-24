var game = SSW.NetGame.Current;
string output = "D:/unity_project/Mushrooms/Logs/Sandbox/checks.txt";
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
System.IO.File.WriteAllText(output, "");
void Check(bool ok, string text)
{
    System.IO.File.AppendAllText(output, (ok ? "PASS " : "FAIL ") + text + "\n");
    if (!ok) throw new System.InvalidOperationException(text);
}
System.Collections.IEnumerator Pick(SSW.NetPlayer player)
{
    yield return new UnityEngine.WaitForSecondsRealtime(1.3f);
    var cards = player.Draft.View.GetComponentsInChildren<SSW.AugmentCardUI>();
    Check(cards.Length == 3 && !cards[0].Locked, player.Job + " three selectable cards");
    cards[0].OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
    yield return new UnityEngine.WaitForSecondsRealtime(0.85f);
}
System.Collections.IEnumerator Run()
{
    foreach (SSW.PlayerJob job in new[] { SSW.PlayerJob.Swordsman, SSW.PlayerJob.Assassin, SSW.PlayerJob.Gunner, SSW.PlayerJob.Gambler, SSW.PlayerJob.Magician, SSW.PlayerJob.Witch })
    {
        if (game.Local.Job != job)
        {
            var previous = game.Practice;
            SSW.PlayerJobStorage.Save(job);
            SSW.MapTravel.LoadScene("SuperUltraLegendScene");
            float end = UnityEngine.Time.realtimeSinceStartup + 10f;
            while (game.Practice == previous || game.Practice == null || game.Local == null || game.Local.Job != job)
            {
                if (UnityEngine.Time.realtimeSinceStartup > end) { Check(false, job + " scene reload timed out"); yield break; }
                yield return null;
            }
        }
        yield return new UnityEngine.WaitForSecondsRealtime(0.4f);
        var practice = game.Practice;
        var player = practice.Player;
        var target = practice.Target;
        Check(player.Job == job && player.IsOwner && !target.IsOwner && game.Players.Count == 2, job + " correct player and dummy");
        Check(UnityEngine.Object.FindObjectsByType<SSW.PlayerIdentity>(UnityEngine.FindObjectsSortMode.None).Length == 2, job + " no duplicate offline player");
        player.Drive.Teleport(new UnityEngine.Vector2(-6.6f, -3.16f));
        target.Drive.Teleport(new UnityEngine.Vector2(-5.1f, -3.16f));
        yield return new UnityEngine.WaitForSecondsRealtime(0.15f);
        float startX = player.Body.position.x;
        player.Move(UnityEngine.Vector2.left);
        yield return new UnityEngine.WaitForSecondsRealtime(0.15f);
        player.Move(UnityEngine.Vector2.zero);
        Check(player.Body.position.x < startX - 0.4f, job + " movement");
        float startY = player.Body.position.y;
        player.Jump();
        yield return new UnityEngine.WaitForSecondsRealtime(0.15f);
        Check(player.Body.position.y > startY + 0.5f, job + " jump");
        player.Drive.Teleport(new UnityEngine.Vector2(-6.6f, -3.16f));
        target.Drive.Teleport(new UnityEngine.Vector2(-5.1f, -3.16f));
        yield return new UnityEngine.WaitForSecondsRealtime(job == SSW.PlayerJob.Gunner ? 2.7f : 0.2f);
        int shots = player.Cast.Shots;
        float health = target.Health.Current;
        player.Cast.Attack(true, UnityEngine.Vector2.right);
        yield return new UnityEngine.WaitForSecondsRealtime(0.2f);
        player.Cast.Attack(false, UnityEngine.Vector2.right);
        yield return new UnityEngine.WaitForSecondsRealtime(0.25f);
        if (job == SSW.PlayerJob.Witch) Check(player.Cast.Shots > shots, job + " potion launched");
        else Check(target.Health.Current < health, job + " attack hit dummy " + target.Health.Current);
        if (job is SSW.PlayerJob.Swordsman or SSW.PlayerJob.Assassin or SSW.PlayerJob.Gunner or SSW.PlayerJob.Gambler)
        {
            var weapon = System.Linq.Enumerable.Single(player.GetComponentsInChildren<UnityEngine.SpriteRenderer>(), s => s.name == job.ToString());
            Check(weapon.enabled && weapon.gameObject.activeInHierarchy && weapon.sprite != null, job + " weapon visible");
        }
        if (job == SSW.PlayerJob.Swordsman)
        {
            float x = player.Body.position.x;
            player.Cast.Cycle(true, UnityEngine.Vector2.left);
            yield return new UnityEngine.WaitForSecondsRealtime(0.25f);
            Check(player.Body.position.x < x - 3f, "Swordsman dash");
            player.Cast.Parry();
            yield return new UnityEngine.WaitForSecondsRealtime(0.05f);
            float hp = player.Health.Current;
            player.Health.TakeDamage(10f);
            Check(player.Health.Current == hp, "Swordsman parry blocks damage");
        }
        if (job == SSW.PlayerJob.Assassin)
        {
            int count = player.Cast.Shots;
            player.Cast.Cycle(true, UnityEngine.Vector2.right);
            yield return new UnityEngine.WaitForSecondsRealtime(0.15f);
            Check(player.Cast.Shots > count, "Assassin dagger");
            player.Cast.Cycle(true, UnityEngine.Vector2.right);
            yield return new UnityEngine.WaitForSecondsRealtime(0.2f);
            Check(player.Body.position.y > -3.3f && player.Cast.Weapon.Status.Ammo == 0, "Assassin safe teleport");
        }
        practice.OpenDraft(true);
        Check(practice.Drafting && UnityEngine.Time.timeScale == 0f && player.Draft.Common, job + " common draft pauses practice");
        yield return Pick(player);
        Check(practice.Drafting && !player.Draft.Common && player.Draft.OwnedCount == 1, job + " common then job draft");
        yield return Pick(player);
        Check(!practice.Drafting && UnityEngine.Time.timeScale == 1f && player.Draft.OwnedCount == 2 && player.Input.inputIsActive, job + " selection applied and input restored");
        var owned = System.Linq.Enumerable.ToArray(player.Draft.Owned);
        var before = player;
        practice.ResetMap();
        yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
        Check(practice.Player != before && System.Linq.Enumerable.SequenceEqual(owned, practice.Player.Draft.Owned), job + " reset keeps augments");
        target = practice.Target;
        target.Health.ReceiveDamage(new SSW.DamageRequest(null, 10000f, SSW.DamageTag.IgnoreDefense));
        yield return new UnityEngine.WaitForSecondsRealtime(0.9f);
        Check(practice.Target != target && practice.Target.Health.Current == practice.Target.Health.Max, job + " dummy respawn");
    }
    var last = game.Practice;
    var pause = UnityEngine.Object.FindFirstObjectByType<SSW.PauseMenu>();
    pause.Open();
    Check(pause.IsOpen && UnityEngine.Time.timeScale == 0f, "pause opens");
    pause.Resume();
    Check(UnityEngine.Time.timeScale == 1f && game.Local.Input.inputIsActive, "pause resumes");
    pause.Exit();
    float finish = UnityEngine.Time.realtimeSinceStartup + 10f;
    while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
    {
        if (UnityEngine.Time.realtimeSinceStartup > finish) { Check(false, "return timeout"); yield break; }
        yield return null;
    }
    Check(!game.Connected && game.Practice == null, "main menu releases solo session");
    System.IO.File.AppendAllText(output, "DONE\n");
}
game.StartCoroutine(Run());
return output;
