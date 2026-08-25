using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class HealTick : ITickEffect
    {
        private readonly float _amount;
        private readonly Component _source;

        public HealTick(float amount, Component source)
        {
            _amount = amount;
            _source = source;
        }
        
        public void Tick(GameObject target)
        {
            target.GetComponentInParent<IHealable>()?.Heal(_amount);
        }
    }
    
    

}