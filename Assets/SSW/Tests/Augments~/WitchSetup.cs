if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
const string folder = "Assets/SSW/Augments/Witch";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/SSW/Augments", "Witch");
const string corrected = folder + "/Regeneration.asset";
var regen = UnityEditor.AssetDatabase.LoadAssetAtPath<RYU._01.Script.Argument.WitchAugment>(corrected);
if (regen == null)
{
    regen = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<RYU._01.Script.Argument.WitchAugment>("Assets/RYU/3.SO/Witch/reproductionUnlock.asset"));
    regen.name = "Regeneration";
    regen.type = RYU._01.Script.Argument.WitchAugmentType.reproductionUnlock;
    UnityEditor.AssetDatabase.CreateAsset(regen, corrected);
}
var additions = new SSW.Augment[]
{
    UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.Augment>("Assets/RYU/3.SO/Witch/PoisonUnlock.asset"),
    regen,
    UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.Augment>("Assets/RYU/3.SO/Witch/AllUnlock.asset"),
    UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.Augment>("Assets/RYU/3.SO/Witch/Pocket.asset")
};
var deck = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetDeck>("Assets/SSW/Resources/Network/Deck.asset");
var target = new UnityEditor.SerializedObject(deck);
var entries = target.FindProperty("_augments");
var added = new System.Collections.Generic.List<object>();
foreach (var item in additions)
{
    if (item == null) throw new System.InvalidOperationException("Missing witch asset");
    bool exists = false;
    for (int i = 0; i < entries.arraySize; i++)
        if (entries.GetArrayElementAtIndex(i).objectReferenceValue == item) exists = true;
    if (exists) continue;
    int index = entries.arraySize++;
    entries.GetArrayElementAtIndex(index).objectReferenceValue = item;
    added.Add(new { index, item.displayName, path = UnityEditor.AssetDatabase.GetAssetPath(item) });
}
target.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssetIfDirty(deck);
return new { total = deck.Count, added };
