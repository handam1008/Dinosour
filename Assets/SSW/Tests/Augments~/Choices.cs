var game = SSW.NetGame.Current;
var deck = UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");
string root = UnityEditor.SessionState.GetString("augments.choices", "D:/unity_project/Mushrooms/Logs/Augments/Choices");
System.IO.Directory.CreateDirectory(root);
System.IO.File.WriteAllText(root + "/Checks.txt", "");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Check(bool ok, string text)
{
    System.IO.File.AppendAllText(root + "/Checks.txt", (ok ? "PASS " : "FAIL ") + text + "\n");
    if (!ok) throw new System.InvalidOperationException(text);
}
bool Applied(SSW.NetPlayer player, SSW.Augment item)
{
    if (item is SSW.SwordAugment sword) return player.GetComponent<SSW.SwordCast>().Has(sword.type);
    if (item is SSW.SwordArgument legacy) return player.GetComponent<SSW.SwordCast>().Has(legacy.type == SwordAugmentType.ParingHeal ? SSW.SwordPerk.ParryHeal : SSW.SwordPerk.DashSpeed);
    if (item is SSW.MagicianAugment magic) return player.GetComponent<SSW.MagicianAugmentController>().Has(magic.type);
    if (item is RYU._01.Script.Argument.WitchAugment witch) return player.GetComponent<RYU._01.Script.Argument.WitchAugmentController>().Has(witch.type);
    if (item is JJW.Script.Augments.GamblerAugment coin) return player.GetComponent<SSW.CoinCast>().Has(coin.type);
    if (item is SSW.GunnerArgument gun) return player.GetComponent<SSW.GunCast>().Has(gun.type);
    if (item is NKY.Scripts.Job.AbstractAssassinAugmentSO knife) return player.GetComponent<SSW.KnifeCast>().Has(knife.type);
    return false;
}
System.Collections.IEnumerator Pick(SSW.NetPlayer player, bool common)
{
    float until = UnityEngine.Time.realtimeSinceStartup + 5f;
    while (!player.Draft.HasView || player.Draft.View.GetComponentsInChildren<SSW.AugmentCardUI>().Any(c => c.Locked))
    {
        if (UnityEngine.Time.realtimeSinceStartup > until) throw new System.InvalidOperationException("Card animation timed out");
        yield return null;
    }
    var view = player.Draft.View;
    var cards = view.GetComponentsInChildren<SSW.AugmentCardUI>(true);
    var offer = player.Draft.Offer;
    var candidates = deck.Candidates(player.Job, player.Draft.Owned, common);
    Check(!view.Spectating && cards.Length == 3, player.Job + " owner can select");
    for (int i = 0; i < cards.Length; i++)
    {
        var card = cards[i];
        if (offer[i] < 0)
        {
            int before = player.Draft.OwnedCount;
            card.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Check(!card.gameObject.activeSelf && card.Locked && player.Draft.HasView && player.Draft.OwnedCount == before, player.Job + " empty slot is hidden and cannot select");
            continue;
        }
        Check(candidates.Contains(offer[i]) && card.Augment == deck.At(offer[i]) && !card.ReadOnly, player.Job + " valid offered card " + offer[i]);
        var title = (UnityEngine.UI.Text)typeof(SSW.AugmentCardUI).GetField("_nameText", flags).GetValue(card);
        var description = (UnityEngine.UI.Text)typeof(SSW.AugmentCardUI).GetField("_descriptionText", flags).GetValue(card);
        Check(title.text == card.Augment.displayName && description.text == card.Augment.description && !string.IsNullOrWhiteSpace(title.text), player.Job + " title and description " + offer[i]);
    }
    if (!common && player.Job == SSW.PlayerJob.Witch)
        UnityEngine.ScreenCapture.CaptureScreenshot(root + "/Witch-" + candidates.Count + ".png");
    int chosen = offer.x;
    cards[0].OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
    until = UnityEngine.Time.realtimeSinceStartup + 4f;
    while (!player.Draft.Owned.Contains(chosen))
    {
        if (UnityEngine.Time.realtimeSinceStartup > until) throw new System.InvalidOperationException("Selection failed " + chosen);
        yield return null;
    }
    Check(player.GetComponent<SSW.AugmentDrafter>().Owned.Contains(deck.At(chosen)), player.Job + " actual UI selection grants " + chosen);
    if (!common) Check(Applied(player, deck.At(chosen)), player.Job + " combat receiver applied " + chosen);
}
System.Collections.IEnumerator Run()
{
    foreach (SSW.PlayerJob job in new[] { SSW.PlayerJob.Witch, SSW.PlayerJob.Magician, SSW.PlayerJob.Gambler, SSW.PlayerJob.Gunner, SSW.PlayerJob.Assassin, SSW.PlayerJob.Swordsman })
    {
        if (game.Local.Job != job)
        {
            var previous = game.Practice;
            SSW.PlayerJobStorage.Save(job);
            SSW.MapTravel.LoadScene("SuperUltraLegendScene");
            float until = UnityEngine.Time.realtimeSinceStartup + 12f;
            while (game.Practice == previous || game.Practice == null || game.Local == null || game.Local.Job != job)
            {
                if (UnityEngine.Time.realtimeSinceStartup > until) throw new System.InvalidOperationException(job + " scene load");
                yield return null;
            }
        }
        yield return new UnityEngine.WaitForSecondsRealtime(0.3f);
        var practice = game.Practice;
        var player = practice.Player;
        int total = deck.Candidates(job, System.Array.Empty<int>(), false).Count;
        Check(total == (job == SSW.PlayerJob.Swordsman ? 4 : job == SSW.PlayerJob.Assassin ? 11 : 10), job + " expected unique candidate count " + total);
        practice.OpenDraft(true);
        Check(practice.Drafting && player.Draft.Common && UnityEngine.Time.timeScale == 0f, job + " common selection starts first");
        yield return Pick(player, true);
        Check(practice.Drafting && !player.Draft.Common, job + " job selection follows common");
        yield return Pick(player, false);
        Check(!practice.Drafting && player.Input.inputIsActive && UnityEngine.Time.timeScale == 1f, job + " resumes after both selections");
        player.Draft.Restore(deck.Candidates(job, player.Draft.Owned, true));
        while (deck.Candidates(job, player.Draft.Owned, false).Count > 0)
        {
            practice.OpenDraft(true);
            yield return Pick(player, false);
        }
        practice.OpenDraft(true);
        Check(!practice.Drafting && UnityEngine.Time.timeScale == 1f && player.Input.inputIsActive, job + " exhausted deck resumes without blank cards");
        int[] owned = player.Draft.Owned.ToArray();
        practice.ResetMap();
        yield return new UnityEngine.WaitForSecondsRealtime(0.25f);
        Check(practice.Player != player && practice.Player.Draft.Owned.SequenceEqual(owned), job + " respawn retains selected cards");
        foreach (int id in owned.Where(i => !deck.IsCommon(deck.At(i))))
            Check(Applied(practice.Player, deck.At(id)), job + " respawn reapplies " + id);
    }
    System.IO.File.AppendAllText(root + "/Checks.txt", "DONE\n");
}
System.Collections.IEnumerator Guard(System.Collections.IEnumerator inner)
{
    while (true)
    {
        object current;
        try
        {
            if (!inner.MoveNext()) yield break;
            current = inner.Current;
        }
        catch (System.Exception error)
        {
            System.IO.File.WriteAllText(root + "/Error.txt", error.ToString());
            yield break;
        }
        if (current is System.Collections.IEnumerator child) yield return Guard(child);
        else yield return current;
        if (System.IO.File.Exists(root + "/Error.txt")) yield break;
    }
}
game.StartCoroutine(Guard(Run()));
return root;
