using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class DamageTick : ITickEffect
    {
        private readonly float _amount;
        private readonly Component _source;

        public DamageTick(float amount, Component source)
        {
            _amount = amount;
            _source = source;
        }
        
        public void Tick(GameObject target)
        {
            
            IDamageable hit = target.GetComponentInParent<IDamageable>();
            if (hit != null)
                CombatDamage.Deal(_source, hit, _amount, DamageTag.JobSkill | DamageTag.DamageOverTime);
        }
    }
}