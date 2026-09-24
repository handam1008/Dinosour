using System;
using System.Collections.Generic;
using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Sound/Bank", fileName = "Sounds")]
    public sealed class SoundBank : ScriptableObject
    {
        [Serializable]
        struct JobSound
        {
            public PlayerJob Job;
            public SoundCue Attack;
            public SoundCue Skill;
            public SoundCue Parry;
            public SoundCue Impact;
        }

        [SerializeField] SoundCue _menu;
        [SerializeField] SoundCue _battle;
        [SerializeField] SoundCue[] _shared = Array.Empty<SoundCue>();
        [SerializeField] JobSound[] _jobs = Array.Empty<JobSound>();
        readonly Dictionary<string, SoundCue> _index = new Dictionary<string, SoundCue>();
        public SoundCue Menu => _menu;
        public SoundCue Battle => _battle;

        public void Prepare()
        {
            _index.Clear();
            Add(_menu);
            Add(_battle);
            foreach (SoundCue cue in _shared) Add(cue);
            foreach (JobSound job in _jobs)
            {
                Add(job.Attack);
                Add(job.Skill);
                Add(job.Parry);
                Add(job.Impact);
            }
        }

        void Add(SoundCue cue)
        {
            if (cue == null) return;
            if (string.IsNullOrWhiteSpace(cue.Id) || System.Text.Encoding.UTF8.GetByteCount(cue.Id) > 61)
                throw new InvalidOperationException($"사운드 ID를 확인해 주세요: {cue.name}");
            if (_index.TryGetValue(cue.Id, out SoundCue previous) && previous != cue)
                throw new InvalidOperationException($"사운드 ID가 겹칩니다: {previous.name}, {cue.name}");
            _index[cue.Id] = cue;
        }

        public bool TryGet(string id, out SoundCue cue) => _index.TryGetValue(id, out cue);
        public bool Contains(SoundCue cue) => cue != null && _index.TryGetValue(cue.Id, out SoundCue found) && found == cue;

        public SoundCue Cast(PlayerJob job, CastKind kind)
        {
            foreach (JobSound item in _jobs)
            {
                if (item.Job != job) continue;
                if (kind == CastKind.Parry) return item.Parry;
                if (kind == CastKind.Cycle) return item.Skill;
                if (kind == CastKind.Press && job != PlayerJob.Magician || kind == CastKind.Release && job == PlayerJob.Magician)
                    return item.Attack;
                return null;
            }
            return null;
        }

        public SoundCue Impact(PlayerJob job)
        {
            foreach (JobSound item in _jobs) if (item.Job == job) return item.Impact;
            return null;
        }
    }
}
