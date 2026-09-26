if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
const string path = "Assets/SSW/Resources/Network/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
bool changed;
long fileId;
string guid;
try
{
    var cast = root.GetComponent<SSW.GunCast>();
    using var settings = new UnityEditor.SerializedObject(cast);
    var balance = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunBalance>("Assets/SSW/Resources/Network/GunBalance.asset");
    if (balance == null || UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(balance)) != "59aba008e14f56b48bb6206f9b17fcc3")
        throw new System.InvalidOperationException("Unexpected gun balance source");
    settings.FindProperty("_balance").objectReferenceValue = balance;
    changed = settings.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path).GetComponent<SSW.GunCast>();
    if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out fileId))
        throw new System.InvalidOperationException("GunCast file ID is missing");
}
finally
{
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
return new { changed, guid, fileId = fileId.ToString() };
