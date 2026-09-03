using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances
{
    public class KDH_WaterBullet : KDH_AbstractBulletAbility
    {
        [SerializeField] private UnityEvent onHitPlayer;
        
        [SerializeField] private float damage = 10f;
        [SerializeField] private float standardDistance = 1f;
        public KDH_Bullet Bullet { get; private set; }

        private Transform _startPos;
        private Transform _endPos;
        
        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            Bullet = bullet;

            _startPos = Bullet.PlayerGun.transform;
            
            _endPos = collision.gameObject.transform;
            if (bullet.IsUpgraded)
                CalculateDistance(collision, damage * bullet.UpgradValue);
            else
                CalculateDistance(collision, damage);
            
            onHitPlayer?.Invoke();
        }

        private void CalculateDistance(Collider2D collision, float damage)
        {
            Vector3 direction = _endPos.position - _startPos.position;
            float distance = direction.magnitude;
            
            if (collision.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(damage * distance);
        }
    }
}