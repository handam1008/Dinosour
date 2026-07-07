using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSetup
{
    [MenuItem("Tools/SSW/Setup Player")]
    static void SetupPlayer()
    {
        string path = "Assets/SSW/Prefabs/Player.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
        if (rb == null) rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (root.GetComponent<Collider2D>() == null)
        {
            CapsuleCollider2D cap = root.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Vertical;
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) cap.size = sr.sprite.bounds.size;
        }

        if (root.GetComponent<Health>() == null) root.AddComponent<Health>();

        PlayerController pc = root.GetComponent<PlayerController>();
        if (pc == null) pc = root.AddComponent<PlayerController>();

        int groundLayer = LayerMask.NameToLayer("Ground");
        SerializedObject pcSo = new SerializedObject(pc);
        SerializedProperty ground = pcSo.FindProperty("_whatIsGround");
        if (ground != null && ground.intValue == 0 && groundLayer >= 0)
            ground.intValue = 1 << groundLayer;
        pcSo.ApplyModifiedProperties();

        PlayerInput input = root.GetComponent<PlayerInput>();
        if (input == null) input = root.AddComponent<PlayerInput>();

        input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        input.defaultActionMap = "Player";
        input.notificationBehavior = PlayerNotifications.SendMessages;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("Player setup complete");
    }
}
