using System.Collections.Generic;
using KDH.Scripts.Bullet;
using KDH.Scripts.Gun;
using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public class KDH_UpgradeList : MonoBehaviour
    {
        [SerializeField] private List<KDH_BulletAbilityDataSO> bulletAbilityLibrary = new();
        [field: SerializeField] public List<KDH_AbstractBulletAbility> bulletAbiliytList = new();

        private KDH_Gun _gun;
        
        public void GetGun(KDH_Gun gun) // 의존성 역전 원칙 위배
        {
            _gun = gun;
        }
        
        public void ApplyBulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            foreach (var ability in bulletAbiliytList)
            {
                ability.BulletAbility(collision, bullet);
            }
        }

        public void AddBulletAbility(KDH_BulletAbilityDataSO ability)
        {
            GameObject instance = Instantiate(ability.bulletAbilityPrefab, transform);
            bulletAbiliytList.Add(instance.GetComponent<KDH_AbstractBulletAbility>());
            foreach (var bullet in _gun.SpawnBullet.bullets)
                Instantiate(ability.bulletTrailEffectPrefab, bullet.transform);
        }

        public void RemoveBulletAbility(GameObject ability)
        {
            bulletAbiliytList.Remove(ability.GetComponent<KDH_AbstractBulletAbility>());
        }
    }
}