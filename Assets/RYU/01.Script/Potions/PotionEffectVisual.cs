using System.Collections.Generic;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    // 지속 효과에 걸린 동안 몸에서 포션 색 입자가 나오게 한다. (마크의 포션 효과 파티클)
    // 캐릭터(마녀·적) 루트에 붙이고, 자식 파티클을 _particles 에 연결할 것.
    public class PotionEffectVisual : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particles;

        private struct Entry
        {
            public Color Color;
            public float EndTime;
        }

        // 여러 포션에 동시에 걸릴 수 있으므로 전부 들고 있는다
        private readonly List<Entry> _active = new List<Entry>();

        // 맞은 대상의 몸 어디에 붙어 있든 찾아준다.
        // (루트에 붙어 있어도, 자식 오브젝트에 붙어 있어도 동작)
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

            // 시간이 끝난 효과를 지운다
            if (_active.RemoveAll(e => Time.time >= e.EndTime) > 0) Refresh();
        }

        private void Refresh()
        {
            if (_particles == null) return;

            if (_active.Count == 0)
            {
                // 이미 나온 입자는 자연스럽게 사라지게 두고 방출만 멈춘다
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            ParticleSystem.MainModule main = _particles.main;
            main.startColor = Blend();

            if (!_particles.isEmitting) _particles.Play();
        }

        // 여러 효과가 겹치면 색을 섞는다 (마크도 섞어서 보여준다)
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
