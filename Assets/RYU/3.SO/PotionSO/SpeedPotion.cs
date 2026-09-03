using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedPotion", menuName = "SO/Potion/SpeedPotion")]
public class SpeedPotion : AbstractPotion
{
    [SerializeField] private float duration = 2f;

    public override void Use(GameObject target, Component source, PotionModifiers mods)
    {
        // 신속도 틱이 없으므로 진한 농도를 지속시간에 적용한다
        target.GetComponentInParent<ISpeedable>()?.ApplySpeed(amount * mods.Power, duration * mods.TickCount);
    }
}
