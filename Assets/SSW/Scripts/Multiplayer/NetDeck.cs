using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Network/Deck", fileName = "Deck")]
    public sealed class NetDeck : ScriptableObject
    {
        [SerializeField] Augment[] _augments;
        public int Count => _augments.Length;
        public Augment At(int index) => _augments[index];

        public List<int> Candidates(PlayerJob job, IEnumerable<int> owned, bool common)
        {
            HashSet<int> held = new HashSet<int>(owned);
            List<int> result = new List<int>();
            for (int i = 0; i < _augments.Length; i++)
            {
                if (held.Contains(i)) continue;
                Augment item = _augments[i];
                bool eligible = common
                    ? item is CommonAugment
                    : item is IJobRestrictedAugment restricted && restricted.RequiredJob == job;
                if (eligible) result.Add(i);
            }
            return result;
        }
    }
}