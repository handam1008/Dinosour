using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Network/Deck", fileName = "Deck")]
    public sealed class NetDeck : ScriptableObject
    {
        [SerializeField] Augment[] _augments;
        [SerializeField] AugmentPool _commonPool;
        public bool IsCommon(Augment item) => item != null && System.Array.IndexOf(_commonPool.augments, item) >= 0;
        public int Count => _augments.Length;
        public Augment At(int index) => _augments[index];

        public List<int> Candidates(PlayerJob job, IEnumerable<int> owned, bool common)
        {
            HashSet<int> held = new HashSet<int>(owned);
            HashSet<Augment> blocked = new HashSet<Augment>();
            foreach (int id in held)
            {
                if (id < 0 || id >= _augments.Length || _augments[id] == null) continue;
                if (_augments[id].excludes != null) blocked.UnionWith(_augments[id].excludes);
            }

            List<int> result = new List<int>();
            for (int i = 0; i < _augments.Length; i++)
            {
                if (held.Contains(i)) continue;
                Augment item = _augments[i];
                if (item == null || blocked.Contains(item)) continue;
                bool eligible = common
                    ? IsCommon(item)
                    : item is IJobRestrictedAugment restricted && restricted.RequiredJob == job;
                if (eligible) result.Add(i);
            }
            return result;
        }
    }
}
