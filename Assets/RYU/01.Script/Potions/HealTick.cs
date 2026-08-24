using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class HealTick : ITickEffect
    {
        private readonly float _amount;
        public HealTick(float amount) => _amount = amount;
        
        public void Tick(GameObject target)
        {
           if(target.TryGetComponent(out IDamageable d)) d.Heal(_amount);    
        }
    }
    
    

}