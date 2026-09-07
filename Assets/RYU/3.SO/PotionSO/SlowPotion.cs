using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "SlowPotion", menuName = "SO/Potion/SlowPotion")]
public class SlowPotion : AbstractPotion
{
    [SerializeField] private float duration = 2f;

    public override void Use(GameObject target, Component source, PotionModifiers mods)
    {
        // 구속은 틱이 없으므로 진한 농도를 지속시간에 적용한다
        float time = duration * mods.TickCount;

        target.GetComponentInParent<ISlowable>()?.ApplySlow(amount * mods.Power, time);
        PotionEffectVisual.Find(target)?.Show(potionColor, time);
    }
}
