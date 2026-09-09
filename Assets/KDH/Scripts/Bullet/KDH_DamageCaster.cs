using KDH.Scripts.Gun;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_DamageCaster : MonoBehaviour
    {
        private KDH_Bullet _bullet;
        private float _damage;
        private float _upgradeValue;

        private void Awake()
        {
            _bullet = GetComponentInParent<KDH_Bullet>();
        }

        public void Init(KDH_Bullet bulletScript)
        {
            _damage = bulletScript.Damage;
            _upgradeValue =  bulletScript.UpgradValue;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.transform.root.TryGetComponent(out KDH_Gun _))
                return;
            
            if (collision.CompareTag("Player"))
                _bullet.PlayerGun.UpgradeList.ApplyBulletAbility(collision, _bullet); // 증강에 적용된 총알들의 능력을 모두 적용
            // _bullet.PlayerGun.

            
            if (collision.TryGetComponent(out IDamageable _))
            {
                if (_bullet.IsUpgraded)
                {
                    if (collision.TryGetComponent<IDamageable>(out var damageable))
                        damageable.TakeDamage(_damage * _upgradeValue);
                    
                    transform.root.gameObject.SetActive(false);
                    _bullet.PlayerGun.SpawnBullet.bullets.Push(transform.root.gameObject);
                    
                    return;
                }
                
                if (collision.TryGetComponent<IDamageable>(out var dmg))
                    dmg.TakeDamage(_damage);
            }
            
            transform.root.gameObject.SetActive(false);
            _bullet.PlayerGun.SpawnBullet.bullets.Push(transform.root.gameObject);
        }
    }
}
