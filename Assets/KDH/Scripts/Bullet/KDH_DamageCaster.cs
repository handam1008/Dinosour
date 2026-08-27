using SSW;
using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_DamageCaster : MonoBehaviour
    {
        private KDH_Bullet bullet;
        private float damage;

        private void Awake()
        {
            bullet = GetComponentInParent<KDH_Bullet>();
        }

        public void Init(KDH_Bullet bulletScript)
        {
            damage = bulletScript.Damage;
        }
        
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(damage);
            
            bullet.BulletUpgrade.HitBullet(collision);
        }
    }
}
