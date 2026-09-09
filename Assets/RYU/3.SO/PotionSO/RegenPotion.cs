using RYU._01.Script.Potions;
using UnityEngine;

namespace RYU._3.SO.PotionSO
{
    [CreateAssetMenu(fileName = "RegenPotion", menuName = "SO/Potion/RegenPotion")]
    public class RegenPotion : AbstractPotion
    {
        [SerializeField] private int healCount = 2;
        [SerializeField] private float healTime = 0.7f;

        public override void Use(GameObject target, Component source, PotionModifiers mods)
        {
            OverTimeRunner runner = target.GetComponentInParent<OverTimeRunner>();
            if (runner == null) return;

            // 진한 농도: 틱 간격은 그대로 두고 횟수를 늘린다
            int count = Mathf.Max(1, Mathf.RoundToInt(healCount * mods.TickCount));
            runner.Run(new HealTick(amount * mods.Power, source), count, healTime);

            // 재생이 도는 동안 몸에서 입자가 나오게 한다
            PotionEffectVisual.Find(target)?.Show(potionColor, count * healTime);
        }
    }
}
