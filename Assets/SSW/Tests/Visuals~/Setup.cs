if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first");
string path = "Assets/SSW/Resources/Network/Player.prefab";
string backup = "Logs/Visuals/BeforeSetup/Player.prefab";
string Hash(string file)
{
    using var hash = System.Security.Cryptography.SHA256.Create();
    return System.BitConverter.ToString(hash.ComputeHash(System.IO.File.ReadAllBytes(file))).Replace("-", "");
}
if (!System.IO.File.Exists(backup) || Hash(path) != Hash(backup)) throw new System.InvalidOperationException("Verified current prefab backup required");
string beforeHash = Hash(path);
var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
if (stage != null && stage.assetPath == path && stage.scene.isDirty) throw new System.InvalidOperationException("Unsaved Player prefab changes");
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/RYU/02.Prefabs/Agent/Witch.prefab")
    .GetComponentInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true);
if (source == null) throw new System.InvalidOperationException("Original SpeedAfterimage is missing");
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var player = root.GetComponent<SSW.NetPlayer>();
    var effects = root.GetComponent<SSW.PlayerFx>();
    string Snapshot(UnityEngine.Component component)
    {
        var data = Newtonsoft.Json.Linq.JObject.Parse(UnityEditor.EditorJsonUtility.ToJson(component));
        if (component == root.transform) data.Remove("m_Children");
        if (component == player) data.Remove("_skin");
        if (component == effects) data.Remove("_trail");
        return data.ToString(Newtonsoft.Json.Formatting.None);
    }
    var preserved = root.GetComponentsInChildren<UnityEngine.Component>(true).Where(c => c != null)
        .Select(c => new { component = c, json = Snapshot(c) }).ToArray();
    var skins = root.GetComponentsInChildren<RYU._01.Script.Customize.DinoSkinApplier>(true);
    var trails = root.GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true);
    if (skins.Length > 1 || trails.Length > 1) throw new System.InvalidOperationException("Duplicate original visual components");
    var skin = skins.Length == 1 ? skins[0] : root.AddComponent<RYU._01.Script.Customize.DinoSkinApplier>();
    var skinData = new UnityEditor.SerializedObject(skin);
    skinData.FindProperty("applyOnStart").boolValue = false;
    skinData.ApplyModifiedPropertiesWithoutUndo();
    var playerData = new UnityEditor.SerializedObject(player);
    playerData.FindProperty("_skin").objectReferenceValue = skin;
    playerData.ApplyModifiedPropertiesWithoutUndo();
    UnityEngine.GameObject child;
    RYU._01.Script.FeedBack.SpeedAfterimage trail;
    if (trails.Length == 1)
    {
        trail = trails[0];
        child = trail.gameObject;
        if (child == root) throw new System.InvalidOperationException("Trail must have its own child for fade cleanup");
    }
    else
    {
        child = new UnityEngine.GameObject("Afterimages");
        child.transform.SetParent(root.transform, false);
        trail = child.AddComponent<RYU._01.Script.FeedBack.SpeedAfterimage>();
        UnityEditor.EditorUtility.CopySerialized(source, trail);
    }
    var adapter = child.GetComponent<SSW.SpeedTrail>() ?? child.AddComponent<SSW.SpeedTrail>();
    var data = new UnityEditor.SerializedObject(adapter);
    data.FindProperty("_player").objectReferenceValue = player;
    data.FindProperty("_effect").objectReferenceValue = trail;
    data.FindProperty("_fadeDuration").floatValue = new UnityEditor.SerializedObject(trail).FindProperty("fadeDuration").floatValue;
    var sprites = root.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true);
    var values = data.FindProperty("_sprites");
    values.arraySize = sprites.Length;
    for (int i = 0; i < sprites.Length; i++) values.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    data.ApplyModifiedPropertiesWithoutUndo();
    var fxData = new UnityEditor.SerializedObject(effects);
    fxData.FindProperty("_trail").objectReferenceValue = adapter;
    fxData.ApplyModifiedPropertiesWithoutUndo();
    foreach (var entry in preserved)
        if (entry.json != Snapshot(entry.component)) throw new System.InvalidOperationException("Existing component changed: " + entry.component.GetType().Name);
    if (Hash(path) != beforeHash) throw new System.InvalidOperationException("Player prefab changed during setup");
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    return new { path, beforeHash, preserved = preserved.Length, sprites = sprites.Length, skin = skin.GetType().Name, trail = trail.GetType().Name, fade = data.FindProperty("_fadeDuration").floatValue };
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
