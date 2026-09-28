
if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play before editing");
string scenePath = "Assets/SSW/MainMenu.unity";
string[] paths = { scenePath, "Assets/SSW/Resources/UI/PauseMenu.prefab", "Assets/SSW/Resources/UI/MatchMenu.prefab", "Assets/SSW/Resources/UI/Vs.prefab" };
string backup = "D:/unity_project/Mushrooms/Logs/MenuUpdate/BeforeSetup";
var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
if (stage != null && stage.scene.isDirty && paths.Contains(stage.assetPath)) throw new System.InvalidOperationException("Unsaved prefab changes");
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
bool opened = !scene.isLoaded;
if (scene.isLoaded && scene.isDirty) throw new System.InvalidOperationException("Unsaved MainMenu changes");
foreach (string path in paths)
{
    string copy = System.IO.Path.Combine(backup, path);
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(copy));
    if (System.IO.File.Exists(copy))
    {
        if (!System.IO.File.ReadAllBytes(path).SequenceEqual(System.IO.File.ReadAllBytes(copy))) throw new System.InvalidOperationException("Asset changed after backup: " + path);
    }
    else System.IO.File.Copy(path, copy);
}
if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
try
{
    var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SSW.MainMenuController>(true)).Single();
    var data = new SerializedObject(menu);
    var main = (CanvasGroup)data.FindProperty("_mainPanel").objectReferenceValue;
    var settings = (CanvasGroup)data.FindProperty("_settingsPanel").objectReferenceValue;
    var bgm = (UnityEngine.UI.Slider)data.FindProperty("_bgmVolume").objectReferenceValue;
    var sfx = (UnityEngine.UI.Slider)data.FindProperty("_sfxVolume").objectReferenceValue;
    var bgmLabel = settings.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(t => t.name == "BgmLabel");
    var sfxLabel = settings.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(t => t.name == "SfxLabel");
    var custom = main.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Button_Custom");
    var quit = main.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Button_Exit");
    if (!Enumerable.Range(0, quit.onClick.GetPersistentEventCount()).Any(i => quit.onClick.GetPersistentMethodName(i) == "OpenQuit")) throw new System.InvalidOperationException("Exit callback missing");
    if (((RectTransform)quit.transform).anchoredPosition.x <= ((RectTransform)custom.transform).anchoredPosition.x) throw new System.InvalidOperationException("Exit must follow Custom");

    void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }
    UnityEngine.UI.Slider AddSlider(UnityEngine.UI.Slider source, UnityEngine.UI.Text label, RectTransform parent, float y)
    {
        var copy = UnityEngine.Object.Instantiate(source, parent, false);
        copy.name = source.name;
        copy.onValueChanged = new UnityEngine.UI.Slider.SliderEvent();
        copy.minValue = 0f;
        copy.maxValue = 1f;
        copy.wholeNumbers = false;
        Place((RectTransform)copy.transform, 85f, y, 310f, 40f);
        var caption = UnityEngine.Object.Instantiate(label, parent, false);
        caption.name = label.name;
        caption.fontSize = 24;
        caption.alignment = TextAnchor.MiddleLeft;
        caption.raycastTarget = false;
        Place(caption.rectTransform, -190f, y, 160f, 44f);
        return copy;
    }
    foreach (string path in paths.Skip(1).Take(2))
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Component controller = path.EndsWith("MatchMenu.prefab") ? root.GetComponent<SSW.MatchUI>() : root.GetComponent<SSW.PauseMenu>();
            var serialized = new SerializedObject(controller);
            var panel = ((GameObject)serialized.FindProperty("_panel").objectReferenceValue).transform;
            var resume = (UnityEngine.UI.Button)serialized.FindProperty("_resume").objectReferenceValue;
            var exit = (UnityEngine.UI.Button)serialized.FindProperty("_exit").objectReferenceValue;
            Place((RectTransform)resume.transform, 0f, 120f, 320f, 70f);
            Place((RectTransform)exit.transform, 0f, 32f, 320f, 70f);
            resume.GetComponentInChildren<UnityEngine.UI.Text>(true).text = "계속하기";
            exit.GetComponentInChildren<UnityEngine.UI.Text>(true).text = "나가기";
            var volume = new GameObject("Volume", typeof(RectTransform)).GetComponent<RectTransform>();
            volume.SetParent(panel, false);
            Place(volume, 0f, -105f, 560f, 136f);
            var music = AddSlider(bgm, bgmLabel, volume, 32f);
            var effects = AddSlider(sfx, sfxLabel, volume, -32f);
            var controls = volume.gameObject.AddComponent<SSW.VolumePanel>();
            var fields = new SerializedObject(controls);
            fields.FindProperty("_bgm").objectReferenceValue = music;
            fields.FindProperty("_sfx").objectReferenceValue = effects;
            fields.ApplyModifiedPropertiesWithoutUndo();
            if (controller is SSW.MatchUI)
            {
                serialized.FindProperty("_volume").objectReferenceValue = controls;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
            if (!saved) throw new System.InvalidOperationException("Prefab save failed: " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    var vsRoot = PrefabUtility.LoadPrefabContents(paths[3]);
    try
    {
        var vs = new SerializedObject(vsRoot.GetComponent<SSW.Vs>());
        vs.FindProperty("_hold").floatValue = 3f;
        vs.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(vsRoot, paths[3], out bool saved);
        if (!saved) throw new System.InvalidOperationException("VS save failed");
    }
    finally { PrefabUtility.UnloadPrefabContents(vsRoot); }
    return new { hold = 3f, duration = 3.75f, buttons = main.GetComponentsInChildren<UnityEngine.UI.Button>(true).Select(b => b.name).ToArray(), paths };
}
finally { if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
