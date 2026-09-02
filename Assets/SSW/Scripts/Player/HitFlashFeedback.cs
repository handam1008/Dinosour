using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class HitFlashFeedback : MonoBehaviour
    {
        SpriteRenderer[] _renderers;
        Color[] _originalColors;
        Coroutine _routine;

        public static void Play(Component target, Color flashColor, float duration)
        {
            if (target == null || !target.gameObject.activeInHierarchy || duration <= 0f) return;

            HitFlashFeedback feedback = target.GetComponent<HitFlashFeedback>();
            if (feedback == null)
                feedback = target.gameObject.AddComponent<HitFlashFeedback>();

            feedback.PlayInternal(flashColor, duration);
        }

        void PlayInternal(Color flashColor, float duration)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            RestoreColors();

            SpriteRenderer[] found = GetComponentsInChildren<SpriteRenderer>(true);
            List<SpriteRenderer> visible = new List<SpriteRenderer>(found.Length);
            foreach (SpriteRenderer renderer in found)
            {
                if (renderer != null
                    && renderer.enabled
                    && renderer.gameObject.activeInHierarchy
                    && renderer.color.a > 0.01f)
                {
                    visible.Add(renderer);
                }
            }

            _renderers = visible.ToArray();
            _originalColors = new Color[_renderers.Length];
            flashColor.a = 1f;

            for (int i = 0; i < _renderers.Length; i++)
            {
                SpriteRenderer renderer = _renderers[i];
                Color original = renderer.color;
                _originalColors[i] = original;

                Color tinted = Color.Lerp(original, flashColor, 0.82f);
                tinted.a = original.a;
                renderer.color = tinted;
            }

            if (_renderers.Length > 0)
                _routine = StartCoroutine(RestoreAfter(duration));
        }

        IEnumerator RestoreAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            _routine = null;
            RestoreColors();
        }

        void OnDisable()
        {
            RestoreColors();
            _routine = null;
        }

        void OnDestroy()
        {
            RestoreColors();
        }

        void RestoreColors()
        {
            if (_renderers == null || _originalColors == null) return;

            int count = Mathf.Min(_renderers.Length, _originalColors.Length);
            for (int i = 0; i < count; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].color = _originalColors[i];
            }

            _renderers = null;
            _originalColors = null;
        }
    }
}
