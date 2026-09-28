if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
var checks = new System.Collections.Generic.List<string>();
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
int[] numbers = { 2, 11, 15, 16 };
var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.MapSources>("Assets/SSW/Editor/Maps/MapSources.asset");
var rotation = (SSW.MapRotation)new UnityEditor.SerializedObject(profile.Game).FindProperty("_maps").objectReferenceValue;
var maps = new System.Collections.Generic.List<object>();
var originals = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Logs/MapPatch/Before/State.json"));
foreach (int number in numbers)
{
    var entry = System.Linq.Enumerable.Single(profile.Entries, value => value.Number == number);
    string source = "Assets/KDH/GameModules/Maps/KDH_Map " + number + ".prefab";
    string path = "Assets/SSW/Maps/Battle/Map" + number.ToString("00") + ".prefab";
    var map = entry.Map;
    Check(UnityEditor.AssetDatabase.GetAssetPath(entry.Source) == source, number + " latest source");
    Check(UnityEditor.AssetDatabase.GetAssetPath(map) == path, number + " output binding");
    Check(System.Linq.Enumerable.Count(rotation.Prefabs, item => item == map) == 1, number + " registered once");
    var old = System.Linq.Enumerable.FirstOrDefault(originals, item => (string)item["path"] == path);
    long hash = new UnityEditor.SerializedObject(map.GetComponent<Unity.Netcode.NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue;
    Check(hash != 0, number + " network hash");
    if (old != null)
    {
        Check(UnityEditor.AssetDatabase.AssetPathToGUID(path) == (string)old["guid"], number + " preserved GUID");
        Check(hash == (long)old["hash"], number + " preserved network root");
    }
    var content = System.Linq.Enumerable.Single(map.GetComponentsInChildren<UnityEngine.Transform>(true), item =>
        item.parent == map.transform && UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(item.gameObject)) == source);
    Check(content != null, number + " connected nested source");
    Check(map.Spawn(0) != map.Spawn(1), number + " distinct spawns");
    Check(map.Bounds.Contains(map.Spawn(0)) && map.Bounds.Contains(map.Spawn(1)), number + " spawns inside map");
    Check(map.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true).Length == 0, number + " no duplicate input");
    Check(map.GetComponentsInChildren<UnityEngine.Camera>(true).Length == 0, number + " no duplicate camera");
    Check(System.Linq.Enumerable.All(map.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true), item => item != null), number + " no missing script");
    var edges = new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");
    Check(edges.arraySize == 4, number + " four boundaries");
    for (int i = 0; i < edges.arraySize; i++)
    {
        var edge = edges.GetArrayElementAtIndex(i);
        var shape = edge.FindPropertyRelative("Shape").objectReferenceValue as UnityEngine.Collider2D;
        Check(shape != null && shape.transform.IsChildOf(map.transform), number + " boundary shape " + i);
        Check(edge.FindPropertyRelative("Damage").floatValue == 5f && UnityEngine.Mathf.Abs(edge.FindPropertyRelative("Force").vector2Value.magnitude - 20f) < 0.001f, number + " source boundary tuning " + i);
    }
    var settings = new UnityEditor.SerializedObject(map.GetComponent<SSW.MapMotion>());
    foreach (string field in new[] { "_bodies", "_moving", "_pins", "_joints" })
    {
        var array = settings.FindProperty(field);
        for (int i = 0; i < array.arraySize; i++)
        {
            var item = array.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.Component;
            Check(item != null && item.transform.IsChildOf(map.transform), number + " motion " + field + " " + i);
        }
    }
    var swings = map.GetComponentsInChildren<SSW.Swing>(true);
    if (number == 11 || number == 16)
    {
        int count = number == 11 ? 3 : 11;
        Check(swings.Length == count && map.GetComponentsInChildren<SSW.Pin>(true).Length == count && map.GetComponentsInChildren<SSW.Rope>(true).Length == count, number + " platform pin rope counts");
        foreach (var swing in swings)
        {
            Check(swing.GetComponent<UnityEngine.Rigidbody2D>().mass == 25f, number + " platform mass");
            var joint = swing.GetComponent<UnityEngine.DistanceJoint2D>();
            if (number == 16) Check(joint.distance == 6f, number + " source rope length");
        }
        Check(settings.FindProperty("_pins").arraySize == count, number + " synchronized pins");
    }
    if (number == 11)
    {
        var field = new UnityEditor.SerializedObject(map.GetComponentInChildren<SSW.SakuraField>(true));
        Check(field.FindProperty("_duration").floatValue == 0.5f && field.FindProperty("_speed").floatValue == 1.2f, "11 sakura tuning");
    }
    if (number == 16) Check(map.GetComponentsInChildren<SSW.SakuraField>(true).Length == 0, "16 removed old sakura");
    if (number == 2 || number == 15)
    {
        var lift = map.GetComponentInChildren<SSW.WaterLift>(true);
        Check(lift != null && map.GetComponentsInChildren<SSW.LiftZone>(true).Length == 0, number + " replaced old lift");
        var field = new UnityEditor.SerializedObject(lift);
        Check(field.FindProperty("_force").floatValue == (number == 2 ? 5.5f : 5f), number + " source lift force");
        Check(field.FindProperty("_delay").floatValue == (number == 2 ? 10f : 0f) && field.FindProperty("_duration").floatValue == (number == 2 ? 5f : 0f), number + " source lift phase");
        Check(field.FindProperty("_effects").arraySize == (number == 2 ? 2 : 0), number + " phase effect bindings");
    }
    maps.Add(new { number, path, source, hash, swings = swings.Length, index = System.Array.IndexOf(System.Linq.Enumerable.ToArray(rotation.Prefabs), map) });
}
Check(SSW.MapBake.BakeAll() == "맵 원본 변경 없음", "second targeted bake unchanged");
var result = new { checks = checks.Count, passed = checks, maps };
System.IO.File.WriteAllText("Logs/MapPatch/Check.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
