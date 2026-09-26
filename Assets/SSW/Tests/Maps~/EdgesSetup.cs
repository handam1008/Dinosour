if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
var game = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetGame>("Assets/SSW/Resources/Network/NetGame.prefab");
var rotation = (SSW.MapRotation)new UnityEditor.SerializedObject(game).FindProperty("_maps").objectReferenceValue;
int maps = 0;
int edges = 0;
foreach (var map in rotation.Prefabs)
{
    string path = UnityEditor.AssetDatabase.GetAssetPath(map);
    if (!path.StartsWith("Assets/SSW/")) throw new System.InvalidOperationException(path);
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        var original = root.GetComponentsInChildren<KDH.Scripts.Objects.KDH_TouchScrrenOutline>(true);
        if (original.Length == 0 && root.GetComponent<SSW.MapEdges>() != null)
        {
            var binding = new UnityEditor.SerializedObject(root.GetComponent<SSW.BattleMap>());
            binding.FindProperty("_edges").objectReferenceValue = root.GetComponent<SSW.MapEdges>();
            binding.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
            continue;
        }
        if (original.Length != 4) throw new System.InvalidOperationException(path + " must have four edges");
        var battle = root.GetComponent<SSW.BattleMap>();
        var component = root.GetComponent<SSW.MapEdges>() ?? root.AddComponent<SSW.MapEdges>();
        var target = new UnityEditor.SerializedObject(component);
        target.FindProperty("_map").objectReferenceValue = battle;
        var list = target.FindProperty("_edges");
        list.arraySize = original.Length;
        for (int i = 0; i < original.Length; i++)
        {
            var source = new UnityEditor.SerializedObject(original[i]);
            var item = list.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("Shape").objectReferenceValue = original[i].GetComponent<UnityEngine.Collider2D>();
            item.FindPropertyRelative("Damage").floatValue = source.FindProperty("damage").floatValue;
            item.FindPropertyRelative("Force").vector2Value = source.FindProperty("direction").vector2Value * source.FindProperty("knockbackForce").floatValue;
        }
        target.ApplyModifiedPropertiesWithoutUndo();
        var settings = new UnityEditor.SerializedObject(battle);
        settings.FindProperty("_edges").objectReferenceValue = component;
        var serverOnly = settings.FindProperty("_serverOnly");
        var retained = new System.Collections.Generic.List<UnityEngine.Object>();
        for (int i = 0; i < serverOnly.arraySize; i++)
        {
            var item = serverOnly.GetArrayElementAtIndex(i).objectReferenceValue;
            if (item != null && item is not KDH.Scripts.Objects.KDH_TouchScrrenOutline) retained.Add(item);
        }
        serverOnly.arraySize = retained.Count;
        for (int i = 0; i < retained.Count; i++) serverOnly.GetArrayElementAtIndex(i).objectReferenceValue = retained[i];
        settings.ApplyModifiedPropertiesWithoutUndo();
        foreach (var item in original) UnityEngine.Object.DestroyImmediate(item);
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
        maps++;
        edges += original.Length;
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
}
return new { maps, edges };
