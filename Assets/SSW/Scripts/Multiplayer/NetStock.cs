using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Network/Potions", fileName = "Potions")]
    public sealed class NetStock : ScriptableObject
    {
        [SerializeField] AbstractPotion[] _potions;
        [SerializeField] int[] _base;
        public int BaseCount => _base.Length;
        public int BaseAt(int index) => _base[index];
        public AbstractPotion At(int index) => _potions[index];
        public int IndexOf(AbstractPotion potion) => System.Array.IndexOf(_potions, potion);

        public int Pick(IReadOnlyList<AbstractPotion> unlocked, float sample)
        {
            float total = 0f;
            foreach (int id in _base) total += Weight(_potions[id]);
            foreach (AbstractPotion potion in unlocked) total += Weight(potion);
            if (total <= 0f) return -1;
            float pick = Mathf.Clamp01(sample) * total;
            int last = -1;
            foreach (int id in _base)
            {
                float weight = Weight(_potions[id]);
                if (weight <= 0f) continue;
                last = id;
                pick -= weight;
                if (pick < 0f) return id;
            }
            foreach (AbstractPotion potion in unlocked)
            {
                float weight = Weight(potion);
                if (weight <= 0f) continue;
                last = IndexOf(potion);
                pick -= weight;
                if (pick < 0f) return last;
            }
            return last;
        }

        static float Weight(AbstractPotion potion) => float.IsFinite(potion.weight) ? Mathf.Max(0f, potion.weight) : 0f;
    }
}
