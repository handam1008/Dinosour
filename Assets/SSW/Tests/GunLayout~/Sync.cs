if (UnityEditor.EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode first");
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KDH/GameModules/KDH_Player Variant.prefab").GetComponent<KDH.Scripts.Gun.KDH_Gun>();
const string path = "Assets/SSW/Resources/Network/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var view = root.GetComponentInChildren<SSW.GunView>(true);
    var muzzle = (Transform)new UnityEditor.SerializedObject(view).FindProperty("_muzzle").objectReferenceValue;
    void Copy(Transform from, Transform to)
    {
        to.localPosition = from.localPosition;
        to.localRotation = from.localRotation;
        to.localScale = from.localScale;
    }
    Copy(source.GunPos, muzzle);
    Copy(source.Tanchang.transform, view.transform.Find("Tanchang"));
    var gun = root.GetComponent<SSW.GunCast>();
    var gunData = new UnityEditor.SerializedObject(gun);
    gunData.FindProperty("_gunView").objectReferenceValue = view;
    gunData.FindProperty("_effects").objectReferenceValue = root.GetComponent<SSW.GunFx>();
    gunData.ApplyModifiedPropertiesWithoutUndo();
    var weaponData = new UnityEditor.SerializedObject(view.GetComponent<SSW.WeaponView>());
    weaponData.FindProperty("_gun").objectReferenceValue = view;
    weaponData.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    return new { muzzle = muzzle.localPosition.ToString(), ammo = view.transform.Find("Tanchang").localPosition.ToString() };
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
