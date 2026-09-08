using System.Collections;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_PoisonStatus : MonoBehaviour
    {
        private Coroutine _poisonRoutine;
        private int _remainingTicks;
        private float _tickDamage;
        private IDamageable _damageable;
    
        public float TickInterval { get; private set; }

        private void Awake()
        {
            TryGetComponent(out _damageable);
        }

        public void ApplyPoison(int dotCount, float duration, float damage)
        {
            TickInterval = duration / dotCount;
            _tickDamage = damage;

            if (_poisonRoutine != null)
            {
                _remainingTicks = Mathf.Max(_remainingTicks, dotCount);
            }
            else
            {
                _remainingTicks = dotCount;
                _poisonRoutine = StartCoroutine(PoisonRoutine());
            }
        }

        private IEnumerator PoisonRoutine()
        {
            while (_remainingTicks > 0)
            {
                yield return new WaitForSeconds(TickInterval);
                _remainingTicks--;
                _damageable?.TakeDamage(_tickDamage);
            }

            _poisonRoutine = null;
        }
    }
}