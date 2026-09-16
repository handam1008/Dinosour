using System.Collections.Generic;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class PotionEffectVisual : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particles;

        private struct Entry
        {
            public Color Color;
            public float EndTime;
        }

        private readonly List<Entry> _active = new List<Entry>();

        public static PotionEffectVisual Find(GameObject target)
        {
            if (target == null) return null;

            PotionEffectVisual visual = target.GetComponentInParent<PotionEffectVisual>();
            if (visual != null) return visual;

            return target.transform.root.GetComponentInChildren<PotionEffectVisual>();
        }

        public void Show(Color color, float duration)
        {
            if (duration <= 0f) return;

            _active.Add(new Entry { Color = color, EndTime = Time.time + duration });
            Refresh();
        }

        private void Update()
        {
            if (_active.Count == 0) return;

            if (_active.RemoveAll(e => Time.time >= e.EndTime) > 0) Refresh();
        }

        private void Refresh()
        {
            if (_particles == null) return;

            if (_active.Count == 0)
            {
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            ParticleSystem.MainModule main = _particles.main;
            main.startColor = Blend();

            if (!_particles.isEmitting) _particles.Play();
        }

        private Color Blend()
        {
            float r = 0f, g = 0f, b = 0f;
            foreach (Entry e in _active)
            {
                r += e.Color.r;
                g += e.Color.g;
                b += e.Color.b;
            }

            int n = _active.Count;
            return new Color(r / n, g / n, b / n, 1f);
        }
    }
}
