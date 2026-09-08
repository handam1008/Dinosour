using System.Collections;
using System.Collections.Generic;
using KDH.Scripts.Gun;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances.Players;
using UnityEngine;

namespace KDH.Scripts.Effects
{
    public class KDH_AfterimageEffectFeedback : KDH_AbstractFeedback
    {
        [SerializeField] private float _spawnInterval = 0.25f;
        [SerializeField] private Color _afterimageColor = new Color(1f, 1f, 1f, 0.5f);
        private Transform _characterRoot;
        private float _fadeDuration;

        private SpriteRenderer[] _renderers;
        private float _timer;
        public bool IsActive { get; set; }

        private void Awake()
        {
            _characterRoot = gameObject.transform.root.gameObject.transform;
            _fadeDuration = GetComponentInParent<KDH_BeautifulFootStep>().Duration;
            _renderers = _characterRoot.GetComponentsInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (!IsActive) return;

            _timer += Time.deltaTime;
            if (_timer >= _spawnInterval)
            {
                _timer = 0f;
                SpawnAfterimage();
            }
        }

        private void SpawnAfterimage()
        {
            GameObject ghostRoot = new GameObject("AfterimageSet");
            ghostRoot.transform.SetPositionAndRotation(_characterRoot.position, _characterRoot.rotation);
            ghostRoot.transform.localScale = _characterRoot.lossyScale;

            List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>();

            foreach (SpriteRenderer sourceSr in _renderers)
            {
                GameObject ghostPart = new GameObject(sourceSr.name + "_Ghost");
                ghostPart.transform.SetParent(ghostRoot.transform, worldPositionStays: true);

                ghostPart.transform.SetPositionAndRotation(sourceSr.transform.position, sourceSr.transform.rotation);
                ghostPart.transform.localScale = sourceSr.transform.lossyScale;

                SpriteRenderer ghostSr = ghostPart.AddComponent<SpriteRenderer>();
                ghostSr.sprite = sourceSr.sprite;
                ghostSr.flipX = sourceSr.flipX;
                ghostSr.flipY = sourceSr.flipY;
                ghostSr.color = _afterimageColor;
                ghostSr.sortingLayerID = sourceSr.sortingLayerID;
                ghostSr.sortingOrder = sourceSr.sortingOrder;

                ghostRenderers.Add(ghostSr);
            }

            StartCoroutine(FadeAndDestroy(ghostRoot, ghostRenderers));
        }

        private IEnumerator FadeAndDestroy(GameObject root, List<SpriteRenderer> renderers)
        {
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _fadeDuration;

                foreach (var sr in renderers)
                {
                    Color c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(_afterimageColor.a, 0f, t));
                }

                yield return null;
            }

            IsActive = false;
            Destroy(root);
        }

        public override void CreateFeedBack()
        {
            IsActive = true;
        }

        public override void StopFeedBack()
        {
            
        }
    }
}