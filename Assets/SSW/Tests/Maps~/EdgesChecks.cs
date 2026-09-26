var checks = new System.Collections.Generic.List<string>();
void Check(bool condition, string label)
{
    if (!condition) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var game = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetGame>("Assets/SSW/Resources/Network/NetGame.prefab");
var rotation = (SSW.MapRotation)new UnityEditor.SerializedObject(game).FindProperty("_maps").objectReferenceValue;
var read = typeof(SSW.MapEdges).GetMethod("Read", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
try
{
    var actor = new UnityEngine.GameObject("EdgeProbe");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor, scene);
    var shape = actor.AddComponent<UnityEngine.CapsuleCollider2D>();
    shape.size = new UnityEngine.Vector2(0.6f, 1.2f);
    foreach (var prefab in rotation.Prefabs)
    {
        var map = UnityEngine.Object.Instantiate(prefab);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(map.gameObject, scene);
        try
        {
            var edges = map.GetComponent<SSW.MapEdges>();
            Check(edges != null && new UnityEditor.SerializedObject(map).FindProperty("_edges").objectReferenceValue == edges, prefab.Title + " edge binding");
            Check(map.GetComponentsInChildren<KDH.Scripts.Objects.KDH_TouchScreenOutline>(true).Length == 0, prefab.Title + " no legacy double hit");
            var settings = new UnityEditor.SerializedObject(edges);
            var entries = settings.FindProperty("_edges");
            Check(entries.arraySize == 4, prefab.Title + " four edges");
            var serverOnly = new UnityEditor.SerializedObject(map).FindProperty("_serverOnly");
            for (int i = 0; i < serverOnly.arraySize; i++)
                Check(serverOnly.GetArrayElementAtIndex(i).objectReferenceValue != null, prefab.Title + " live server reference");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var border = (UnityEngine.Collider2D)entry.FindPropertyRelative("Shape").objectReferenceValue;
                float expected = entry.FindPropertyRelative("Damage").floatValue;
                UnityEngine.Vector2 force = entry.FindPropertyRelative("Force").vector2Value;
                Check(border != null && border.enabled && !border.isTrigger && expected > 0f && force.sqrMagnitude > 0f, prefab.Title + " " + i + " configured");
                UnityEngine.Vector2 normal = force.normalized;
                UnityEngine.Physics2D.SyncTransforms();
                var bounds = border.bounds;
                float extent = UnityEngine.Mathf.Abs(normal.x) * (bounds.extents.x + shape.size.x * 0.5f)
                    + UnityEngine.Mathf.Abs(normal.y) * (bounds.extents.y + shape.size.y * 0.5f);
                UnityEngine.Vector2 point = (UnityEngine.Vector2)bounds.center + normal * (extent + 0.016f);
                actor.transform.position = point;
                UnityEngine.Physics2D.SyncTransforms();
                object[] args = { shape, 0u, 0f, UnityEngine.Vector2.zero };
                uint mask = (uint)read.Invoke(edges, args);
                Check(mask == (1u << i) && UnityEngine.Mathf.Approximately((float)args[2], expected), prefab.Title + " " + i + " cast gap damages");
                Check(((UnityEngine.Vector2)args[3] - force).sqrMagnitude < 0.001f, prefab.Title + " " + i + " original bounce");
                args[1] = mask;
                read.Invoke(edges, args);
                Check((float)args[2] == 0f && (UnityEngine.Vector2)args[3] == UnityEngine.Vector2.zero, prefab.Title + " " + i + " held contact once");
                actor.transform.position = point + normal * 0.035f;
                UnityEngine.Physics2D.SyncTransforms();
                Check((uint)read.Invoke(edges, args) == mask && (float)args[2] == 0f, prefab.Title + " " + i + " contact hysteresis");
                actor.transform.position = point + normal * 0.2f;
                UnityEngine.Physics2D.SyncTransforms();
                Check((uint)read.Invoke(edges, args) == 0u, prefab.Title + " " + i + " exit clears");
                actor.transform.position = point;
                UnityEngine.Physics2D.SyncTransforms();
                args[1] = 0u;
                Check((uint)read.Invoke(edges, args) == mask && (float)args[2] == expected, prefab.Title + " " + i + " reentry damages");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }
    return new { passed = checks.Count, maps = rotation.Prefabs.Count };
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
