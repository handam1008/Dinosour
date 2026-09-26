if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
string path = "Assets/SSW/Maps/Battle/Map09.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var original = root.GetComponentInChildren<KDH.Scripts.Objects.KDH_Volcano>(true);
    if (original == null) return "Already configured";
    var effect = original.GetComponent<UnityEngine.ParticleSystem>();
    var lava = original.gameObject.AddComponent<SSW.Lava>();
    var target = new UnityEditor.SerializedObject(lava);
    target.FindProperty("_effect").objectReferenceValue = effect;
    target.FindProperty("_damage").floatValue = new UnityEditor.SerializedObject(original).FindProperty("damage").floatValue;
    target.ApplyModifiedPropertiesWithoutUndo();
    var player = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetPlayer>("Assets/SSW/Resources/Network/Player.prefab");
    var collision = effect.collision;
    collision.collidesWith |= 1 << player.Collider.gameObject.layer;
    var main = effect.main;
    main.playOnAwake = false;
    effect.useAutoRandomSeed = false;
    effect.randomSeed = 1;
    var settings = new UnityEditor.SerializedObject(root.GetComponent<SSW.BattleMap>());
    var serverOnly = settings.FindProperty("_serverOnly");
    for (int i = serverOnly.arraySize - 1; i >= 0; i--)
        if (serverOnly.GetArrayElementAtIndex(i).objectReferenceValue == original)
        {
            serverOnly.GetArrayElementAtIndex(i).objectReferenceValue = null;
            serverOnly.DeleteArrayElementAtIndex(i);
        }
    settings.ApplyModifiedPropertiesWithoutUndo();
    UnityEngine.Object.DestroyImmediate(original);
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    return new { mask = collision.collidesWith.value, damage = 20, synced = true };
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
