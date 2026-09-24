string path = "Assets/SSW/Resources/Network/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    foreach (var view in root.GetComponentsInChildren<SSW.WeaponView>(true))
    {
        var so = new UnityEditor.SerializedObject(view);
        var job = (SSW.PlayerJob)so.FindProperty("_job").intValue;
        so.FindProperty("_angle").floatValue = job == SSW.PlayerJob.Swordsman ? 180f : job == SSW.PlayerJob.Assassin ? -90f : 0f;
        if (job == SSW.PlayerJob.Swordsman) so.FindProperty("_offset").floatValue = 0.95f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
return path;
