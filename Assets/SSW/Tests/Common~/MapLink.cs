public static class MapLink
{
    public static object Main()
    {
        return new { maps = new[] { Bind(13), Bind(16) } };
    }

    static object Bind(int number)
    {
        string path = "Assets/SSW/Maps/Battle/Map" + number + ".prefab";
        var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
        try
        {
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>((number == 13 ? "Assets/KDH/GameModules/Maps/" : "Assets/MapPrefab/") + "KDH_Map " + number + ".prefab");
            UnityEngine.GameObject content;
            if (root.transform.childCount == 1 && UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(root.transform.GetChild(0).gameObject) == source)
                content = root.transform.GetChild(0).gameObject;
            else
            {
                while (root.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                content = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source, root.scene);
                content.transform.SetParent(root.transform, true);
            }
            foreach (var system in content.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)) UnityEngine.Object.DestroyImmediate(system.gameObject);
            if (number == 13)
            {
                foreach (var original in content.GetComponentsInChildren<KDH.Scripts.Objects.KDH_SakuraMode>(true))
                {
                    var field = original.GetComponent<SSW.SakuraField>();
                    if (field == null) field = original.gameObject.AddComponent<SSW.SakuraField>();
                    var petals = original.GetComponent<UnityEngine.ParticleSystem>();
                    var collision = petals.collision;
                    collision.enabled = true;
                    collision.sendCollisionMessages = true;
                    var so = new UnityEditor.SerializedObject(field);
                    so.FindProperty("_petals").objectReferenceValue = petals;
                    so.FindProperty("_effect").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ParticleSystem>("Assets/SSW/Maps/Battle/SakuraFx.prefab");
                    so.ApplyModifiedPropertiesWithoutUndo();
                    UnityEngine.Object.DestroyImmediate(original);
                }
                foreach (var pool in content.GetComponentsInChildren<KDH_SakuraEffectPooling>(true)) UnityEngine.Object.DestroyImmediate(pool);
            }
            var points = new System.Collections.Generic.List<UnityEngine.Transform>();
            foreach (var item in content.GetComponentsInChildren<UnityEngine.Transform>(true))
                if (item.name.Replace(" ", "").StartsWith("SpawnPoint", System.StringComparison.Ordinal)) points.Add(item);
            points.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (points.Count != 2) throw new System.InvalidOperationException("Map requires two spawn points");
            var spawn = new UnityEditor.SerializedObject(root.GetComponent<SSW.SpawnPoints>());
            spawn.FindProperty("_first").objectReferenceValue = points[0];
            spawn.FindProperty("_second").objectReferenceValue = points[1];
            spawn.ApplyModifiedPropertiesWithoutUndo();
            var bounds = new UnityEngine.Bounds();
            bool first = true;
            foreach (var sprite in content.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))
            {
                if (sprite.gameObject.layer != 8) continue;
                if (first) { bounds = sprite.bounds; first = false; }
                else bounds.Encapsulate(sprite.bounds);
            }
            if (first) throw new System.InvalidOperationException("Map has no terrain");
            var serverOnly = new System.Collections.Generic.List<UnityEngine.Object>();
            var moving = new System.Collections.Generic.List<UnityEngine.Object>();
            foreach (var item in content.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))
            {
                if (item is KDH.Scripts.Objects.KDH_TouchScrrenOutline || item is KDH.Scripts.Objects.KDH_Earthquake
                    || item is KDH.Scripts.Objects.KDH_IceArea || item is KDH.Scripts.Objects.KDH_SakuraMode) serverOnly.Add(item);
                if (item is KDH.Scripts.Objects.KDH_Earthquake) moving.Add(item.transform);
            }
            void SetArray(UnityEditor.SerializedObject target, string name, System.Collections.Generic.IEnumerable<UnityEngine.Object> items)
            {
                var values = new System.Collections.Generic.List<UnityEngine.Object>(items);
                var property = target.FindProperty(name);
                property.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            var map = new UnityEditor.SerializedObject(root.GetComponent<SSW.BattleMap>());
            map.FindProperty("_bounds").boundsValue = bounds;
            map.FindProperty("_fallY").floatValue = bounds.min.y - 3f;
            SetArray(map, "_serverOnly", serverOnly);
            map.ApplyModifiedPropertiesWithoutUndo();
            var bodies = new System.Collections.Generic.List<UnityEngine.Object>();
            foreach (var body in content.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true))
                if (body.bodyType != UnityEngine.RigidbodyType2D.Static) bodies.Add(body);
            foreach (var joint in content.GetComponentsInChildren<UnityEngine.DistanceJoint2D>(true))
            {
                joint.autoConfigureDistance = false;
                joint.distance = UnityEngine.Vector2.Distance(joint.transform.TransformPoint(joint.anchor), joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
            }
            var motion = new UnityEditor.SerializedObject(root.GetComponent<SSW.MapMotion>());
            SetArray(motion, "_bodies", bodies);
            SetArray(motion, "_moving", moving);
            SetArray(motion, "_pins", content.GetComponentsInChildren<SSW.Pin>(true));
            SetArray(motion, "_joints", content.GetComponentsInChildren<UnityEngine.Joint2D>(true));
            motion.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { path, changed = true, bodies = bodies.Count, pins = content.GetComponentsInChildren<SSW.Pin>(true).Length };
        }
        finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
    }
}
