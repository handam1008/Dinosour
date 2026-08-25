using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "DamagePotion", menuName = "SO/Potion/DamagePotion")]
public class DamagePotion : AbstractPotion
{

    public override void Use(GameObject target)
    {
        if(target.TryGetComponent(out IDamageable d)) d.TakeDamage(amount);
    }
}
