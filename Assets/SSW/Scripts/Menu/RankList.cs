using System.Collections.Generic;
using RYU._01.Script.Leaderboard;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace SSW
{
    public sealed class RankList : MonoBehaviour
    {
        static readonly string[] Tiers = { "원숭이급", "공룡급", "마그마급", "메테오급", "빙하기급", "멸종급" };
        [SerializeField] RankUserInfo _prefab;
        [SerializeField] RectTransform _content;
        readonly List<RankUserInfo> _rows = new List<RankUserInfo>();

        public int Count => _rows.Count;

        public void Show(IReadOnlyList<LeaderboardEntry> entries)
        {
            while (_rows.Count > entries.Count)
            {
                int last = _rows.Count - 1;
                RankUserInfo row = _rows[last];
                _rows.RemoveAt(last);
                row.transform.SetParent(null);
                Destroy(row.gameObject);
            }
            for (int i = 0; i < entries.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(_prefab, _content));
                LeaderboardEntry entry = entries[i];
                int tier = RankTier.Index(entry.Rank);
                string name = entry.PlayerName;
                if (!string.IsNullOrWhiteSpace(name)) name = name.Split('#')[0];
                if (string.IsNullOrWhiteSpace(name)) name = entry.PlayerId;
                _rows[i].SetUserInfo(entry.Rank + 1, entry.Score, Tiers[tier], name);
                _rows[i].SetTierImage(_rows[i].tier[tier]);
            }
        }

    }
}
