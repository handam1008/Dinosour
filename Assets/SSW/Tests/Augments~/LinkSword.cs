if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
var types = new[] { SSW.SwordPerk.ParryCooldown, SSW.SwordPerk.DashBleed, SSW.SwordPerk.DashRoot, SSW.SwordPerk.DashRecovery, SSW.SwordPerk.StandingHeal, SSW.SwordPerk.SwordGrowth };
var names = new[] { "DashColldown", "DashHpDown", "DashStop", "DashStill", "StandHeal", "GlowSword" };
var deck = UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");
using var list = new UnityEditor.SerializedObject(deck);
var items = list.FindProperty("_augments");
var added = new System.Collections.Generic.List<object>();
for (int i = 0; i < types.Length; i++)
{
    string path = "Assets/SSW/Augments/Sword/" + names[i] + ".asset";
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.SwordAugment>(path);
    if (asset == null) throw new System.InvalidOperationException("Latest Base sword asset missing: " + path);
    asset.type = types[i];
    UnityEditor.EditorUtility.SetDirty(asset);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
    int id = 86 + i;
    if (items.arraySize <= id) items.arraySize = id + 1;
    items.GetArrayElementAtIndex(id).objectReferenceValue = asset;
    added.Add(new { id, path, asset.displayName });
}
list.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssetIfDirty(deck);
return new { added, swords = deck.Candidates(SSW.PlayerJob.Swordsman, System.Array.Empty<int>(), false).Count };
