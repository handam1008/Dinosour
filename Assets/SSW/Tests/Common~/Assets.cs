var pool = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.AugmentPool>("Assets/AJH/03.SO/Pool/RealCommonAugmentPool.asset");
var deck = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetDeck>("Assets/SSW/Resources/Network/Deck.asset");
var player = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab");
var checks = new System.Collections.Generic.List<string>();
void Check(bool ok, string label) { if (!ok) throw new System.InvalidOperationException(label); checks.Add(label); }
Check(pool.augments.Length == 30 && System.Linq.Enumerable.Distinct(pool.augments).Count() == 30, "source pool has 30 unique augments");
foreach (SSW.PlayerJob job in System.Enum.GetValues(typeof(SSW.PlayerJob)))
    Check(deck.Candidates(job, new int[0], true).Count == 30, job + " receives every common augment");
foreach (var type in new[] { typeof(SSW.BuffHealth), typeof(SSW.BuffSkill), typeof(SSW.BuffGuard), typeof(SSW.BuffArea), typeof(SSW.BuffFx), typeof(SSW.NetBuff) })
{
    var component = player.GetComponent(type);
    Check(component != null, type.Name + " attached");
    var so = new UnityEditor.SerializedObject(component);
    var property = so.GetIterator();
    while (property.NextVisible(true))
        if (property.propertyType == UnityEditor.SerializedPropertyType.ObjectReference)
            Check(property.objectReferenceValue != null, type.Name + "." + property.propertyPath + " assigned");
}
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SSW/Resources/Network/Common" }))
{
    var visual = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    Check(visual.GetComponentsInChildren<UnityEngine.Collider2D>(true).Length == 0 && visual.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true).Length == 0, visual.name + " has no gameplay physics");
    foreach (var behaviour in visual.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))
        Check(behaviour is HealDial || behaviour is NuclearCharge || behaviour is NuclearBlast || behaviour is NuclearWave, visual.name + " contains visual scripts only");
}
return new { passed = checks.Count, checks, common = pool.augments.Length, deck = deck.Count, protocol = SSW.NetGame.Protocol };
