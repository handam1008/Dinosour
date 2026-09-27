using DevLib.ServiceLocator;
using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_AirBullet : KDH_AbstractBulletAbility
    {
        [Header("Bullet Settings")] 
        [SerializeField] private float flyPower = 2f;
        private bool _canUseSkill;
        private bool _initPrefab;
        private PlayerController _targetController;
        private Rigidbody2D _targetRbCompo;
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
    
        public Transform HitPoint { get; private set; }
        public KDH_Bullet Bullet { get; private set; }
        [SerializeField] private UnityEvent onHitPlayer;

        private SoundCue applyDamageSound;
        
        private void Update()
        {
            if (_targetController != null)
                _canUseSkill = !_targetController.IsGrounded;
        }
        
        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            if (!_initPrefab)
            {
                _targetController = collision.gameObject.GetComponent<PlayerController>();
                _targetRbCompo = _targetController.GetComponent<Rigidbody2D>();
                _initPrefab = true;
            }

            _targetRbCompo.linearVelocityY = 0;
            
            if (bullet.IsUpgraded)
            {
                Bullet = bullet;
                HitPoint = collision.transform;
                Airborne(collision, flyPower * Bullet.UpgradValue);

                onHitPlayer?.Invoke();
                return;
            }

            Bullet = bullet;
            HitPoint = collision.transform;
            Airborne(collision, flyPower);
            
            onHitPlayer?.Invoke();
        }

        private void Airborne(Collider2D collision, float power)
        {            
            if (!_canUseSkill) return;

            if (collision.TryGetComponent(out IForceReceiver receiver))
            {
                if (applyDamageSound == null)
                    applyDamageSound = Bullet.PlayerGun.SoundCues.list[6];
                
                if (applyDamageSound != null)
                    NetGame.Current.Sounds.Play(applyDamageSound); // 사운드
                
                receiver.ApplyForce(new Vector2(0, power), ForceMode2D.Impulse);
            }
        }
    }
}
