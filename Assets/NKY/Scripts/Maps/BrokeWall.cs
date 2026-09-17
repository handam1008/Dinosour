using System;
using SSW;
using UnityEngine;

namespace NKY.Scripts.Maps
{
    public class BrokeWall : MonoBehaviour, IDamageable
    {
        public float Current { get; private set; }
        [field: SerializeField] public float Max { get; private set; }

        [SerializeField] private float recorvaryStartTime;
        [SerializeField] private float recorvaryValue;
        [SerializeField] private float recorveryDelay;
        
        private bool isDead = false;
        private float lastRecorvaryTime;
        private float lastHitTime;
        
        private bool IsFighting =>  Time.time > lastHitTime + recorvaryStartTime;
        
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, false);
        }

        public void TakeDamage(float amount, bool isCritical)
        {
            if(isDead) return;
            
            lastHitTime = Time.time;
            Current = Mathf.Clamp(Current - amount, 0, Max);

            if (Current <= 0)
            {

                Broke();
            }
        }

        private void Broke()
        {
            isDead = true;
            Destroy(gameObject);
        }

        private void Update()
        {
            if (!IsFighting && Time.time > lastRecorvaryTime)
            {
                Heal(recorvaryValue);
                lastRecorvaryTime = Time.time + recorveryDelay;
            }
        }

        public void Heal(float amount)
        {
            Current = Mathf.Clamp(Current + amount, 0, Max);
        }
    }
}