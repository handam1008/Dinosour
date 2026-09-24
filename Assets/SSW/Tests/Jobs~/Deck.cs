if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
const string folder = "Assets/SSW/Augments/Sword";
if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/SSW/Augments", "Sword");
var names = new[] { "받아내기", "질주", "긴 돌진" };
var descriptions = new[] { "패링에 성공하면 1초 동안 체력을 10 회복합니다.", "돌진 후 3초 동안 이동속도가 50% 증가합니다.", "돌진 거리가 50% 증가합니다." };
var icon = AssetDatabase.LoadAllAssetsAtPath("Assets/KHG/05.Asset/Sword.png").OfType<Sprite>().First();
var swords = new SSW.Augment[3];
for (int i = 0; i < swords.Length; i++)
{
    var perk = (SSW.SwordPerk)i;
    string path = folder + "/" + perk + ".asset";
    if (AssetDatabase.LoadAssetAtPath<SSW.Augment>(path) != null) throw new System.InvalidOperationException("Augment already exists");
    var augment = ScriptableObject.CreateInstance<SSW.SwordAugment>();
    augment.type = perk;
    augment.displayName = names[i];
    augment.description = descriptions[i];
    augment.icon = icon;
    AssetDatabase.CreateAsset(augment, path);
    swords[i] = augment;
}
var deck = AssetDatabase.LoadAssetAtPath<SSW.NetDeck>("Assets/SSW/Resources/Network/Deck.asset");
var added = AssetDatabase.FindAssets("t:Augment", new[] { "Assets/KDH", "Assets/JJW", "Assets/NKY" })
    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
    .Select(p => AssetDatabase.LoadAssetAtPath<SSW.Augment>(p))
    .Where(a => a is SSW.IJobRestrictedAugment restricted && (restricted.RequiredJob == SSW.PlayerJob.Gunner || restricted.RequiredJob == SSW.PlayerJob.Gambler || restricted.RequiredJob == SSW.PlayerJob.Assassin))
    .Concat(swords).Distinct().ToArray();
var so = new SerializedObject(deck);
var items = so.FindProperty("_augments");
var existing = new System.Collections.Generic.HashSet<UnityEngine.Object>();
for (int i = 0; i < items.arraySize; i++) existing.Add(items.GetArrayElementAtIndex(i).objectReferenceValue);
foreach (var item in added)
{
    if (!existing.Add(item)) continue;
    int index = items.arraySize++;
    items.GetArrayElementAtIndex(index).objectReferenceValue = item;
}
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssetIfDirty(deck);
return new { deck.Count, jobs = System.Enum.GetValues(typeof(SSW.PlayerJob)).Cast<SSW.PlayerJob>().Where(j => j != SSW.PlayerJob.None).Select(j => new { job = j.ToString(), choices = deck.Candidates(j, new int[0], false).Count }).ToArray() };
