using System;
using SSW;
using UnityEditor;
using Unity.Netcode.Components;
using UnityEngine;

public static class ShotSetup
{
    public static void Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before setup.");
        foreach (string name in new[] { "Card", "Potion" })
        {
            string path = "Assets/SSW/Resources/Network/" + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                NetworkRigidbody2D oldBody = root.GetComponent<NetworkRigidbody2D>();
                if (oldBody != null) UnityEngine.Object.DestroyImmediate(oldBody);
                NetworkTransform oldSync = root.GetComponent<NetworkTransform>();
                if (oldSync != null) UnityEngine.Object.DestroyImmediate(oldSync);
                ShotSync sync = root.GetComponent<ShotSync>();
                if (sync == null) sync = root.AddComponent<ShotSync>();
                var serial = new SerializedObject(sync);
                serial.FindProperty("_ground").intValue = 1 << 8;
                serial.FindProperty("_body").objectReferenceValue = root.GetComponent<Rigidbody2D>();
                serial.FindProperty("_sprite").objectReferenceValue = root.GetComponent<SpriteRenderer>();
                serial.FindProperty("_feedback").objectReferenceValue = root.GetComponent<MagicianCardFeedback>();
                serial.ApplyModifiedPropertiesWithoutUndo();
                Component cast = name == "Card" ? (Component)root.GetComponent<NetCard>() : root.GetComponent<NetPotion>();
                var owner = new SerializedObject(cast);
                owner.FindProperty("_flight").objectReferenceValue = sync;
                owner.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (saved.GetComponent<ShotSync>() == null || saved.GetComponent<NetworkTransform>() != null || saved.GetComponent<NetworkRigidbody2D>() != null)
                throw new InvalidOperationException(name + " still has conflicting sync.");
        }
        Debug.Log("Card and potion use timestamped flight presentation.");
    }
}
