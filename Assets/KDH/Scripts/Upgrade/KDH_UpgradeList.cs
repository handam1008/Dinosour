using System.Collections.Generic;
using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public class KDH_UpgradeList : MonoBehaviour
    {
        [SerializeField] private List<KDH_BulletAbilityDataSO> bulletAbilityLibrary = new();
        [field: SerializeField] public List<KDH_AbstractBulletAbility> bulletAbiliytList = new();

        public void ApplyBulletAbility(Collider2D collision)
        {
            foreach (var ability in bulletAbiliytList)
            {
                ability.BulletAbility(collision);
            }
        }
        
        public void AddBulletAbility(KDH_BulletAbilityDataSO ability)
        {
            bulletAbiliytList.Add(ability.BulletAbilityPrefab.GetComponent<KDH_AbstractBulletAbility>());
        }

        public void RemoveBulletAbility(KDH_BulletAbilityDataSO ability)
        {
            bulletAbiliytList.Remove(ability.BulletAbilityPrefab.GetComponent<KDH_AbstractBulletAbility>());
        }
    }
}