using System.Collections.Generic;
using KDH.Scripts.Arguments;
using KDH.Scripts.Bullet;
using KDH.Scripts.Gun;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public class KDH_UpgradeList : AugmentReceiverBehaviour
    {
        [field: SerializeField] public List<KDH_AbstractBulletAbility> bulletAbilityList = new();
        [field: SerializeField] public List<KDH_AbstractPlayerAbility> playerAbilityList = new();
        
        private KDH_Gun _gun;
        
        public void GetGun(KDH_Gun gun)
        {
            _gun = gun;
        }
        
        public void ApplyBulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            foreach (var ability in bulletAbilityList)
            {
                ability.BulletAbility(collision, bullet);
            }
            
            foreach (var ability in playerAbilityList)
            {
                ability.PlayerAbility();
            }
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
        {
            bulletAbilityList.Remove(ability.GetComponent<KDH_AbstractBulletAbility>());
        }

        public override PlayerJob Job => PlayerJob.Gunner;
    
        readonly HashSet<GunnerAugmentType> _has = new();
        
        public override bool TryReceive(Augment augment)
        {
            if (augment is not GunnerArgument w) return false;
            _has.Add(w.type);
            return true;
            
            
        }
    
        public bool Has(GunnerAugmentType t) => _has.Contains(t);
        
        // 게임 코드가 읽어갈 값. 없으면 1배(= 효과 없음)
        
        public float SplashMultiplier => Has(GunnerAugmentType.IceBullet) ? 1.3f : 1f;
    }
}