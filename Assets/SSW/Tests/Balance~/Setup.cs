if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
const string sourceRoot = "Assets/KDH/GameModules/Prefabs/AbilityBullets/";
const string firePath = "Assets/SSW/Prefabs/Balance/FireBullet.prefab";
const string balancePath = "Assets/SSW/Resources/Network/GunBalance.asset";
const string sourcesPath = "Assets/SSW/Editor/GunSources.asset";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/SSW/Prefabs/Balance"))
    UnityEditor.AssetDatabase.CreateFolder("Assets/SSW/Prefabs", "Balance");
T Asset<T>(string path) where T : UnityEngine.Object
    => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new System.InvalidOperationException("Missing asset: " + path);
var fire = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(firePath);
if (fire == null)
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    try
    {
        var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(Asset<UnityEngine.GameObject>(sourceRoot + "KDH_FireBullet.prefab"), scene);
        var ability = instance.GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_FireBullet>();
        using var setup = new UnityEditor.SerializedObject(ability);
        setup.FindProperty("dotDamage").intValue = 1;
        setup.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(ability);
        fire = UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, firePath);
    }
    finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
}
var balance = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunBalance>(balancePath);
if (balance == null)
{
    balance = UnityEngine.ScriptableObject.CreateInstance<SSW.GunBalance>();
    UnityEditor.AssetDatabase.CreateAsset(balance, balancePath);
}
var sources = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunSources>(sourcesPath);
if (sources == null)
{
    sources = UnityEngine.ScriptableObject.CreateInstance<SSW.GunSources>();
    using var setup = new UnityEditor.SerializedObject(sources);
    setup.FindProperty("_balance").objectReferenceValue = balance;
    setup.FindProperty("_fire").objectReferenceValue = fire.GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_FireBullet>();
    setup.FindProperty("_gravity").objectReferenceValue = Asset<UnityEngine.GameObject>(sourceRoot + "KDH_GravityBullet.prefab").GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_GravityBullet>();
    setup.FindProperty("_ice").objectReferenceValue = Asset<UnityEngine.GameObject>(sourceRoot + "KDH_IceBullet.prefab").GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_IceBullet>();
    setup.FindProperty("_poison").objectReferenceValue = Asset<UnityEngine.GameObject>(sourceRoot + "KDH_PoisonBullet.prefab").GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_PoisonBullet>();
    setup.FindProperty("_shuriken").objectReferenceValue = Asset<UnityEngine.GameObject>(sourceRoot + "KDH_ShurikenBullet.prefab").GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_ShurikenBullet>();
    setup.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.AssetDatabase.CreateAsset(sources, sourcesPath);
}
SSW.GunBake.BakeAll();
UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(balance, out string guid, out long fileId);
string root = System.IO.Path.GetFullPath("Logs/Balance26");
System.IO.Directory.CreateDirectory(root);
var patch = new { prefab = "Assets/SSW/Resources/Network/Player.prefab", component = "SSW.GunCast", field = "_balance", asset = balancePath, guid, fileId };
System.IO.File.WriteAllText(root + "/PlayerPatch.json", Newtonsoft.Json.JsonConvert.SerializeObject(patch, Newtonsoft.Json.Formatting.Indented));
return new { sources = sourcesPath, firePath, variant = UnityEditor.PrefabUtility.GetPrefabAssetType(fire).ToString(), balance = UnityEngine.JsonUtility.ToJson(balance), patch };
