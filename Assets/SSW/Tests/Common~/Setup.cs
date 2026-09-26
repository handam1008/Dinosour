if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
const string folder = "Assets/SSW/Resources/Network/Common";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/SSW/Resources/Network", "Common");
UnityEngine.GameObject Visual(string name, string source)
{
    string path = folder + "/" + name + ".prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(source);
    try
    {
        foreach (var behaviour in root.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))
            if (behaviour is not HealDial && behaviour is not NuclearCharge && behaviour is not NuclearBlast && behaviour is not NuclearWave)
                UnityEngine.Object.DestroyImmediate(behaviour);
        foreach (var shape in root.GetComponentsInChildren<UnityEngine.Collider2D>(true)) UnityEngine.Object.DestroyImmediate(shape);
        foreach (var body in root.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true)) UnityEngine.Object.DestroyImmediate(body);
        foreach (var system in root.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))
        {
            var main = system.main;
            main.stopAction = UnityEngine.ParticleSystemStopAction.None;
        }
        root.SetActive(true);
        root.name = name;
        return UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
}
var visuals = new System.Collections.Generic.Dictionary<SSW.BuffEffect, UnityEngine.GameObject>();
var kinds = new[] { SSW.BuffEffect.Guard, SSW.BuffEffect.HealCharge, SSW.BuffEffect.HealBurst, SSW.BuffEffect.IceBlast,
    SSW.BuffEffect.Freeze, SSW.BuffEffect.NuclearCharge, SSW.BuffEffect.NuclearBlast, SSW.BuffEffect.Slam,
    SSW.BuffEffect.Mine, SSW.BuffEffect.Bomb, SSW.BuffEffect.Explosion };
var sources = new[] { "Barrier", "HealField", "HealEffect", "IceBlast", "IceFreeze", "NuclearCharge", "NuclearBlast", "SlamImpact", "Mine", "FuseBomb", "MineBlast" };
for (int i = 0; i < kinds.Length; i++) visuals[kinds[i]] = Visual(kinds[i].ToString(), "Assets/AJH/02.Prefabs/" + sources[i] + ".prefab");
var auras = new[] { Visual("SlowAura", "Assets/AJH/02.Prefabs/SlowArea.prefab"), Visual("MagnetAura", "Assets/AJH/02.Prefabs/MagnetArea.prefab"), Visual("NomAura", "Assets/AJH/02.Prefabs/NomArea.prefab") };
var tether = Visual("Tether", "Assets/AJH/02.Prefabs/Line.prefab").GetComponent<UnityEngine.LineRenderer>();
var pool = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.AugmentPool>("Assets/AJH/03.SO/Pool/RealCommonAugmentPool.asset");
var deck = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetDeck>("Assets/SSW/Resources/Network/Deck.asset");
var serializedDeck = new UnityEditor.SerializedObject(deck);
serializedDeck.FindProperty("_commonPool").objectReferenceValue = pool;
var items = serializedDeck.FindProperty("_augments");
var known = new System.Collections.Generic.HashSet<UnityEngine.Object>();
for (int i = 0; i < items.arraySize; i++) known.Add(items.GetArrayElementAtIndex(i).objectReferenceValue);
foreach (var augment in pool.augments)
{
    if (!known.Add(augment)) continue;
    int i = items.arraySize++;
    items.GetArrayElementAtIndex(i).objectReferenceValue = augment;
}
serializedDeck.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssetIfDirty(deck);
const string playerPath = "Assets/SSW/Resources/Network/Player.prefab";
var playerRoot = UnityEditor.PrefabUtility.LoadPrefabContents(playerPath);
try
{
    T Ensure<T>() where T : UnityEngine.Component => playerRoot.GetComponent<T>() ?? playerRoot.AddComponent<T>();
    var player = Ensure<SSW.NetPlayer>();
    var buffs = Ensure<SSW.NetBuff>();
    var health = Ensure<SSW.BuffHealth>();
    var skill = Ensure<SSW.BuffSkill>();
    var guard = Ensure<SSW.BuffGuard>();
    var areas = Ensure<SSW.BuffArea>();
    var fx = Ensure<SSW.BuffFx>();
    var source = Ensure<SSW.AugmentDrafter>();
    var motion = Ensure<SSW.PlayerController>();
    var animation = new UnityEditor.SerializedObject(player).FindProperty("_animation").objectReferenceValue;
    var leap = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.Augment>("Assets/AJH/03.SO/DefanceAugment/LeapBomb.asset");
    var refs = new System.Collections.Generic.Dictionary<string, UnityEngine.Object>
    {
        ["_player"] = player, ["_buffs"] = buffs, ["_health"] = health, ["_skill"] = skill, ["_guard"] = guard,
        ["_areas"] = areas, ["_fx"] = fx, ["_source"] = source, ["_motion"] = motion,
        ["_animation"] = animation, ["_leapBombAugment"] = leap, ["_tetherPrefab"] = tether
    };
    foreach (var component in new UnityEngine.Component[] { buffs, health, skill, guard, areas, fx })
    {
        var so = new UnityEditor.SerializedObject(component);
        foreach (var entry in refs)
        {
            var property = so.FindProperty(entry.Key);
            if (property != null && property.propertyType == UnityEditor.SerializedPropertyType.ObjectReference) property.objectReferenceValue = entry.Value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    var guardSo = new UnityEditor.SerializedObject(guard);
    guardSo.FindProperty("_nuclearWallMask").intValue = motion.GroundMask.value;
    guardSo.ApplyModifiedPropertiesWithoutUndo();
    var playerSo = new UnityEditor.SerializedObject(player);
    playerSo.FindProperty("_guard").objectReferenceValue = guard;
    playerSo.ApplyModifiedPropertiesWithoutUndo();
    var sourceSo = new UnityEditor.SerializedObject(source);
    sourceSo.FindProperty("_commonPool").objectReferenceValue = pool;
    sourceSo.ApplyModifiedPropertiesWithoutUndo();
    var fxSo = new UnityEditor.SerializedObject(fx);
    var effects = fxSo.FindProperty("_effects");
    effects.arraySize = kinds.Length;
    for (int i = 0; i < kinds.Length; i++)
    {
        var entry = effects.GetArrayElementAtIndex(i);
        entry.FindPropertyRelative("Kind").enumValueIndex = (int)kinds[i];
        entry.FindPropertyRelative("Prefab").objectReferenceValue = visuals[kinds[i]];
    }
    var auraList = fxSo.FindProperty("_auraPrefabs");
    auraList.arraySize = auras.Length;
    for (int i = 0; i < auras.Length; i++) auraList.GetArrayElementAtIndex(i).objectReferenceValue = auras[i];
    fxSo.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(playerRoot, playerPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(playerRoot); }
var descriptions = new System.Collections.Generic.Dictionary<string, string>
{
    ["Giant"] = "최대 체력이 55%, 몸 크기가 10% 증가한다.",
    ["Vampire"] = "입힌 피해의 55%만큼 체력을 회복한다.",
    ["Berserker"] = "체력이 75% 이하일 때 이동 속도가 45%, 피해가 60% 증가한다.",
    ["Confidence"] = "공격 성공 시 2초 동안 이동 속도가 30% 증가한다.",
    ["GlassCannon"] = "최대 체력이 30%, 몸 크기가 10% 감소하고 피해가 80% 증가한다.",
    ["Phoenix"] = "최대 체력이 25%, 몸 크기가 8% 감소한다. 라운드마다 치명상을 한 번 버티고 체력을 모두 회복한다. 부활 중 1초간 멈추고 2.5초간 무적이 된다.",
    ["DeathWaltz"] = "최대 체력이 30% 증가하고 받은 피해를 5초에 걸쳐 나누어 받는다.",
    ["Recharge"] = "방어가 끝나면 한 번 더 방어한다. 방어 쿨타임이 40% 증가한다."
};
foreach (var entry in descriptions)
{
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.CommonAugment>("Assets/SSW/Augments/Common/" + entry.Key + ".asset");
    asset.description = entry.Value;
    UnityEditor.EditorUtility.SetDirty(asset);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
}
return new { common = pool.augments.Length, deck = deck.Count, effects = visuals.Count, auras = auras.Length };
