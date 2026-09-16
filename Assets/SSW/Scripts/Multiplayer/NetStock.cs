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
    }
}