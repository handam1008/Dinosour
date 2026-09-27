using System.Collections;
using DevLib.ServiceLocator;
using KDH.Scripts.Bullet;
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
    
        KDH_Bullet _bullet;
        
        SoundCue dotDamageSound;
        
        public float TickInterval { get; private set; }

        private void Awake()
        {
            TryGetComponent(out _damageable);
        }

        public void ApplyPoison(int dotCount, float duration, float damage, KDH_Bullet bullet)
        {
            if (_bullet == null)
                _bullet = bullet;
            
            TickInterval = duration / dotCount;
            _tickDamage = damage;

            if (_poisonRoutine != null)
            {
                _remainingTicks = Mathf.Max(_remainingTicks, dotCount);
            }
            else
            {
                _remainingTicks = dotCount;
                _poisonRoutine = StartCoroutine(PoisonRoutine(bullet));
            }
        }

        private IEnumerator PoisonRoutine(KDH_Bullet bullet)
        {
            while (_remainingTicks > 0)
            {
                yield return new WaitForSeconds(TickInterval);

                if (_bullet == null)
                    _bullet = bullet;
                    
                if (dotDamageSound == null)
                    dotDamageSound = _bullet.PlayerGun.SoundCues.list[5];
                    
                if (dotDamageSound != null)
                    ServiceLocator.Get<IAudioService>().PlaySfx(dotDamageSound);
                
                _remainingTicks--;
                _damageable?.TakeDamage(_tickDamage);
            }

            _poisonRoutine = null;
        }
    }
}