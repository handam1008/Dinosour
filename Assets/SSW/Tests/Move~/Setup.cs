using System;
using UnityEditor;
using Unity.Netcode.Components;
using UnityEngine;

public static class MotionSetup
{
    public static void Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before prefab setup.");
        const string path = "Assets/SSW/Resources/Network/Player.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            NetworkRigidbody2D rigidbody = root.GetComponent<NetworkRigidbody2D>();
            if (rigidbody != null) UnityEngine.Object.DestroyImmediate(rigidbody);
            NetworkTransform transform = root.GetComponent<NetworkTransform>();
            if (transform != null) UnityEngine.Object.DestroyImmediate(transform);
            Rigidbody2D body = root.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.interpolation = RigidbodyInterpolation2D.None;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (saved.GetComponent<NetworkTransform>() != null || saved.GetComponent<NetworkRigidbody2D>() != null)
            throw new InvalidOperationException("Conflicting transform authority is still attached.");
        Debug.Log("Player prefab uses the shared motion motor; conflicting physics sync removed.");
    }
}
