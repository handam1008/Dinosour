using System.Collections.Generic;
using KDH.Scripts.Bullet;
using KDH.Scripts.Gun;
using KDH.Scripts.Upgrade;
using UnityEngine;

public class KDH_UpgradeList : MonoBehaviour
{
    [field: SerializeField] public List<KDH_AbstractBulletAbility> bulletAbilityList = new();
    [field: SerializeField] public List<KDH_AbstractPlayerAbility> playerAbilityList = new();

    private KDH_Gun _gun;

    public void GetGun(KDH_Gun gun) => _gun = gun;

    public void ApplyBulletAbility(Collider2D collision, KDH_Bullet bullet)
    {
        foreach (var ability in bulletAbilityList)
            ability.BulletAbility(collision, bullet);

        foreach (var ability in playerAbilityList)
            ability.PlayerAbility();
    }

    public void AddBulletAbility(KDH_BulletAbilityDataSO ability)
    {
        GameObject instance = Instantiate(ability.bulletAbilityPrefab, transform);
        bulletAbilityList.Add(instance.GetComponent<KDH_AbstractBulletAbility>());
        foreach (var bullet in _gun.SpawnBullet.bullets)
            Instantiate(ability.bulletTrailEffectPrefab, bullet.transform);
    }

    public void AddPlayerAbility(KDH_PlayerAbilitySO ability)
    {
        GameObject instance = Instantiate(ability.playerAbilityPrefab, transform);
        playerAbilityList.Add(instance.GetComponent<KDH_AbstractPlayerAbility>());
    }

    public void RemoveBulletAbility(GameObject ability)
        => bulletAbilityList.Remove(ability.GetComponent<KDH_AbstractBulletAbility>());
}