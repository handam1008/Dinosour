using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "DamagePotion", menuName = "SO/Potion/DamagePotion")]
public class DamagePotion : AbstractPotion
{

    public override void Use(GameObject target, Component source)
    {
        IDamageable hit = target.GetComponentInParent<IDamageable>();
        if (hit != null)
        {
            CombatDamage.Deal(
                source,
                hit,
                amount,
                DamageTag.JobSkill | DamageTag.Projectile);
        }
    }
}
