if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
const string path = "Assets/SSW/Resources/Network/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var player = root.GetComponent<SSW.NetPlayer>();
    var guard = root.GetComponent<SSW.BuffGuard>();
    if (guard == null) throw new System.InvalidOperationException("Missing existing BuffGuard component");
    var data = new UnityEditor.SerializedObject(player);
    var field = data.FindProperty("_guard");
    bool changed = field.objectReferenceValue != guard;
    if (changed)
    {
        field.objectReferenceValue = guard;
        data.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    }
    return new { changed, valid = player.Guard == guard };
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
