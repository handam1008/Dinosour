using System.Collections.Generic;
using KDH.Scripts.Bullet;
using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public class KDH_UpgradeList : MonoBehaviour
    {
        [SerializeField] private List<KDH_BulletAbilityDataSO> bulletAbilityLibrary = new();
        [field: SerializeField] public List<KDH_AbstractBulletAbility> bulletAbiliytList = new();
        
        public void ApplyBulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            foreach (var ability in bulletAbiliytList)
            {
                ability.BulletAbility(collision, bullet);
            }
        }
        
        public void AddBulletAbility(KDH_BulletAbilityDataSO ability)
        {    
            GameObject instance = Instantiate(ability.BulletAbilityPrefab, transform);
            bulletAbiliytList.Add(instance.GetComponent<KDH_AbstractBulletAbility>());
        }

        public void RemoveBulletAbility(GameObject ability)
        {
            bulletAbiliytList.Remove(ability.GetComponent<KDH_AbstractBulletAbility>());
        }
    }
}