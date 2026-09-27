using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_ShurikenBullet : KDH_AbstractBulletAbility
    {
        [Header("Bullet Settings")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float standardDistance = 1f;
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
        
        public KDH_Bullet Bullet { get; private set; }
        private Transform _startPos;
        private Transform _endPos;
        [SerializeField] private UnityEvent onHitPlayer;
        
        public Transform HitPoint { get; private set; }

        private SoundCue _shurikenSound;
        
        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            Bullet = bullet;
            HitPoint = collision.transform;
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
            {
                damageable.TakeDamage(damage * distance);

                // if (_shurikenSound == null)
                //     _shurikenSound = Bullet.PlayerGun.SoundCues.list[10];
                //
                // if (_shurikenSound != null)
                //     NetGame.Current.Sounds.Play(_shurikenSound);
            }
        }
    }
}