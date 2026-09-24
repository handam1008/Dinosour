if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first");
int scenes = 0;
foreach (string path in new[] { "Assets/SSW/SuperUltraLegendScene.unity", "Assets/SSW/Scenes/Map_Basic.unity", "Assets/SSW/Scenes/Map_JumpPad.unity", "Assets/SSW/Scenes/Map_Teleport.unity", "Assets/SSW/Scenes/Map_Vanish.unity", "Assets/SSW/Scenes/Crate.unity", "Assets/SSW/Scenes/Swing.unity", "Assets/SSW/Scenes/Wind.unity" })
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
    try
    {
        var parts = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(scene.GetRootGameObjects(), r => r.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)));
        if (System.Linq.Enumerable.Any(parts, p => p == null)) throw new System.InvalidOperationException(path + " missing script");
        var arena = System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<SSW.NetArena>(parts));
        var so = new UnityEditor.SerializedObject(arena);
        foreach (string key in new[] { "_view", "_spawnPoints", "_practice" })
            if (so.FindProperty(key).objectReferenceValue == null) throw new System.InvalidOperationException(path + " " + key);
        var practice = System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<SSW.Practice>(parts));
        so = new UnityEditor.SerializedObject(practice);
        foreach (string key in new[] { "_arena", "_selector" })
            if (so.FindProperty(key).objectReferenceValue == null) throw new System.InvalidOperationException(path + " " + key);
        scenes++;
    }
    finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
}
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab");
if (prefab.GetComponents<SSW.JobCast>().Length != 4) throw new System.InvalidOperationException("job modules");
foreach (var view in prefab.GetComponentsInChildren<SSW.WeaponView>(true))
{
    var so = new UnityEditor.SerializedObject(view);
    var renderer = (UnityEngine.SpriteRenderer)so.FindProperty("_sprite").objectReferenceValue;
    if (renderer == null || renderer.sprite == null) throw new System.InvalidOperationException(view.name + " sprite");
}
return new { scenes, jobs = 6, weapons = 4, missingScripts = 0, activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
