using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Players
{
    public class KDH_BeautifulFootStep : KDH_AbstractPlayerAbility
    {
        [field: SerializeField] public KDH_PlayerAbilitySO PlayerAbilityData { get; private set; }

        [field: SerializeField] public float Duration { get; private set; } = 2.0f;
        [SerializeField] private float speedAmount = 0.2f;
        [SerializeField] private float cooldown = 3.0f;
        [SerializeField] private UnityEvent onHitPlayer;
        private float _timer;
        private bool _canUseAbility;
        
        private void Update()
        {
            if (_canUseAbility) return;
            
            _timer += Time.deltaTime;

            if (_timer >= cooldown)
            {
                _canUseAbility = true;
                _timer = 0;
            }
        }

        public override void PlayerAbility()
        {
            if (!_canUseAbility) return;
            
            if (transform.root.gameObject.TryGetComponent(out ISpeedable speedable))
            {
                speedable.ApplySpeed(speedAmount, Duration);
            }
            
            onHitPlayer?.Invoke();
            _canUseAbility = false;
        }
    }
}