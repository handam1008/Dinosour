using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "DamagePotion", menuName = "SO/Potion/DamagePotion")]
public class DamagePotion : AbstractPotion
{
    public override void Use(GameObject target, Component source, PotionModifiers mods)
    {
        IDamageable hit = target.GetComponentInParent<IDamageable>();
        if (hit == null) return;

        CombatDamage.Deal(
            source,
            hit,
            amount * mods.Power,
            DamageTag.JobSkill | DamageTag.Projectile);
    }
}
