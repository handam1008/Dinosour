using System;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Maps/Sources", fileName = "MapSources")]
    public sealed class MapSources : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int Number;
            public GameObject Source;
            public BattleMap Map;
            public string Hash;
        }

        [SerializeField] NetGame _game;
        [SerializeField] Entry[] _entries = Array.Empty<Entry>();
        public NetGame Game => _game;
        public Entry[] Entries => (Entry[])_entries.Clone();

        public void Replace(Entry[] entries) => _entries = entries;
    }
}
