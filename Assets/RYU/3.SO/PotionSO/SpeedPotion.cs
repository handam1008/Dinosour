using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedPotion", menuName = "SO/Potion/SpeedPotion")]
public class SpeedPotion : AbstractPotion
{
    [SerializeField] private float duration = 2f;
   
    public override void Use(GameObject target, Component source)
    {
        target.GetComponentInParent<ISpeedable>()?.ApplySpeed(amount, duration);
    } 
}
