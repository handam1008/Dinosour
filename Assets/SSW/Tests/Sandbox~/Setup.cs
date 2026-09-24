if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first");
var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (original.isDirty) throw new System.InvalidOperationException("Active scene has unsaved edits");
string originalPath = original.path;
var changed = new System.Collections.Generic.List<string>();
foreach (string path in new[] {
    "Assets/SSW/SuperUltraLegendScene.unity", "Assets/SSW/Scenes/Map_Basic.unity",
    "Assets/SSW/Scenes/Map_JumpPad.unity", "Assets/SSW/Scenes/Map_Teleport.unity",
    "Assets/SSW/Scenes/Map_Vanish.unity", "Assets/SSW/Scenes/Crate.unity",
    "Assets/SSW/Scenes/Swing.unity", "Assets/SSW/Scenes/Wind.unity" })
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
    var parts = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(scene.GetRootGameObjects(), r => r.GetComponentsInChildren<UnityEngine.Component>(true)));
    var local = System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<SSW.PlayerIdentity>(parts));
    var target = System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<SSW.Health>(parts), h => h.gameObject != local.gameObject);
    var camera = System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<UnityEngine.Camera>(parts));
    var selector = System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<SSW.MapSelector>(parts));
    var map = System.Linq.Enumerable.SingleOrDefault(System.Linq.Enumerable.OfType<SSW.MapHost>(parts));
    var arena = System.Linq.Enumerable.SingleOrDefault(System.Linq.Enumerable.OfType<SSW.NetArena>(parts));
    if (arena == null) arena = new UnityEngine.GameObject("NetArena").AddComponent<SSW.NetArena>();
    var points = System.Linq.Enumerable.SingleOrDefault(System.Linq.Enumerable.OfType<SSW.SpawnPoints>(parts));
    if (points == null)
    {
        var root = new UnityEngine.GameObject("SpawnPoints");
        points = root.AddComponent<SSW.SpawnPoints>();
        var first = new UnityEngine.GameObject("Spawn 1").transform;
        first.SetParent(root.transform);
        first.position = local.transform.position;
        var second = new UnityEngine.GameObject("Spawn 2").transform;
        second.SetParent(root.transform);
        second.position = target.transform.position;
        var so = new UnityEditor.SerializedObject(points);
        so.FindProperty("_first").objectReferenceValue = first;
        so.FindProperty("_second").objectReferenceValue = second;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    var practice = arena.GetComponent<SSW.Practice>();
    if (practice == null) practice = arena.gameObject.AddComponent<SSW.Practice>();
    var ps = new UnityEditor.SerializedObject(practice);
    ps.FindProperty("_arena").objectReferenceValue = arena;
    ps.FindProperty("_mapHost").objectReferenceValue = map;
    ps.FindProperty("_selector").objectReferenceValue = selector;
    ps.ApplyModifiedPropertiesWithoutUndo();
    var aset = new UnityEditor.SerializedObject(arena);
    aset.FindProperty("_view").objectReferenceValue = camera;
    aset.FindProperty("_follow").objectReferenceValue = camera.GetComponent<SSW.SandboxCameraFollow>();
    aset.FindProperty("_spawnPoints").objectReferenceValue = points;
    aset.FindProperty("_practice").objectReferenceValue = practice;
    var offline = aset.FindProperty("_offline");
    offline.arraySize = 2;
    offline.GetArrayElementAtIndex(0).objectReferenceValue = local.gameObject;
    offline.GetArrayElementAtIndex(1).objectReferenceValue = target.gameObject;
    var tools = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(System.Linq.Enumerable.OfType<UnityEngine.Behaviour>(parts), b => b is SSW.MapHost || b is SSW.MapSelector || b is SSW.SandboxCollisionSetup));
    var localTools = aset.FindProperty("_localTools");
    localTools.arraySize = tools.Length;
    for (int i = 0; i < tools.Length; i++) localTools.GetArrayElementAtIndex(i).objectReferenceValue = tools[i];
    aset.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException(path);
    changed.Add(path);
}
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalPath);
return changed;
