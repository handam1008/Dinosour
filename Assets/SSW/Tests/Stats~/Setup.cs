if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
const string bookPath = "Assets/SSW/Resources/Network/Stats.asset";
const string sourcePath = "Assets/SSW/Editor/StatSources.asset";
const string stockPath = "Assets/SSW/Resources/Network/Potions.asset";
T Asset<T>(string path) where T : UnityEngine.Object
    => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new System.InvalidOperationException("Missing asset: " + path);
var sources = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.StatSources>(sourcePath);
bool created = sources == null;
if (created)
{
    var book = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.StatsBook>(bookPath);
    if (book == null)
    {
        book = UnityEngine.ScriptableObject.CreateInstance<SSW.StatsBook>();
        UnityEditor.AssetDatabase.CreateAsset(book, bookPath);
    }
    sources = UnityEngine.ScriptableObject.CreateInstance<SSW.StatSources>();
    var jobs = new[] { SSW.PlayerJob.Witch, SSW.PlayerJob.Magician, SSW.PlayerJob.Swordsman,
        SSW.PlayerJob.Assassin, SSW.PlayerJob.Gambler, SSW.PlayerJob.Gunner };
    var paths = new[] {
        "Assets/RYU/02.Prefabs/Agent/Witch.prefab",
        "Assets/SSW/Prefabs/PlayerMagician.prefab",
        "Assets/KHG/02.Prefab/Player.prefab",
        "Assets/NKY/Player (1).prefab",
        "Assets/JJW/Scene/Gamblinger.unity",
        "Assets/KDH/GameModules/KDH_Player Variant.prefab"
    };
    using var setup = new UnityEditor.SerializedObject(sources);
    setup.FindProperty("_book").objectReferenceValue = book;
    setup.FindProperty("_stock").objectReferenceValue = Asset<SSW.NetStock>(stockPath);
    var entries = setup.FindProperty("_entries");
    entries.arraySize = jobs.Length;
    for (int i = 0; i < jobs.Length; i++)
    {
        var entry = entries.GetArrayElementAtIndex(i);
        entry.FindPropertyRelative("Job").intValue = (int)jobs[i];
        bool scene = jobs[i] == SSW.PlayerJob.Gambler;
        entry.FindPropertyRelative("Prefab").objectReferenceValue = scene ? null : Asset<UnityEngine.GameObject>(paths[i]);
        entry.FindPropertyRelative("Scene").objectReferenceValue = scene ? Asset<UnityEditor.SceneAsset>(paths[i]) : null;
        entry.FindPropertyRelative("Root").stringValue = scene ? "Player (1)" : string.Empty;
    }
    setup.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.AssetDatabase.CreateAsset(sources, sourcePath);
    UnityEditor.EditorUtility.SetDirty(sources);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(sources);
}
string report = SSW.StatBake.Bake(sources);
const string playerPath = "Assets/SSW/Resources/Network/Player.prefab";
var player = UnityEditor.PrefabUtility.LoadPrefabContents(playerPath);
string[] order;
try
{
    using var target = new UnityEditor.SerializedObject(player.GetComponent<SSW.NetPlayer>());
    var bookRef = target.FindProperty("_statsBook");
    if (bookRef.objectReferenceValue != sources.Book)
    {
        bookRef.objectReferenceValue = sources.Book;
        target.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(player, playerPath);
    }
    order = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
        player.GetComponents<Unity.Netcode.NetworkBehaviour>(), x => x.GetType().Name));
    if (System.Array.IndexOf(order, "NetPlayer") > System.Array.IndexOf(order, "MotionView"))
        throw new System.InvalidOperationException("NetPlayer must initialize before MotionView");
}
finally
{
    UnityEditor.PrefabUtility.UnloadPrefabContents(player);
}
return new { created, book = UnityEditor.AssetDatabase.GetAssetPath(sources.Book),
    sources = sourcePath, count = sources.Book.Count, order, report };
