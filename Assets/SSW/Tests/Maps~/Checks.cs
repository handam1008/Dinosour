var checks = new System.Collections.Generic.List<string>();
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
var game = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetGame>("Assets/SSW/Resources/Network/NetGame.prefab");
var rotation = (SSW.MapRotation)new UnityEditor.SerializedObject(game).FindProperty("_maps").objectReferenceValue;
Check(rotation.Prefabs.Count == 14, "all 14 maps registered");
var hashes = new System.Collections.Generic.HashSet<long>();
foreach (var map in rotation.Prefabs)
{
    var net = new UnityEditor.SerializedObject(map.GetComponent<Unity.Netcode.NetworkObject>());
    long hash = net.FindProperty("GlobalObjectIdHash").longValue;
    Check(hash != 0 && hashes.Add(hash), map.Title + " unique network prefab");
    Check(map.Spawn(0) != map.Spawn(1), map.Title + " two spawn markers");
    Check(map.Bounds.Contains(map.Spawn(0)) && map.Bounds.Contains(map.Spawn(1)), map.Title + " spawns inside view");
    Check(map.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true).Length == 0, map.Title + " no duplicate input");
    foreach (var joint in map.GetComponentsInChildren<UnityEngine.DistanceJoint2D>(true))
    {
        float distance = UnityEngine.Vector2.Distance(joint.transform.TransformPoint(joint.anchor), joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
        Check(UnityEngine.Mathf.Abs(joint.distance - distance) < 0.01f, map.Title + " rope matches initial platform position");
    }
    foreach (var transform in map.GetComponentsInChildren<UnityEngine.Transform>(true))
        Check(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, map.Title + " script " + transform.name);
}
System.IO.Directory.CreateDirectory("Logs/MapRotation25");
System.IO.File.WriteAllText("Logs/MapRotation25/Checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(checks, Newtonsoft.Json.Formatting.Indented));
UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return new { checks = checks.Count, maps = rotation.Prefabs.Count, hashes = hashes.Count };
