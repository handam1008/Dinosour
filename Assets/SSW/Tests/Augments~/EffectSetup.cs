if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
const string path = "Assets/SSW/Resources/Network/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
bool changed;
try
{
    using var settings = new UnityEditor.SerializedObject(root.GetComponent<SSW.BuffHealth>());
    var health = settings.FindProperty("_tenLivesHealth");
    changed = health.floatValue != 3f;
    if (changed)
    {
        health.floatValue = 3f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    }
}
finally
{
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
return new { changed, health = 3 };
