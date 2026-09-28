if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play required");
const string skinKey = "RYU.DinoSkin.v1";
bool hadSkin = UnityEngine.PlayerPrefs.HasKey(skinKey);
string savedSkin = UnityEngine.PlayerPrefs.GetString(skinKey, "");
var checks = new System.Collections.Generic.List<string>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var menu = UnityEngine.Object.FindAnyObjectByType<SSW.MainMenuController>();
var panel = UnityEngine.Object.FindAnyObjectByType<SSW.JobSettingsPanel>(UnityEngine.FindObjectsInactive.Include);
var preview = (SSW.MenuDinosaurPreview)typeof(SSW.JobSettingsPanel).GetField("_preview", flags).GetValue(panel);
var body = (UnityEngine.UI.Image)typeof(SSW.MenuDinosaurPreview).GetField("_dinosaurImage", flags).GetValue(preview);
var jobs = (SSW.JobCatalog)typeof(SSW.JobSettingsPanel).GetField("_catalog", flags).GetValue(panel);
void Check(bool value, string label)
{
    if (!value) throw new System.Exception(label);
    checks.Add(label);
}
async System.Threading.Tasks.Task Run()
{
    string error = null;
    try
    {
        menu.ShowCustom();
        await System.Threading.Tasks.Task.Delay(400);
        var customize = UnityEngine.Object.FindAnyObjectByType<RYU._01.Script.Customize.DinoCustomizeUI>();
        var tags = customize.GetComponentsInChildren<RYU._01.Script.Customize.DinoSkinTag>(true);
        foreach (var skin in RYU._01.Script.Customize.DinoSkinCatalog.Load().Skins)
        {
            menu.ShowCustom();
            await System.Threading.Tasks.Task.Delay(300);
            string tab = skin.gender == RYU._01.Script.Customize.DinoGender.Male ? "boyTab" : "girlTab";
            ((UnityEngine.UI.Button)customize.GetType().GetField(tab, flags).GetValue(customize)).onClick.Invoke();
            System.Linq.Enumerable.Single(tags, value => value.id == skin.id).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            menu.ShowJobs();
            await System.Threading.Tasks.Task.Delay(350);
            var expected = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(skin.library.GetCategoryLabelNames("Idle"), label => skin.library.GetSprite("Idle", label)));
            Check(RYU._01.Script.Customize.DinoSkinStorage.LoadId() == skin.id, skin.id + " native customization saved");
            Check(System.Linq.Enumerable.SequenceEqual((UnityEngine.Sprite[])typeof(SSW.MenuDinosaurPreview).GetField("_idleFrames", flags).GetValue(preview), expected), skin.id + " job preview uses saved idle frames");
            Check(System.Array.IndexOf(expected, body.sprite) >= 0 && body.enabled && body.color.a > .99f, skin.id + " visible body uses selected skin");
            Check(preview.HatImage.enabled && preview.HatImage.transform.parent == body.transform, skin.id + " hat remains attached");
            int frame = preview.FrameIndex;
            await System.Threading.Tasks.Task.Delay(150);
            Check(preview.FrameIndex != frame, skin.id + " animation advances");
        }
        var saved = RYU._01.Script.Customize.DinoSkinStorage.Load();
        var frames = (UnityEngine.Sprite[])typeof(SSW.MenuDinosaurPreview).GetField("_idleFrames", flags).GetValue(preview);
        foreach (var slot in panel.GetComponentsInChildren<SSW.JobSlotControl>())
        {
            slot.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
            await System.Threading.Tasks.Task.Delay(140);
            Check(panel.SelectedJob == slot.Definition.Job && jobs.TryGet(panel.SelectedJob, out SSW.JobDefinition definition) && preview.HatImage.sprite == definition.HatSprite, panel.SelectedJob + " selected job hat matches");
            Check(System.Array.IndexOf(frames, body.sprite) >= 0 && RYU._01.Script.Customize.DinoSkinStorage.LoadId() == saved.id, panel.SelectedJob + " job switch preserves skin");
        }
        UnityEngine.ScreenCapture.CaptureScreenshot("@ROOT@/JobPreview.png");
        await System.Threading.Tasks.Task.Delay(500);
    }
    catch (System.Exception exception) { error = exception.ToString(); }
    finally
    {
        if (hadSkin) UnityEngine.PlayerPrefs.SetString(skinKey, savedSkin);
        else UnityEngine.PlayerPrefs.DeleteKey(skinKey);
        UnityEngine.PlayerPrefs.Save();
        menu.ShowMain();
        System.IO.File.WriteAllText("@ROOT@/Preview.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { success = error == null, error, checks, restored = hadSkin == UnityEngine.PlayerPrefs.HasKey(skinKey) && savedSkin == UnityEngine.PlayerPrefs.GetString(skinKey, "") }, Newtonsoft.Json.Formatting.Indented));
    }
}
_ = Run();
return true;
