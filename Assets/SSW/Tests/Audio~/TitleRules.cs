var checks = new System.Collections.Generic.List<string>();
void Check(bool valid, string label)
{
    if (!valid) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
var prefab = UnityEngine.Resources.Load<SSW.NetGame>("Network/NetGame");
var source = prefab.GetComponent<KHG_TitleManager>();
Check(source != null && prefab.GetComponent<SSW.ProfileTitle>() != null, "Title manager saved on network prefab");
Check(source.equippedTitle == source.eggTitle && source.eggTitle.titleName == "부화의 전조", "Default egg title");
var titles = new[] { source.monkeyTitle, source.dinoTitle, source.magmaTitle, source.meteorTitle, source.iceAgeTitle, source.extinctTitle, source.eggTitle };
Check(System.Linq.Enumerable.All(titles, t => t != null && !string.IsNullOrWhiteSpace(t.titleName)), "All seven title assets bound");
var ranks = new[] { 0, 1, 5, 6, 15, 16, 30, 31, 47, 48, 999 };
var tiers = new[] { "Extinct", "IceAge", "IceAge", "Meteor", "Meteor", "Magma", "Magma", "Dino", "Dino", "Monkey", "Monkey" };
for (int i = 0; i < ranks.Length; i++) Check(SSW.RankTier.Key(ranks[i]) == tiers[i], "Rank boundary " + ranks[i]);
var score = new SSW.ProfileScore();
Check(score.CompleteRead(score.BeginRead(), 1000, 31) && score.Rank == 31, "Leaderboard rank cached with score");
int stale = score.BeginRead();
score.Report(1010);
Check(score.Rank == null, "Match report invalidates old rank");
Check(!score.CompleteRead(stale, 1000, 31) && score.Value == 1010 && score.Rank == null, "In-flight stale response cannot restore old rank");
Check(!score.CompleteRead(score.BeginRead(), 1000, 31) && score.Rank == null, "Eventually consistent old score cannot restore old rank");
Check(score.CompleteRead(score.BeginRead(), 1010, 15) && score.Rank == 15, "New score refresh updates rank");
var cardRoot = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/SSW/Resources/UI/Vs.prefab");
try
{
    foreach (var card in cardRoot.GetComponentsInChildren<SSW.VsCard>(true))
    {
        var tag = (UnityEngine.UI.Text)new UnityEditor.SerializedObject(card).FindProperty("_tag").objectReferenceValue;
        Check(string.IsNullOrEmpty(tag.text) && !tag.gameObject.activeSelf, "No fixed title on " + card.name);
        Check(!tag.supportRichText && tag.resizeTextForBestFit, "Title text rendering on " + card.name);
    }
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(cardRoot); }
return Newtonsoft.Json.JsonConvert.SerializeObject(new { success = true, count = checks.Count, checks });
