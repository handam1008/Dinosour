if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
{
    var so = new SerializedObject(target);
    so.FindProperty(field).objectReferenceValue = value;
    so.ApplyModifiedPropertiesWithoutUndo();
}
void Refs(UnityEngine.Object target, string field, UnityEngine.Object[] values)
{
    var so = new SerializedObject(target);
    var array = so.FindProperty(field);
    array.arraySize = values.Length;
    for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    so.ApplyModifiedPropertiesWithoutUndo();
}
SpriteRenderer Source(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<SpriteRenderer>(true);
var sprites = new[] {
    Source("Assets/KDH/GameModules/Prefabs/Bullet.prefab").sprite,
    Source("Assets/JJW/Prefab/SilverCoin.prefab").sprite,
    Source("Assets/JJW/Prefab/GoldCoin.prefab").sprite,
    Source("Assets/NKY/Skill.prefab").sprite
};
var material = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SSW/Resources/Network/Potion.prefab").GetComponent<SpriteRenderer>().sharedMaterial;
const string boltPath = "Assets/SSW/Resources/Network/Bolt.prefab";
if (AssetDatabase.LoadAssetAtPath<GameObject>(boltPath) != null) throw new System.InvalidOperationException("Bolt already exists");
var temp = new GameObject("Bolt");
var body = temp.AddComponent<Rigidbody2D>();
body.gravityScale = 0f;
body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
var renderer = temp.AddComponent<SpriteRenderer>();
renderer.sprite = sprites[0];
renderer.sharedMaterial = material;
renderer.sortingOrder = 12;
var net = temp.AddComponent<Unity.Netcode.NetworkObject>();
var flight = temp.AddComponent<SSW.ShotSync>();
var bolt = temp.AddComponent<SSW.NetBolt>();
Set(flight, "_body", body);
Set(flight, "_sprite", renderer);
Set(bolt, "_flight", flight);
Set(bolt, "_body", body);
Set(bolt, "_sprite", renderer);
Refs(bolt, "_styles", sprites);
var root = PrefabUtility.LoadPrefabContents("Assets/SSW/Resources/Network/Player.prefab");
try
{
    var player = root.GetComponent<SSW.NetPlayer>();
    var fs = new SerializedObject(flight);
    fs.FindProperty("_ground").intValue = player.GroundMask.value;
    fs.ApplyModifiedPropertiesWithoutUndo();
    var boltPrefab = PrefabUtility.SaveAsPrefabAsset(temp, boltPath);
    var cast = root.GetComponent<SSW.NetCast>();
    Set(cast, "_boltPrefab", boltPrefab.GetComponent<SSW.NetBolt>());
    var fx = root.AddComponent<SSW.PlayerFx>();
    Set(fx, "_player", player);
    Set(player, "_effects", fx);
    Set(player, "_buffs", root.GetComponent<SSW.NetBuff>());
    var canvas = new GameObject("StatusCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
    canvas.transform.SetParent(root.transform, false);
    canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.GetComponent<Canvas>().sortingOrder = 25;
    var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
    scaler.matchWidthOrHeight = 0.5f;
    var blind = new GameObject("VisionMask", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
    blind.transform.SetParent(canvas.transform, false);
    blind.rectTransform.anchorMin = Vector2.zero;
    blind.rectTransform.anchorMax = Vector2.one;
    blind.rectTransform.offsetMin = blind.rectTransform.offsetMax = Vector2.zero;
    blind.raycastTarget = false;
    var mask = new Material(Shader.Find("UI/VisionMask"));
    AssetDatabase.CreateAsset(mask, "Assets/SSW/Resources/Shaders/VisionMask.mat");
    blind.material = mask;
    blind.enabled = false;
    Set(fx, "_blind", blind);
    var gunSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KDH/GameModules/KDH_Player Variant.prefab").GetComponentsInChildren<SpriteRenderer>(true).Single(s => s.name == "GunImage");
    var swordSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/KHG/05.Asset/Sword.png").OfType<Sprite>().First();
    var types = new[] { typeof(SSW.GunCast), typeof(SSW.CoinCast), typeof(SSW.KnifeCast), typeof(SSW.SwordCast) };
    var weaponSprites = new[] { gunSource.sprite, sprites[2], sprites[3], swordSprite };
    var jobs = new SSW.JobCast[types.Length];
    for (int i = 0; i < types.Length; i++)
    {
        var job = (SSW.JobCast)root.AddComponent(types[i]);
        jobs[i] = job;
        Set(job, "Player", player);
        Set(job, "_source", root.GetComponent<SSW.AugmentDrafter>());
        var weapon = new GameObject(job.Job.ToString());
        weapon.transform.SetParent(player.View, false);
        weapon.transform.localScale = Vector3.one * (i < 2 ? 0.6f : 0.8f);
        var style = weapon.AddComponent<SpriteRenderer>();
        style.sprite = weaponSprites[i];
        style.sharedMaterial = material;
        style.sortingOrder = 15;
        var view = weapon.AddComponent<SSW.WeaponView>();
        Set(view, "_player", player);
        Set(view, "_sprite", style);
        var vs = new SerializedObject(view);
        vs.FindProperty("_job").enumValueIndex = (int)job.Job;
        vs.FindProperty("_melee").boolValue = i >= 2;
        vs.ApplyModifiedPropertiesWithoutUndo();
        Set(job, "_view", view);
    }
    Refs(cast, "_jobs", jobs);
    Refs(fx, "_sprites", root.GetComponentsInChildren<SpriteRenderer>(true));
    PrefabUtility.SaveAsPrefabAsset(root, "Assets/SSW/Resources/Network/Player.prefab");
    var gameRoot = PrefabUtility.LoadPrefabContents("Assets/SSW/Resources/Network/NetGame.prefab");
    try
    {
        var game = gameRoot.GetComponent<SSW.NetGame>();
        var so = new SerializedObject(game);
        var shots = so.FindProperty("_shots");
        int index = shots.arraySize++;
        shots.GetArrayElementAtIndex(index).objectReferenceValue = boltPrefab.GetComponent<Unity.Netcode.NetworkObject>();
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(gameRoot, "Assets/SSW/Resources/Network/NetGame.prefab");
    }
    finally { PrefabUtility.UnloadPrefabContents(gameRoot); }
}
finally
{
    PrefabUtility.UnloadPrefabContents(root);
    UnityEngine.Object.DestroyImmediate(temp);
}
return new { bolt = boltPath, jobs = 4 };
