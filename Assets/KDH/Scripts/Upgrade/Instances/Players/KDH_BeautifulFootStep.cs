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
        [SerializeField] private UnityEvent onHitPlayer;
        public override void PlayerAbility()
        {
            if (transform.root.gameObject.TryGetComponent(out ISpeedable speedable))
            {
                speedable.ApplySpeed(speedAmount, Duration);
            }
            
            onHitPlayer?.Invoke();
        }
    }
}