using System.Collections;
using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

namespace RYU._3.SO.PotionSO
{
    [CreateAssetMenu(fileName = "RegenPotion", menuName = "SO/Potion/RegenPotion")]
    public class RegenPotion : AbstractPotion
    {
        [SerializeField] private int healCount = 2;
        [SerializeField] private float healTime = 0.7f;
   
        public override void Use(GameObject target)
        {
            if(target.TryGetComponent(out OverTimeRunner runner))
                runner.Run(new HealTick(amount), healCount,healTime);
        }
    }
}
