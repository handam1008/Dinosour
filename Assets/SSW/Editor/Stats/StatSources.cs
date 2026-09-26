using System;
using UnityEditor;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Game/Stat Sources", fileName = "StatSources")]
    public sealed class StatSources : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public PlayerJob Job;
            public GameObject Prefab;
            public SceneAsset Scene;
            public string Root;
        }

        [SerializeField] StatsBook _book;
        [SerializeField] NetStock _stock;
        [SerializeField] Entry[] _entries = Array.Empty<Entry>();
        public StatsBook Book => _book;
        public NetStock Stock => _stock;
        public Entry[] Entries => (Entry[])_entries.Clone();
    }
}
