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
   
        public override void Use(GameObject target, Component source)
        {
            OverTimeRunner runner = target.GetComponentInParent<OverTimeRunner>();
            if (runner != null) runner.Run(new HealTick(amount, source), healCount, healTime);
        }
    }
}
