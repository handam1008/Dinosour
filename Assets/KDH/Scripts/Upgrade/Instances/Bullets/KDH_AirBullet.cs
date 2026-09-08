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
        private PlayerController _targetController;
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
    
        public Transform HitPoint { get; private set; }
        public KDH_Bullet Bullet { get; private set; }
        [SerializeField] private UnityEvent onHitPlayer;

        private void Update()
        {
            if (_targetController != null)
                _canUseSkill = !_targetController.IsGrounded;
        }
        
        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            _targetController = collision.gameObject.GetComponent<PlayerController>();
            
            if (bullet.IsUpgraded)
            {
                Airborne(collision, flyPower * Bullet.UpgradValue);
                HitPoint = collision.transform;
                Bullet = bullet;

                onHitPlayer?.Invoke();
                return;
            }

            Airborne(collision, flyPower);
            HitPoint = collision.transform;
            Bullet = bullet;
            
            onHitPlayer?.Invoke();
        }

        private void Airborne(Collider2D collision, float power)
        {            
            if (!_canUseSkill) return;

            if (collision.TryGetComponent(out IForceReceiver receiver))
            {
                receiver.ApplyForce(new Vector2(0, power), ForceMode2D.Impulse);
                #if UNITY_EDITOR
                Debug.Log("공중에 뜸 상태에서 더 띄우기");
                #endif
            }
        }
    }
}
