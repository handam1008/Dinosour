using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SSW
{
    internal static class MapRefresh
    {
        public static BattleMap Apply(MapSources.Entry entry)
        {
            string path = entry.Map != null ? AssetDatabase.GetAssetPath(entry.Map)
                : "Assets/SSW/Maps/Battle/Map" + entry.Number.ToString("00") + ".prefab";
            if (!path.StartsWith("Assets/SSW/", StringComparison.Ordinal))
                throw new InvalidOperationException("맵 출력 경로는 Assets/SSW 안이어야 합니다: " + path);
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty && (stage.assetPath == path || stage.assetPath == AssetDatabase.GetAssetPath(entry.Source)))
                throw new InvalidOperationException("프리팹 편집 내용을 먼저 처리해야 합니다: " + stage.assetPath);
            bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            Scene preview = default;
            GameObject root;
            if (existing) root = PrefabUtility.LoadPrefabContents(path);
            else
            {
                preview = EditorSceneManager.NewPreviewScene();
                root = new GameObject("Map" + entry.Number.ToString("00"));
                SceneManager.MoveGameObjectToScene(root, preview);
            }
            try
            {
                GameObject content = Content(root, entry, existing);
                if (!existing) RemoveLocalObjects(content);
                BattleMap map = Ensure<BattleMap>(root);
                Ensure<NetworkObject>(root);
                SpawnPoints spawns = Ensure<SpawnPoints>(root);
                MapMotion motion = Ensure<MapMotion>(root);
                MapEdges edges = Ensure<MapEdges>(root);
                var moving = MapRead.Array(motion, "_moving");
                var server = MapRead.Array(map, "_serverOnly");
                var read = new MapRead(entry.Source, content);
                new MapParts(read, map, entry.Number, moving, server).Bind();
                Edges(read, map, edges);
                read.Finish();
                Transform[] points = entry.Source.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name.Replace(" ", "").StartsWith("SpawnPoint", StringComparison.Ordinal))
                    .OrderBy(item => item.name, StringComparer.Ordinal).Select(item => (Transform)read.Resolve(item)).ToArray();
                if (points.Length == 0 && existing)
                {
                    using var saved = new SerializedObject(spawns);
                    points = new[] { saved.FindProperty("_first").objectReferenceValue as Transform,
                        saved.FindProperty("_second").objectReferenceValue as Transform };
                }
                if (points.Length != 2 || points.Any(item => item == null))
                    throw new InvalidOperationException(path + ": SpawnPoint가 정확히 두 개여야 합니다.");
                MapRead.Edit(spawns, target =>
                {
                    target.FindProperty("_first").objectReferenceValue = points[0];
                    target.FindProperty("_second").objectReferenceValue = points[1];
                });
                SpriteRenderer[] terrain = root.GetComponentsInChildren<SpriteRenderer>(true).Where(item => item.gameObject.layer == 8).ToArray();
                if (terrain.Length == 0) throw new InvalidOperationException(path + ": Ground 지형이 없습니다.");
                Bounds bounds = terrain[0].bounds;
                foreach (SpriteRenderer item in terrain.Skip(1)) bounds.Encapsulate(item.bounds);
                MapRead.Edit(map, target =>
                {
                    if (!existing) target.FindProperty("_title").stringValue = entry.Source.name;
                    target.FindProperty("_spawns").objectReferenceValue = spawns;
                    target.FindProperty("_bounds").boundsValue = bounds;
                    target.FindProperty("_fallY").floatValue = bounds.min.y - 3f;
                    target.FindProperty("_edges").objectReferenceValue = edges;
                    MapRead.Array(target, "_serverOnly", server);
                });
                MapRead.Edit(motion, target =>
                {
                    MapRead.Array(target, "_bodies", root.GetComponentsInChildren<Rigidbody2D>(true)
                        .Where(item => item.bodyType != RigidbodyType2D.Static));
                    MapRead.Array(target, "_moving", moving);
                    MapRead.Array(target, "_pins", root.GetComponentsInChildren<Pin>(true));
                    MapRead.Array(target, "_joints", root.GetComponentsInChildren<Joint2D>(true));
                });
                if (root.GetComponentsInChildren<MonoBehaviour>(true).Any(item => item == null))
                    throw new InvalidOperationException(path + ": 스크립트가 누락됐습니다.");
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success) throw new InvalidOperationException(path + ": 프리팹 저장에 실패했습니다.");
                return AssetDatabase.LoadAssetAtPath<BattleMap>(path);
            }
            finally
            {
                if (existing) PrefabUtility.UnloadPrefabContents(root);
                else EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static T Ensure<T>(GameObject root) where T : Component => root.GetComponent<T>() ?? root.AddComponent<T>();

        static GameObject Content(GameObject root, MapSources.Entry entry, bool existing)
        {
            if (!existing)
            {
                var content = (GameObject)PrefabUtility.InstantiatePrefab(entry.Source, root.scene);
                content.transform.SetParent(root.transform, true);
                return content;
            }
            string expected = AssetDatabase.GetAssetPath(entry.Source);
            var candidates = new List<GameObject>();
            for (int i = 0; i < root.transform.childCount; i++)
            {
                GameObject child = root.transform.GetChild(i).gameObject;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child);
                string path = AssetDatabase.GetAssetPath(source);
                if (path == expected) return child;
                if (entry.Number == 13 && path == "Assets/KDH/GameModules/Maps/KDH_Map 13.prefab") candidates.Add(child);
            }
            if (candidates.Count != 1)
                throw new InvalidOperationException(root.name + ": 기존 원본 연결을 찾지 못했습니다. 지형을 재생성하지 않습니다.");
            var settings = new PrefabReplacingSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides,
                changeRootNameToAssetName = false,
                logInfo = false
            };
            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(candidates[0], entry.Source, settings, InteractionMode.AutomatedAction);
            return candidates[0];
        }

        static void RemoveLocalObjects(GameObject content)
        {
            foreach (EventSystem system in content.GetComponentsInChildren<EventSystem>(true))
            {
                foreach (BaseInputModule input in system.GetComponents<BaseInputModule>()) Object.DestroyImmediate(input);
                Object.DestroyImmediate(system);
            }
            foreach (Camera camera in content.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(camera);
            foreach (AudioListener listener in content.GetComponentsInChildren<AudioListener>(true)) Object.DestroyImmediate(listener);
        }

        static void Edges(MapRead read, BattleMap map, MapEdges edges)
        {
            MonoBehaviour[] originals = read.Of("KDH_TouchScreenOutline").ToArray();
            if (originals.Length != 4) throw new InvalidOperationException(map.name + ": 원본 경계는 네 개여야 합니다.");
            MapRead.Edit(edges, target =>
            {
                target.FindProperty("_map").objectReferenceValue = map;
                var array = target.FindProperty("_edges");
                array.arraySize = originals.Length;
                for (int i = 0; i < originals.Length; i++)
                {
                    var source = new SerializedObject(originals[i]);
                    var item = array.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("Shape").objectReferenceValue = read.Get<Collider2D>(originals[i]);
                    item.FindPropertyRelative("Damage").floatValue = source.FindProperty("damage").floatValue;
                    item.FindPropertyRelative("Force").vector2Value = source.FindProperty("direction").vector2Value
                        * source.FindProperty("knockbackForce").floatValue;
                    read.Remove(originals[i]);
                }
            });
        }
    }
}
