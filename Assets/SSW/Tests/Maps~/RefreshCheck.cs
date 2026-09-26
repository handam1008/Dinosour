if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
string bake = SSW.MapBake.BakeAll();
if (bake != "맵 원본 변경 없음") throw new System.InvalidOperationException("Unexpected rebake: " + bake);
var game = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetGame>("Assets/SSW/Resources/Network/NetGame.prefab");
var rotation = (SSW.MapRotation)new UnityEditor.SerializedObject(game).FindProperty("_maps").objectReferenceValue;
var seen = new System.Collections.Generic.HashSet<string>();
var result = new System.Collections.Generic.List<object>();
int checks = 0;
foreach (var map in rotation.Prefabs)
{
    string path = UnityEditor.AssetDatabase.GetAssetPath(map);
    if (!seen.Add(path)) throw new System.InvalidOperationException("Duplicate map " + path);
    var scripts = map.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true);
    if (System.Linq.Enumerable.Any(scripts, value => value == null)) throw new System.InvalidOperationException("Missing script " + path);
    var config = new UnityEditor.SerializedObject(map);
    var server = config.FindProperty("_serverOnly");
    for (int i = 0; i < server.arraySize; i++)
    {
        var value = server.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.Component;
        if (value == null || !value.transform.IsChildOf(map.transform)) throw new System.InvalidOperationException("Unbound authority " + path);
        checks++;
    }
    var spawns = config.FindProperty("_spawns").objectReferenceValue;
    var points = new UnityEditor.SerializedObject(spawns);
    foreach (string field in new[] { "_first", "_second" })
    {
        var value = points.FindProperty(field).objectReferenceValue as UnityEngine.Transform;
        if (value == null || !value.IsChildOf(map.transform)) throw new System.InvalidOperationException("Unbound spawn " + path);
        checks++;
    }
    var edges = new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");
    if (edges.arraySize != 4) throw new System.InvalidOperationException("Four edges required " + path);
    for (int i = 0; i < edges.arraySize; i++)
    {
        var item = edges.GetArrayElementAtIndex(i);
        var shape = item.FindPropertyRelative("Shape").objectReferenceValue as UnityEngine.Collider2D;
        if (shape == null || !shape.transform.IsChildOf(map.transform) || item.FindPropertyRelative("Damage").floatValue <= 0f)
            throw new System.InvalidOperationException("Invalid edge " + path);
        checks++;
    }
    var motion = new UnityEditor.SerializedObject(map.GetComponent<SSW.MapMotion>());
    foreach (string field in new[] { "_bodies", "_moving", "_pins", "_joints" })
    {
        var array = motion.FindProperty(field);
        for (int i = 0; i < array.arraySize; i++)
        {
            var part = array.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.Component;
            if (part == null || !part.transform.IsChildOf(map.transform)) throw new System.InvalidOperationException("Unbound motion " + path + " " + field);
            checks++;
        }
    }
    result.Add(new { path, title = map.Title, serverOnly = server.arraySize, scripts = scripts.Length });
}
if (seen.Count != 17) throw new System.InvalidOperationException("Expected 17 maps");
return new { checks, bake, maps = result.ToArray() };
