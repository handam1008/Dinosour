if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
string swordPath = "Assets/KHG/05.Asset/사이버-검.aseprite";
var icon = AssetDatabase.LoadAllAssetsAtPath(swordPath).OfType<Sprite>().First();
foreach (string guid in AssetDatabase.FindAssets("t:SwordAugment", new[] { "Assets/SSW/Augments/Sword" }))
{
    var augment = AssetDatabase.LoadAssetAtPath<SSW.SwordAugment>(AssetDatabase.GUIDToAssetPath(guid));
    augment.icon = icon;
    EditorUtility.SetDirty(augment);
    AssetDatabase.SaveAssetIfDirty(augment);
}
const string path = "Assets/SSW/Augments/Sword/DashPower.asset";
var power = AssetDatabase.LoadAssetAtPath<SSW.SwordAugment>(path);
if (power == null)
{
    power = ScriptableObject.CreateInstance<SSW.SwordAugment>();
    power.type = SSW.SwordPerk.DashPower;
    power.displayName = "반격 돌진";
    power.description = "패링 성공 후 다음 돌진의 피해가 2배가 됩니다.";
    power.icon = icon;
    AssetDatabase.CreateAsset(power, path);
}
var deck = AssetDatabase.LoadAssetAtPath<SSW.NetDeck>("Assets/SSW/Resources/Network/Deck.asset");
var so = new SerializedObject(deck);
var items = so.FindProperty("_augments");
bool found = false;
for (int i = 0; i < items.arraySize; i++) found |= items.GetArrayElementAtIndex(i).objectReferenceValue == power;
if (!found) { int index = items.arraySize++; items.GetArrayElementAtIndex(index).objectReferenceValue = power; }
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssetIfDirty(deck);
var root = PrefabUtility.LoadPrefabContents("Assets/SSW/Resources/Network/Player.prefab");
try
{
    var weapon = root.GetComponentsInChildren<SSW.WeaponView>(true).Single(w => new SerializedObject(w).FindProperty("_job").enumValueIndex == (int)SSW.PlayerJob.Swordsman);
    var sprite = (SpriteRenderer)new SerializedObject(weapon).FindProperty("_sprite").objectReferenceValue;
    sprite.sprite = icon;
    PrefabUtility.SaveAsPrefabAsset(root, "Assets/SSW/Resources/Network/Player.prefab");
}
finally { PrefabUtility.UnloadPrefabContents(root); }
return new { icon.name, icon.rect, icon.bounds, deck.Count };
