using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "SlowPotion", menuName = "SO/Potion/SlowPotion")]
public class SlowPotion : AbstractPotion
{
    [SerializeField] private float duration = 2f;
    
    public override void Use(GameObject target, Component source)
    {
        target.GetComponentInParent<ISlowable>()?.ApplySlow(amount, duration);
    }
}
