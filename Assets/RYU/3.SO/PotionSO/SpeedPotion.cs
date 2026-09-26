using RYU._01.Script.FeedBack;
using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedPotion", menuName = "SO/Potion/SpeedPotion")]
public class SpeedPotion : AbstractPotion
{
    [SerializeField] private float duration = 2f;

    public override void Use(GameObject target, Component source, PotionModifiers mods)
    {
        float time = duration * mods.TickCount;

        target.GetComponentInParent<ISpeedable>()?.ApplySpeed(amount * mods.Power, time);
        SpeedAfterimage.Find(target)?.Play(time);
    }
}
