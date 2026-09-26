if (UnityEditor.EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode first");
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/SSW/MainMenu.unity" || scene.isDirty) throw new InvalidOperationException("Open the clean MainMenu scene first");
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SSW/Resources/Network/Player.prefab");
var hatView = source.GetComponentInChildren<SSW.PlayerHatView>(true);
var hat = (SpriteRenderer)new UnityEditor.SerializedObject(hatView).FindProperty("_renderer").objectReferenceValue;
var body = hat.transform.parent.parent.GetComponent<SpriteRenderer>();
var definitions = Resources.Load<SSW.JobCatalog>("JobCatalog").Definitions;
var field = typeof(SSW.MenuDinosaurPreview).GetField("_hatLayout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
var changed = new List<string>();
void Configure(SSW.MenuDinosaurPreview preview)
{
    var layout = field.GetValue(preview);
    layout.GetType().GetMethod("Configure").Invoke(layout, new object[] { body, hat });
    var definition = definitions.FirstOrDefault(d => d.HatSprite == preview.HatImage.sprite);
    if (definition != null) preview.Show(definition);
    UnityEditor.EditorUtility.SetDirty(preview);
    changed.Add(UnityEditor.AnimationUtility.CalculateTransformPath(preview.transform, null));
}
foreach (var root in scene.GetRootGameObjects())
    foreach (var preview in root.GetComponentsInChildren<SSW.MenuDinosaurPreview>(true))
    {
        Configure(preview);
        ((RectTransform)preview.transform).anchoredPosition = new Vector2(0f, -50f);
        preview.DinosaurTransform.sizeDelta = new Vector2(280f, 280f);
    }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
foreach (string path in new[] { "Assets/SSW/Resources/UI/Vs.prefab", "Assets/SSW/Resources/UI/Draft.prefab" })
{
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        foreach (var preview in root.GetComponentsInChildren<SSW.MenuDinosaurPreview>(true))
        {
            Configure(preview);
            if (path.EndsWith("Vs.prefab"))
            {
                ((RectTransform)preview.transform).anchoredPosition = new Vector2(0f, -20f);
                preview.transform.localScale = Vector3.one * 0.38f;
            }
        }
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
}
return changed;
