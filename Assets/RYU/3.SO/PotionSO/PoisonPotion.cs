using RYU._01.Script.Potions;
using UnityEngine;

namespace RYU._3.SO.PotionSO
{
    [CreateAssetMenu(fileName = "PoisonPotion", menuName = "SO/Potion/PoisonPotion")]
    public class PoisonPotion : AbstractPotion
    {
        [SerializeField] private int damageCount = 2;
        [SerializeField] private float damageTime = 0.7f;

        public override void Use(GameObject target, Component source, PotionModifiers mods)
        {
            OverTimeRunner runner = target.GetComponentInParent<OverTimeRunner>();
            if (runner == null) return;

            int count = Mathf.Max(1, Mathf.RoundToInt(damageCount * mods.TickCount));
            runner.Run(new DamageTick(amount * mods.Power, source), count, damageTime);

            PotionEffectVisual.Find(target)?.Show(potionColor, count * damageTime);
        }
    }
}
