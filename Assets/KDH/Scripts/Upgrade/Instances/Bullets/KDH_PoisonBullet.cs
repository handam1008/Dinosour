using KDH.Scripts.Bullet;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_PoisonBullet : KDH_AbstractBulletAbility
    {
        [Header("Bullet Settings")]
        [SerializeField] private float dotDamage = 7f;
        [field: SerializeField] public int DotCount { get; private set; } = 8;
        [field: SerializeField] public float PoisonDuration { get; private set; } = 4f;
        public float PosionTickInterval { get; private set; }
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }

        public KDH_Bullet Bullet { get; private set; }
        public Transform HitPoint { get; private set; }

        [SerializeField] private UnityEvent onHitPlayer;
    
        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            float finalDamage = bullet.IsUpgraded ? dotDamage * bullet.UpgradValue : dotDamage;

            if (!collision.TryGetComponent(out KDH_PoisonStatus status))
                status = collision.gameObject.AddComponent<KDH_PoisonStatus>();

            status.ApplyPoison(DotCount, PoisonDuration, finalDamage);
            PosionTickInterval = status.TickInterval;
        
            HitPoint = collision.transform;
            Bullet = bullet;

            onHitPlayer?.Invoke();
        }
    }
}