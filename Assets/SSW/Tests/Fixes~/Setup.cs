if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
var reports = new System.Collections.Generic.List<object>();
foreach (int number in new[] { 8, 12 })
{
    string path = "Assets/SSW/Maps/Battle/Map" + number.ToString("00") + ".prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        var map = root.GetComponent<SSW.BattleMap>();
        var bounds = SSW.MapFrame.Read(root.GetComponent<SSW.MapEdges>());
        using var settings = new UnityEditor.SerializedObject(map);
        settings.FindProperty("_bounds").boundsValue = bounds;
        settings.FindProperty("_fallY").floatValue = bounds.min.y - 3f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
        reports.Add(new { path, center = new { bounds.center.x, bounds.center.y }, height = bounds.size.y });
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
}
const string playerPath = "Assets/SSW/Resources/Network/Player.prefab";
var player = UnityEditor.PrefabUtility.LoadPrefabContents(playerPath);
try
{
    using var health = new UnityEditor.SerializedObject(player.GetComponent<SSW.Health>());
    health.FindProperty("_numberAnchor").objectReferenceValue = player.GetComponentInChildren<SSW.DinosaurVisualController>(true).GetComponent<UnityEngine.SpriteRenderer>();
    health.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(player, playerPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(player); }
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SSW/Resources" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (prefab.GetComponent<SSW.MatchUI>() == null) continue;
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        using var settings = new UnityEditor.SerializedObject(root.GetComponent<SSW.MatchUI>());
        var exit = (UnityEngine.UI.Button)settings.FindProperty("_exit").objectReferenceValue;
        var text = exit.GetComponentInChildren<UnityEngine.UI.Text>(true);
        settings.FindProperty("_exitText").objectReferenceValue = text;
        text.text = "항복";
        settings.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
}
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/SSW/MainMenu.unity" || scene.isDirty) throw new System.InvalidOperationException("Clean MainMenu scene required");
var legacy = UnityEngine.Object.FindFirstObjectByType<RYU._01.Script.PlayerInfoPanel>(UnityEngine.FindObjectsInactive.Include);
using (var old = new UnityEditor.SerializedObject(legacy))
{
    var info = legacy.GetComponent<SSW.ProfileInfo>();
    if (info == null) info = legacy.gameObject.AddComponent<SSW.ProfileInfo>();
    using var settings = new UnityEditor.SerializedObject(info);
    settings.FindProperty("_name").objectReferenceValue = old.FindProperty("playerNameText").objectReferenceValue;
    settings.FindProperty("_score").objectReferenceValue = old.FindProperty("scoreText").objectReferenceValue;
    settings.ApplyModifiedPropertiesWithoutUndo();
    legacy.enabled = false;
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
SSW.StatBake.BakeAll();
return new { maps = reports, spread = UnityEngine.Resources.Load<SSW.StatsBook>("Network/Stats").At(SSW.PlayerJob.Witch).Spread };
