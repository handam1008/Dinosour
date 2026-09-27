using DevLib.ServiceLocator;
using KDH.Scripts.Bullet;
using SSW;
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
    
        SoundCue _applySlowSound;
        
        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            HitPoint = collision.transform;
            Bullet = bullet;
            
            float finalDamage = bullet.IsUpgraded ? dotDamage * bullet.UpgradValue : dotDamage;

            if (!collision.TryGetComponent(out KDH_PoisonStatus status))
                status = collision.gameObject.AddComponent<KDH_PoisonStatus>();

            status.ApplyPoison(DotCount, PoisonDuration, finalDamage, Bullet);
            PosionTickInterval = status.TickInterval;

            if (_applySlowSound == null)
                _applySlowSound = Bullet.PlayerGun.SoundCues.list[9];
            
            if (_applySlowSound != null)
                ServiceLocator.Get<IAudioService>().PlaySfx(_applySlowSound); // 사운드
            
            onHitPlayer?.Invoke();
        }
    }
}