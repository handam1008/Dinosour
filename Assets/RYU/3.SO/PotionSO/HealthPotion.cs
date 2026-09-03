using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthPotion", menuName = "SO/Potion/HealthPotion")]
public class HealthPotion : AbstractPotion
{
    public override void Use(GameObject target, Component source, PotionModifiers mods)
    {
        target.GetComponentInParent<IHealable>()?.Heal(amount * mods.Power);
    }
}
