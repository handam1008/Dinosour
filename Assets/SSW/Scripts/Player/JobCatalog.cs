using System;
using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [Serializable]
    public sealed class JobDefinition
    {
        [SerializeField] PlayerJob _job;
        [SerializeField] string _koreanName;
        [SerializeField] Color _accent;
        [SerializeField] Sprite _hatSprite;
        [SerializeField] Vector2 _previewOffset;
        [SerializeField] float _previewScale = 1f;
        [SerializeField] Vector2 _worldOffset;
        [SerializeField] float _worldScale = 1f;

        public PlayerJob Job => _job;
        public string KoreanName => _koreanName;
        public Color Accent => _accent;
        public Sprite HatSprite => _hatSprite;
        public Vector2 PreviewOffset => _previewOffset;
        public float PreviewScale => _previewScale;
        public Vector2 WorldOffset => _worldOffset;
        public float WorldScale => _worldScale;

        public JobDefinition(PlayerJob job, string koreanName, Color accent)
        {
            _job = job;
            _koreanName = koreanName;
            _accent = accent;
        }
    }

    [CreateAssetMenu(fileName = "JobCatalog", menuName = "SSW/Job Catalog")]
    public sealed class JobCatalog : ScriptableObject
    {
        [SerializeField] JobDefinition[] _definitions =
        {
            new JobDefinition(PlayerJob.Gunner, "총", new Color(0.88f, 0.47f, 0.25f)),
            new JobDefinition(PlayerJob.Witch, "마녀", new Color(0.56f, 0.38f, 0.76f)),
            new JobDefinition(PlayerJob.Magician, "마술사", new Color(0.30f, 0.56f, 0.82f)),
            new JobDefinition(PlayerJob.Gambler, "도박사", new Color(0.78f, 0.25f, 0.31f)),
            new JobDefinition(PlayerJob.Assassin, "암살자", new Color(0.31f, 0.35f, 0.42f)),
            new JobDefinition(PlayerJob.Swordsman, "칼잽이", new Color(0.34f, 0.68f, 0.58f))
        };

        public IReadOnlyList<JobDefinition> Definitions => _definitions;

        public bool TryGet(PlayerJob job, out JobDefinition definition)
        {
            foreach (JobDefinition candidate in _definitions)
            {
                if (candidate.Job != job)
                    continue;

                definition = candidate;
                return true;
            }

            definition = null;
            return false;
        }
    }
}
