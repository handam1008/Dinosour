using SSW;
using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_DamageCaster : MonoBehaviour
    {
        private KDH_Bullet bullet;
        private float damage;
        private float upgradValue;

        private void Awake()
        {
            bullet = GetComponentInParent<KDH_Bullet>();
        }

        public void Init(KDH_Bullet bulletScript)
        {
            damage = bulletScript.Damage;
            upgradValue =  bulletScript.UpgradValue;
        }
        
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (bullet.IsUpgraded)
            {
                if (collision.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(damage * upgradValue);
            }
            
            if (collision.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(damage);
                
            bullet.PlayerGun.UpgradeList.ApplyBulletAbility(collision, bullet); // 증강에 적용된 총알들의 능력을 모두 적용
        }
    }
}
