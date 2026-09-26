if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play mode required");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.name != "MainMenu") throw new System.InvalidOperationException("MainMenu required");
string path = "@ROOT@/Menu.json";
if (System.IO.File.Exists(path)) throw new System.InvalidOperationException("Fresh run required");
var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SSW.MainMenuController>(true)).Single();
var button = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnityEngine.UI.Button>(true))
    .Single(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentTarget(i) == menu && b.onClick.GetPersistentMethodName(i) == "ShowCustom"));
var catalog = RYU._01.Script.Customize.DinoSkinCatalog.Load();
const string key = "RYU.DinoSkin.v1";
bool had = UnityEngine.PlayerPrefs.HasKey(key);
string saved = UnityEngine.PlayerPrefs.GetString(key, "");
var cases = new System.Collections.Generic.List<object>();
int count = 0;
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    count++;
}
void Write(string status, string error = null) => System.IO.File.WriteAllText(path,
    Newtonsoft.Json.JsonConvert.SerializeObject(new { status, error, count, cases, had, saved,
        restored = UnityEngine.PlayerPrefs.HasKey(key) == had && UnityEngine.PlayerPrefs.GetString(key, "") == saved,
        method = "Native Unity Button.onClick and OnEnable; no physical mouse input" }, Newtonsoft.Json.Formatting.Indented));
async System.Threading.Tasks.Task Run()
{
    string error = null;
    try
    {
        Write("running");
        button.onClick.Invoke();
        await System.Threading.Tasks.Task.Delay(1000);
        var ui = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RYU._01.Script.Customize.DinoCustomizeUI>()).Single();
        var fields = new UnityEditor.SerializedObject(ui);
        var boy = (UnityEngine.UI.Button)fields.FindProperty("boyTab").objectReferenceValue;
        var girl = (UnityEngine.UI.Button)fields.FindProperty("girlTab").objectReferenceValue;
        var tags = ui.GetComponentsInChildren<RYU._01.Script.Customize.DinoSkinTag>(true);
        Check(tags.Length == catalog.Skins.Count && tags.Select(t => t.id).Distinct().Count() == tags.Length, "one button per original skin");
        foreach (var skin in catalog.Skins)
        {
            (skin.gender == RYU._01.Script.Customize.DinoGender.Male ? boy : girl).onClick.Invoke();
            var tag = tags.Single(t => t.id == skin.id);
            var pick = tag.GetComponent<UnityEngine.UI.Button>();
            Check(pick.gameObject.activeInHierarchy && pick.interactable, "active selectable button " + skin.id);
            pick.onClick.Invoke();
            Check(UnityEngine.PlayerPrefs.GetString(key) == skin.id && RYU._01.Script.Customize.DinoSkinStorage.LoadId() == skin.id, "native selection saved " + skin.id);
            Check(skin.library != null && RYU._01.Script.Customize.DinoSkinStorage.Load().library == skin.library, "saved library " + skin.id);
            ui.enabled = false;
            ui.enabled = true;
            Check(pick.gameObject.activeInHierarchy, "reopening selects saved gender " + skin.id);
            cases.Add(new { skin.id, skin.displayName, gender = skin.gender.ToString(), library = skin.library.name });
        }
    }
    catch (System.Exception exception) { error = exception.ToString(); }
    finally
    {
        if (had) UnityEngine.PlayerPrefs.SetString(key, saved);
        else UnityEngine.PlayerPrefs.DeleteKey(key);
        UnityEngine.PlayerPrefs.Save();
        menu.ShowMain();
        Write(error == null ? "completed" : "failed", error);
    }
}
_ = Run();
return new { running = true, path };
