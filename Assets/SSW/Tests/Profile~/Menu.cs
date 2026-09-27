if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play required");
string root = "Logs/Profile27";
if (System.IO.File.Exists(root + "/Menu.json")) throw new System.InvalidOperationException("Preserve the previous run");
const string skinKey = "RYU.DinoSkin.v1";
bool hadSkin = UnityEngine.PlayerPrefs.HasKey(skinKey);
string savedSkin = UnityEngine.PlayerPrefs.GetString(skinKey, "");
var checks = new System.Collections.Generic.List<string>();
var fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
SSW.MainMenuController Menu() => UnityEngine.Object.FindFirstObjectByType<SSW.MainMenuController>();
SSW.ProfileInfo Info() => UnityEngine.Object.FindFirstObjectByType<SSW.ProfileInfo>(UnityEngine.FindObjectsInactive.Include);
SSW.MenuDinosaurPreview Preview()
{
    var card = UnityEngine.Object.FindFirstObjectByType<SSW.ProfileCard>(UnityEngine.FindObjectsInactive.Include);
    return (SSW.MenuDinosaurPreview)typeof(SSW.ProfileCard).GetField("_preview", fields).GetValue(card);
}
string ScoreText() => ((TMPro.TMP_Text)typeof(SSW.ProfileInfo).GetField("_score", fields).GetValue(Info())).text;
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
void CheckSkin(RYU._01.Script.Customize.DinoSkin skin)
{
    var preview = Preview();
    var body = (UnityEngine.UI.Image)typeof(SSW.MenuDinosaurPreview).GetField("_dinosaurImage", fields).GetValue(preview);
    var frames = (UnityEngine.Sprite[])typeof(SSW.MenuDinosaurPreview).GetField("_idleFrames", fields).GetValue(preview);
    var expected = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(skin.library.GetCategoryLabelNames("Idle"), label => skin.library.GetSprite("Idle", label)));
    Check(System.Linq.Enumerable.SequenceEqual(frames, expected), skin.id + " idle frames match selected library");
    Check(body.enabled && body.color.a == 1 && System.Array.IndexOf(expected, body.sprite) >= 0, skin.id + " visible body uses selected skin");
    Check(preview.HatImage.enabled && preview.HatImage.sprite != null && preview.HatImage.transform.parent == body.transform, skin.id + " job hat remains attached");
}
void Report(double score)
{
    var report = typeof(RYU._01.Script.Leaderboard.MatchReporter).GetField("Reported", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    var callbacks = (System.Action<RYU._01.Script.Leaderboard.MatchReportResult>)report.GetValue(null);
    Check(callbacks != null, "result event has a listener");
    callbacks(new RYU._01.Script.Leaderboard.MatchReportResult { score = score, delta = 17 });
}
async System.Threading.Tasks.Task<string> CloudScore()
{
    try
    {
        var entry = await Unity.Services.Leaderboards.LeaderboardsService.Instance.GetPlayerScoreAsync("Ranking");
        return entry.Score.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    catch (Unity.Services.Leaderboards.Exceptions.LeaderboardsException error)
    {
        if (error.Reason == Unity.Services.Leaderboards.Exceptions.LeaderboardsExceptionReason.EntryNotFound) return "no entry";
        throw;
    }
}
async System.Threading.Tasks.Task Run()
{
    string error = null;
    string originalProfile = null;
    string originalPlayer = null;
    string testPlayer = null;
    bool originallySignedIn = false;
    string before = null;
    string after = null;
    bool authRestored = false;
    try
    {
        var catalog = RYU._01.Script.Customize.DinoSkinCatalog.Load();
        Menu().ShowCustom();
        await System.Threading.Tasks.Task.Delay(600);
        var customize = UnityEngine.Object.FindFirstObjectByType<RYU._01.Script.Customize.DinoCustomizeUI>();
        var tags = customize.GetComponentsInChildren<RYU._01.Script.Customize.DinoSkinTag>(true);
        var boy = (UnityEngine.UI.Button)customize.GetType().GetField("boyTab", fields).GetValue(customize);
        var girl = (UnityEngine.UI.Button)customize.GetType().GetField("girlTab", fields).GetValue(customize);
        Check(tags.Length == catalog.Skins.Count, "all customization buttons are present");
        foreach (var skin in catalog.Skins)
        {
            (skin.gender == RYU._01.Script.Customize.DinoGender.Male ? boy : girl).onClick.Invoke();
            var tag = System.Linq.Enumerable.Single(tags, value => value.id == skin.id);
            var button = tag.GetComponent<UnityEngine.UI.Button>();
            Check(button.gameObject.activeInHierarchy && button.interactable, skin.id + " native button is selectable");
            button.onClick.Invoke();
            await System.Threading.Tasks.Task.Delay(180);
            Check(RYU._01.Script.Customize.DinoSkinStorage.LoadId() == skin.id, skin.id + " native save completed");
            CheckSkin(skin);
            int frame = Preview().FrameIndex;
            await System.Threading.Tasks.Task.Delay(140);
            Check(Preview().FrameIndex != frame, skin.id + " idle animation advances");
            if (skin.id == "male_mort" || skin.id == "female_vita")
            {
                UnityEngine.ScreenCapture.CaptureScreenshot(root + "/" + skin.id + ".png");
                await System.Threading.Tasks.Task.Delay(150);
            }
        }
        var selected = RYU._01.Script.Customize.DinoSkinStorage.Load();
        var card = UnityEngine.Object.FindFirstObjectByType<SSW.ProfileCard>();
        card.gameObject.SetActive(false);
        card.gameObject.SetActive(true);
        CheckSkin(selected);
        if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized)
            await Unity.Services.Core.UnityServices.InitializeAsync();
        var auth = Unity.Services.Authentication.AuthenticationService.Instance;
        originalProfile = auth.Profile;
        originallySignedIn = auth.IsSignedIn;
        originalPlayer = auth.PlayerId;
        if (auth.IsSignedIn) auth.SignOut();
        auth.SwitchProfile("profile27-ui");
        await auth.SignInAnonymouslyAsync();
        testPlayer = auth.PlayerId;
        before = await CloudScore();
        Menu().ShowLeaderboard();
        await System.Threading.Tasks.Task.Delay(800);
        Check((string)typeof(SSW.ProfileInfo).GetField("_player", fields).GetValue(Info()) == testPlayer, "login after menu start connects profile");
        Info().gameObject.SetActive(false);
        Report(1234);
        Check(SSW.ProfileScores.TryGet(testPlayer, out double cached) && cached == 1234, "result received while profile is absent is cached");
        var loading = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
        while (!loading.isDone) await System.Threading.Tasks.Task.Delay(100);
        await System.Threading.Tasks.Task.Delay(600);
        Check(ScoreText() == "1234점", "new menu shows the result from the previous scene");
        CheckSkin(selected);
        Info().Refresh();
        await System.Threading.Tasks.Task.Delay(600);
        Check(ScoreText() == "1234점", "leaderboard without an entry cannot erase completed result");
        Report(1200);
        Check(ScoreText() == "1200점", "visible profile immediately applies next result");
        Menu().ShowLeaderboard();
        await System.Threading.Tasks.Task.Delay(600);
        UnityEngine.ScreenCapture.CaptureScreenshot(root + "/Leaderboard.png");
        await System.Threading.Tasks.Task.Delay(200);
        after = await CloudScore();
        Check(before == after, "UGS score is unchanged by UI checks");
        auth.SignOut();
        Check(ScoreText() == "-", "sign out removes the previous account score");
    }
    catch (System.Exception exception) { error = exception.ToString(); }
    finally
    {
        if (hadSkin) UnityEngine.PlayerPrefs.SetString(skinKey, savedSkin);
        else UnityEngine.PlayerPrefs.DeleteKey(skinKey);
        UnityEngine.PlayerPrefs.Save();
        try
        {
            Menu().ShowMain();
            if (originalProfile != null)
            {
                var auth = Unity.Services.Authentication.AuthenticationService.Instance;
                if (auth.IsSignedIn) auth.SignOut();
                auth.SwitchProfile(originalProfile);
                if (originallySignedIn) await auth.SignInAnonymouslyAsync();
                authRestored = auth.Profile == originalProfile && auth.IsSignedIn == originallySignedIn && (!originallySignedIn || auth.PlayerId == originalPlayer);
                if (!authRestored) throw new System.InvalidOperationException("Original account was not restored");
            }
            if (testPlayer != null)
            {
                var cache = (System.Collections.Generic.Dictionary<string, SSW.ProfileScore>)typeof(SSW.ProfileScores).GetField("Scores", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                cache.Remove(testPlayer);
            }
            var card = UnityEngine.Object.FindFirstObjectByType<SSW.ProfileCard>(UnityEngine.FindObjectsInactive.Include);
            card.gameObject.SetActive(false);
            card.gameObject.SetActive(true);
        }
        catch (System.Exception cleanup) { error = (error ?? "") + "\nCleanup: " + cleanup; }
        var result = new { status = error == null ? "completed" : "failed", error, count = checks.Count, checks, before, after, cloudWrite = false, authRestored, skinRestored = UnityEngine.PlayerPrefs.HasKey(skinKey) == hadSkin && UnityEngine.PlayerPrefs.GetString(skinKey, "") == savedSkin, scope = "Native customization buttons, result event fixture, real MainMenu reload and read-only UGS score queries; no ranked match submitted" };
        System.IO.File.WriteAllText(root + "/Menu.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
    }
}
_ = Run();
return new { status = "started", root };
