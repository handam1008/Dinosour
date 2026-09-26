public static class GunSetup
{
    public static object Main()
    {
        if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
        const string folder = "Assets/SSW/Resources/Network/Gun";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/SSW/Resources/Network", "Gun");
        UnityEngine.GameObject Visual(string name, UnityEngine.GameObject prefab, bool ui = false)
        {
            string path = folder + "/" + name + ".prefab";
            var obj = UnityEditor.PrefabUtility.LoadPrefabContents(UnityEditor.AssetDatabase.GetAssetPath(prefab));
            try
            {
                foreach (var behaviour in obj.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))
                    if (!ui || behaviour is not UnityEngine.EventSystems.UIBehaviour || behaviour is UnityEngine.UI.GraphicRaycaster)
                        UnityEngine.Object.DestroyImmediate(behaviour);
                foreach (var shape in obj.GetComponentsInChildren<UnityEngine.Collider2D>(true)) UnityEngine.Object.DestroyImmediate(shape);
                foreach (var body in obj.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true)) UnityEngine.Object.DestroyImmediate(body);
                foreach (var graphic in obj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;
                foreach (var system in obj.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))
                {
                    var main = system.main;
                    main.stopAction = UnityEngine.ParticleSystemStopAction.None;
                }
                obj.name = name;
                obj.SetActive(true);
                return UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally { UnityEditor.PrefabUtility.UnloadPrefabContents(obj); }
        }
        UnityEngine.GameObject Asset(string path) => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
        UnityEngine.GameObject Ref(UnityEngine.Object obj, string name) => (UnityEngine.GameObject)new UnityEditor.SerializedObject(obj).FindProperty(name).objectReferenceValue;
        var original = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/KDH/GameModules/KDH_Player Variant.prefab");
        var playerRoot = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/SSW/Resources/Network/Player.prefab");
        try
        {
            var sourceGun = original.GetComponent<KDH.Scripts.Gun.KDH_Gun>();
            var sourceBullet = sourceGun.BulletPrefab.GetComponent<KDH.Scripts.Bullet.KDH_Bullet>();
            var mapping = new UnityEditor.SerializedObject(original.GetComponent<KDH.Scripts.Player.GunnerAugmentController>()).FindProperty("augmentMappings");
            var data = new System.Collections.Generic.Dictionary<KDH.Scripts.Arguments.GunnerAugmentType, KDH.Scripts.Upgrade.KDH_BulletAbilityDataSO>();
            var playerData = new System.Collections.Generic.Dictionary<KDH.Scripts.Arguments.GunnerAugmentType, KDH.Scripts.Upgrade.KDH_PlayerAbilitySO>();
            for (int i = 0; i < mapping.arraySize; i++)
            {
                var entry = mapping.GetArrayElementAtIndex(i);
                var type = (KDH.Scripts.Arguments.GunnerAugmentType)entry.FindPropertyRelative("type").enumValueIndex;
                if (entry.FindPropertyRelative("bulletAbilityData").objectReferenceValue is KDH.Scripts.Upgrade.KDH_BulletAbilityDataSO bullet) data.Add(type, bullet);
                if (entry.FindPropertyRelative("playerAbilityData").objectReferenceValue is KDH.Scripts.Upgrade.KDH_PlayerAbilitySO ability) playerData.Add(type, ability);
            }
            if (data.Count != 8 || playerData.Count != 2 || sourceGun.AmmoPrefabs.Length != 9) throw new System.InvalidOperationException("Unexpected original gun configuration");
            var effects = new System.Collections.Generic.List<(KDH.Scripts.Arguments.GunnerAugmentType Type, UnityEngine.GameObject Normal, UnityEngine.GameObject Charged, UnityEngine.GameObject Trail)>();
            foreach (var item in data)
            {
                string name = item.Key.ToString().Replace("Bullet", "").Replace("Quest_EvolutionAbility", "Evolution");
                effects.Add((item.Key, Visual(name + "Hit", item.Value.bulletNormalEffectPrefab),
                    Visual(name + "Charged", item.Value.bulletUpgradedEffectPrefab), Visual(name + "Trail", item.Value.bulletTrailEffectPrefab)));
            }
            var lightning = data[KDH.Scripts.Arguments.GunnerAugmentType.LightningBullet].bulletAbilityPrefab.GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_LightningBullet>();
            var flags = Visual("Flags", Ref(lightning, "hitFlag"));
            var strike = Visual("Lightning", Ref(lightning, "lightningEffect"));
            var evolution = data[KDH.Scripts.Arguments.GunnerAugmentType.Quest_EvolutionAbility].bulletAbilityPrefab;
            var evolve = Visual("Evolve", Ref(evolution.GetComponentInChildren<KDH.Scripts.Effects.Bullets.KDH_Quest_EvolutionClearEffectFeedback>(true), "clearEffect"));
            var quest = Visual("Quest", Ref(evolution.GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_Quest_EvolutionBullet>(), "questUIPrefab"), true);
            var shrinkSource = playerData[KDH.Scripts.Arguments.GunnerAugmentType.ShrinkingDeviceAbility].playerAbilityPrefab;
            var shrink = Visual("Shrink", Ref(shrinkSource.GetComponentInChildren<KDH.Scripts.Effects.Players.KDH_ShrinkingDeviceEffectFeedback>(true), "effectPrefab"));
            var ammo = Visual("Ammo", Asset("Assets/KDH/GameModules/Prefabs/Ammo.prefab"));
            var ghostRoot = new UnityEngine.GameObject("Ghost", typeof(UnityEngine.SpriteRenderer));
            UnityEngine.GameObject ghost;
            try { ghost = UnityEditor.PrefabUtility.SaveAsPrefabAsset(ghostRoot, folder + "/Ghost.prefab"); }
            finally { UnityEngine.Object.DestroyImmediate(ghostRoot); }
            var player = playerRoot.GetComponent<SSW.NetPlayer>();
            var gun = playerRoot.GetComponent<SSW.GunCast>();
            var gunSo = new UnityEditor.SerializedObject(gun);
            var weapon = (SSW.WeaponView)gunSo.FindProperty("_view").objectReferenceValue;
            var view = weapon.GetComponent<SSW.GunView>() ?? weapon.gameObject.AddComponent<SSW.GunView>();
            var fx = playerRoot.GetComponent<SSW.GunFx>() ?? playerRoot.AddComponent<SSW.GunFx>();
            var pool = playerRoot.GetComponentInChildren<SSW.EffectPool>(true);
            if (pool == null)
            {
                var poolRoot = new UnityEngine.GameObject("EffectPool");
                poolRoot.transform.SetParent(playerRoot.transform, false);
                pool = poolRoot.AddComponent<SSW.EffectPool>();
            }
            foreach (var child in new[] { "Tanchang", "FirePos" })
            {
                var existing = weapon.transform.Find(child);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            var holder = new UnityEngine.GameObject("Tanchang").transform;
            holder.SetParent(weapon.transform, false);
            holder.localPosition = sourceGun.Tanchang.transform.localPosition;
            holder.localRotation = sourceGun.Tanchang.transform.localRotation;
            holder.localScale = sourceGun.Tanchang.transform.localScale;
            var muzzle = new UnityEngine.GameObject("FirePos").transform;
            muzzle.SetParent(weapon.transform, false);
            muzzle.localPosition = sourceGun.GunPos.localPosition;
            muzzle.localRotation = sourceGun.GunPos.localRotation;
            muzzle.localScale = sourceGun.GunPos.localScale;
            weapon.transform.localScale = UnityEngine.Vector3.one;
            var weaponSo = new UnityEditor.SerializedObject(weapon);
            weaponSo.FindProperty("_gun").objectReferenceValue = view;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();
            var viewSo = new UnityEditor.SerializedObject(view);
            viewSo.FindProperty("_player").objectReferenceValue = player;
            viewSo.FindProperty("_gun").objectReferenceValue = gun;
            viewSo.FindProperty("_effects").objectReferenceValue = fx;
            viewSo.FindProperty("_muzzle").objectReferenceValue = muzzle;
            var slots = viewSo.FindProperty("_slots");
            slots.arraySize = sourceGun.AmmoPrefabs.Length;
            for (int i = 0; i < sourceGun.AmmoPrefabs.Length; i++)
            {
                var source = sourceGun.AmmoPrefabs[i].transform;
                var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(ammo, holder);
                instance.name = source.name;
                instance.transform.localPosition = source.localPosition;
                instance.transform.localRotation = source.localRotation;
                instance.transform.localScale = source.localScale;
                foreach (var sprite in instance.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true)) sprite.sortingOrder += 16;
                slots.GetArrayElementAtIndex(i).FindPropertyRelative("Root").objectReferenceValue = instance;
                slots.GetArrayElementAtIndex(i).FindPropertyRelative("Fill").objectReferenceValue = instance.transform.Find("GagePivot");
                instance.SetActive(false);
            }
            var oldQuest = viewSo.FindProperty("_quest").objectReferenceValue as UnityEngine.Canvas;
            if (oldQuest != null) UnityEngine.Object.DestroyImmediate(oldQuest.gameObject);
            var questObject = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(quest, playerRoot.transform);
            questObject.SetActive(false);
            viewSo.FindProperty("_quest").objectReferenceValue = questObject.GetComponent<UnityEngine.Canvas>();
            viewSo.FindProperty("_questText").objectReferenceValue = questObject.GetComponentInChildren<UnityEngine.UI.Text>(true);
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            gunSo.FindProperty("_gunView").objectReferenceValue = view;
            gunSo.FindProperty("_effects").objectReferenceValue = fx;
            gunSo.FindProperty("_capacity").intValue = sourceGun.MaxAmmo;
            gunSo.FindProperty("_reload").floatValue = sourceGun.ChargeSpeed;
            gunSo.FindProperty("_interval").floatValue = 0f;
            gunSo.FindProperty("_damage").floatValue = sourceBullet.Damage;
            gunSo.FindProperty("_speed").floatValue = sourceBullet.Speed;
            var questSource = new UnityEditor.SerializedObject(evolution.GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_Quest_EvolutionBullet>());
            gunSo.FindProperty("_questHits").intValue = questSource.FindProperty("questClearCount").intValue;
            var poison = data[KDH.Scripts.Arguments.GunnerAugmentType.PoisonBullet].bulletAbilityPrefab.GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_PoisonBullet>();
            gunSo.FindProperty("_poisonTicks").intValue = poison.DotCount;
            var shrinkSettings = new UnityEditor.SerializedObject(shrinkSource.GetComponent<KDH_ShrinkingDevice>());
            gunSo.FindProperty("_shrinkCooldown").floatValue = shrinkSettings.FindProperty("cooldown").floatValue;
            gunSo.ApplyModifiedPropertiesWithoutUndo();
            var fxSo = new UnityEditor.SerializedObject(fx);
            var refs = new System.Collections.Generic.Dictionary<string, UnityEngine.Object>
            {
                ["_player"] = player, ["_gun"] = gun, ["_pool"] = pool, ["_flagPrefab"] = flags,
                ["_lightningPrefab"] = strike, ["_shrinkPrefab"] = shrink, ["_evolvePrefab"] = evolve, ["_ghostPrefab"] = ghost
            };
            foreach (var entry in refs) fxSo.FindProperty(entry.Key).objectReferenceValue = entry.Value;
            var fxEntries = fxSo.FindProperty("_effects");
            fxEntries.arraySize = effects.Count;
            for (int i = 0; i < effects.Count; i++)
            {
                var entry = fxEntries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Type").enumValueIndex = (int)effects[i].Type;
                entry.FindPropertyRelative("Normal").objectReferenceValue = effects[i].Normal;
                entry.FindPropertyRelative("Charged").objectReferenceValue = effects[i].Charged;
                entry.FindPropertyRelative("Trail").objectReferenceValue = effects[i].Trail;
            }
            var sprites = new UnityEditor.SerializedObject(playerRoot.GetComponent<SSW.PlayerFx>()).FindProperty("_sprites");
            var fxSprites = fxSo.FindProperty("_sprites");
            fxSprites.arraySize = sprites.arraySize;
            for (int i = 0; i < sprites.arraySize; i++) fxSprites.GetArrayElementAtIndex(i).objectReferenceValue = sprites.GetArrayElementAtIndex(i).objectReferenceValue;
            fxSo.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(playerRoot, "Assets/SSW/Resources/Network/Player.prefab");
            return new { slots = slots.arraySize, effects = effects.Count, speed = sourceBullet.Speed, damage = sourceBullet.Damage,
                quest = gun.QuestHits, poisonTicks = poison.DotCount, pool = pool.name };
        }
        finally
        {
            UnityEditor.PrefabUtility.UnloadPrefabContents(playerRoot);
            UnityEditor.PrefabUtility.UnloadPrefabContents(original);
        }
    }
}
