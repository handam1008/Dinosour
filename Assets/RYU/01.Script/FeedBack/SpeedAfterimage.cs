using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RYU._01.Script.FeedBack
{
    public class SpeedAfterimage : MonoBehaviour
    {
        [SerializeField] private float spawnInterval = 0.08f;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Color ghostColor = new Color(0.6f, 1f, 1f, 0.5f);
        [SerializeField] private int sortingOffset = -1;

        private SpriteRenderer[] _renderers;
        private float _until;
        private float _timer;

        public static SpeedAfterimage Find(GameObject target)
        {
            if (target == null) return null;

            SpeedAfterimage found = target.GetComponentInParent<SpeedAfterimage>();
            if (found != null) return found;

            return target.transform.root.GetComponentInChildren<SpeedAfterimage>();
        }

        public void Play(float duration)
        {
            if (duration <= 0f) return;
            _until = Mathf.Max(_until, Time.time + duration);
        }

        private void Awake()
        {
            _renderers = transform.root.GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void Update()
        {
            if (Time.time >= _until) return;

            _timer += Time.deltaTime;
            if (_timer < spawnInterval) return;

            _timer = 0f;
            Spawn();
        }

        private void Spawn()
        {
            List<SpriteRenderer> ghosts = new List<SpriteRenderer>();

            foreach (SpriteRenderer source in _renderers)
            {
                if (source == null || !source.enabled || source.sprite == null) continue;

                GameObject ghost = new GameObject("Afterimage");
                ghost.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                ghost.transform.localScale = source.transform.lossyScale;

                SpriteRenderer view = ghost.AddComponent<SpriteRenderer>();
                view.sprite = source.sprite;
                view.flipX = source.flipX;
                view.flipY = source.flipY;
                view.color = ghostColor;
                view.sortingLayerID = source.sortingLayerID;
                view.sortingOrder = source.sortingOrder + sortingOffset;

                ghosts.Add(view);
            }

            if (ghosts.Count > 0) StartCoroutine(FadeAndDestroy(ghosts));
        }

        private IEnumerator FadeAndDestroy(List<SpriteRenderer> ghosts)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(ghostColor.a, 0f, elapsed / fadeDuration);

                foreach (SpriteRenderer ghost in ghosts)
                {
                    if (ghost == null) continue;
                    Color color = ghost.color;
                    ghost.color = new Color(color.r, color.g, color.b, alpha);
                }

                yield return null;
            }

            foreach (SpriteRenderer ghost in ghosts)
                if (ghost != null) Destroy(ghost.gameObject);
        }
    }
}
