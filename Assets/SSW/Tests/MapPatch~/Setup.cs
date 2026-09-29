if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
int[] numbers = { 2, 11, 15, 16 };
string folder = "Logs/MapPatch/Before";
if (System.IO.Directory.Exists(folder)) throw new System.InvalidOperationException("Existing backup: " + folder);
System.IO.Directory.CreateDirectory(folder);
var before = new System.Collections.Generic.List<object>();
var assets = new System.Collections.Generic.List<string>
{
    "Assets/SSW/Editor/Maps/MapSources.asset",
    "Assets/SSW/Resources/Network/NetGame.prefab"
};
foreach (int number in numbers)
    assets.Add("Assets/SSW/Maps/Battle/Map" + number.ToString("00") + ".prefab");
foreach (string path in assets)
{
    if (!System.IO.File.Exists(path)) continue;
    System.IO.File.Copy(path, folder + "/" + System.IO.Path.GetFileName(path));
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    var net = asset != null ? asset.GetComponent<Unity.Netcode.NetworkObject>() : null;
    before.Add(new
    {
        path,
        guid = UnityEditor.AssetDatabase.AssetPathToGUID(path),
        hash = net != null ? new UnityEditor.SerializedObject(net).FindProperty("GlobalObjectIdHash").longValue : 0L
    });
}
System.IO.File.WriteAllText(folder + "/State.json", Newtonsoft.Json.JsonConvert.SerializeObject(before, Newtonsoft.Json.Formatting.Indented));
UnityEditor.SessionState.SetString("MapBake.Scope", "2,11,15,16");
string result = SSW.MapBake.Setup(numbers);
System.IO.File.WriteAllText("Logs/MapPatch/Setup.txt", result);
return result;
