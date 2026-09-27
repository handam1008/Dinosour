using RYU._01.Script.Potions;
using UnityEngine;

namespace SSW
{
    internal static class PotionUse
    {
        public static void Apply(AbstractPotion potion, Component target, NetPlayer source, PotionModifiers mods)
        {
            potion.Use(target.gameObject, source, mods);
            if (potion is not SpeedPotion) return;
            NetPlayer player = target.GetComponentInParent<NetPlayer>();
            if (player != null) player.Effects.Trail();
        }
    }
}
