using System;
using KDH.Scripts.Bullet;
using KDH.Scripts.Gun;
using SSW;
using UnityEditor;
using UnityEngine;

public static class BulletSetup
{
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var gun = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KDH/GameModules/KDH_Player Variant.prefab").GetComponent<KDH_Gun>();
        GameObject source = gun.BulletPrefab;
        var bullet = new SerializedObject(source.GetComponent<KDH_Bullet>());
        var normal = (Sprite)bullet.FindProperty("normalBulletSprite").objectReferenceValue;
        var charged = (Sprite)bullet.FindProperty("upgradedBulletSprite").objectReferenceValue;
        var renderer = source.GetComponent<SpriteRenderer>();
        if (normal == null || charged == null || renderer.sharedMaterial == null)
            throw new InvalidOperationException("Bullet presentation references are missing.");
        const string trailPath = "Assets/SSW/Resources/Network/Gun/BulletTrail.prefab";
        var trail = new GameObject("BulletTrail");
        GameObject prefab;
        try
        {
            foreach (Transform child in source.transform)
            {
                if (child.GetComponentInChildren<ParticleSystem>(true) == null) continue;
                GameObject copy = UnityEngine.Object.Instantiate(child.gameObject, trail.transform, false);
                copy.name = child.name;
                foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
                foreach (Collider2D collider in copy.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(collider);
                foreach (Rigidbody2D body in copy.GetComponentsInChildren<Rigidbody2D>(true)) UnityEngine.Object.DestroyImmediate(body);
            }
            if (trail.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                throw new InvalidOperationException("Bullet has no particle effect.");
            foreach (ParticleSystem particles in trail.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                main.stopAction = ParticleSystemStopAction.None;
            }
            prefab = PrefabUtility.SaveAsPrefabAsset(trail, trailPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(trail); }
        const string playerPath = "Assets/SSW/Resources/Network/Player.prefab";
        GameObject player = PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            var effects = new SerializedObject(player.GetComponent<GunFx>());
            effects.FindProperty("_normalBullet").objectReferenceValue = normal;
            effects.FindProperty("_chargedBullet").objectReferenceValue = charged;
            effects.FindProperty("_bulletMaterial").objectReferenceValue = renderer.sharedMaterial;
            effects.FindProperty("_bulletColor").colorValue = renderer.color;
            effects.FindProperty("_bulletTrail").objectReferenceValue = prefab;
            effects.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, playerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        return new { normal = normal.name, charged = charged.name, trail = trailPath };
    }
}
