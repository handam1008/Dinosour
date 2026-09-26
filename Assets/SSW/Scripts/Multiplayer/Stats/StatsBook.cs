using System;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Game/Stats", fileName = "Stats")]
    public sealed class StatsBook : ScriptableObject
    {
        [SerializeField] FighterStats[] _fighters = Array.Empty<FighterStats>();
        public int Count => _fighters.Length;

        public FighterStats At(PlayerJob job)
        {
            int match = -1;
            for (int i = 0; i < _fighters.Length; i++)
            {
                if (_fighters[i].Job != job) continue;
                if (match >= 0) throw new InvalidOperationException($"{name}: {job} 수치가 중복됐습니다.");
                match = i;
            }
            if (job == PlayerJob.None || match < 0)
                throw new InvalidOperationException($"{name}: {job} 기본 수치가 없습니다.");
            return _fighters[match];
        }

#if UNITY_EDITOR
        public bool Replace(FighterStats[] fighters)
        {
            if (fighters == null) throw new ArgumentNullException(nameof(fighters));
            bool changed = _fighters.Length != fighters.Length;
            for (int i = 0; !changed && i < fighters.Length; i++) changed = !_fighters[i].Equals(fighters[i]);
            if (!changed) return false;
            _fighters = (FighterStats[])fighters.Clone();
            return true;
        }
#endif
    }
}
