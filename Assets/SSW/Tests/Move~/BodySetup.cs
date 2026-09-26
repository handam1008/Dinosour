if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
string path = "Assets/SSW/Resources/Network/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.PhysicsMaterial2D>("Assets/SSW/Player Physics.physicsMaterial2D");
    var body = root.GetComponent<UnityEngine.Rigidbody2D>();
    var shape = root.GetComponent<UnityEngine.Collider2D>();
    body.sharedMaterial = material;
    shape.sharedMaterial = material;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    return new { body = body.sharedMaterial.name, collider = shape.sharedMaterial.name, material.friction, material.bounciness };
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
