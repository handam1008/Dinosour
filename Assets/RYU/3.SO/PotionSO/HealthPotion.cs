using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthPotion", menuName = "SO/Potion/HealthPotion")]
public class HealthPotion : AbstractPotion
{
    public override void Use(GameObject target, Component source)
    { 
        target.GetComponentInParent<IHealable>()?.Heal(amount);
    }
}
