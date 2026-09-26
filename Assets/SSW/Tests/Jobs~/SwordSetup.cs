if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
string path = "Assets/SSW/Resources/Network/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var settings = new UnityEditor.SerializedObject(root.GetComponent<SSW.SwordCast>());
    settings.FindProperty("_damage").floatValue = 10f;
    settings.FindProperty("_dashCooldown").floatValue = 7f;
    settings.FindProperty("_parryCooldown").floatValue = 6f;
    settings.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    return new { damage = 10, dash = 7, parry = 6 };
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
