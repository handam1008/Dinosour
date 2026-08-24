using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class DamageTick : ITickEffect
    {
        private readonly float _amount;
        public DamageTick(float amount) => _amount = amount;

        public void Tick(GameObject target)
        {
            if (target.TryGetComponent(out IDamageable d)) d.TakeDamage(_amount);
        }
    }
}