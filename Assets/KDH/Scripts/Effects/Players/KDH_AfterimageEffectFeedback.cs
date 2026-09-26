using System.Collections;
using System.Collections.Generic;
using KDH.Scripts.System.FeedbackSystem;
using KDH.Scripts.Upgrade.Instances.Players;
using UnityEngine;

namespace KDH.Scripts.Effects
{
    public class KDH_AfterimageEffectFeedback : KDH_AbstractFeedback
    {
        [SerializeField] private float _spawnInterval = 0.25f; // 이게 한 틱
        [SerializeField] private Color _afterimageColor = new Color(1f, 1f, 1f, 0.5f); // 색의 투명도 설정
        private Transform _characterRoot; // 최상의 부모
        private float _fadeDuration; // 총 지속시간

        private SpriteRenderer[] _renderers; // 내가 가진 모양을 저장할 배열
        private float _timer; // 타이머
        public bool IsActive { get; set; } // 현재 이펙트가 켜질지 말지 정하는 bool값 (이걸 잘 조절해야 쓸 수 있음)

        private void Awake()
        {
            _characterRoot = gameObject.transform.root.gameObject.transform;
            _fadeDuration = GetComponentInParent<KDH_BeautifulFootStep>().Duration; // 이거는 지속시간 건드는 거라 이렇게 안해도 되고 내가 상수로 정해서 써도 됨
            _renderers = _characterRoot.GetComponentsInChildren<SpriteRenderer>();
        }

        private void Update() // 이펙트 적용되는 부분
        {
            if (!IsActive) return;

            _timer += Time.deltaTime;
            if (_timer >= _spawnInterval)
            {
                _timer = 0f;
                SpawnAfterimage();
            }
        }

        private void SpawnAfterimage() // 이펙트 부분
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

        private IEnumerator FadeAndDestroy(GameObject root, List<SpriteRenderer> renderers) // 이펙트 적용되게 시간으로 나눠서 키게 하기
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

        public override void CreateFeedBack() // <- 이게 아 이게 아마 근데 내 구조가 추상클래스토 된거라 그냥 복사해서 이름 바꿔서 니가 원할 때 bool값을 true 바꾸고 시간 지나면 끝나게 하는 게 맞는 듯?
        {
            IsActive = true;
        }

        public override void StopFeedBack()
        {
            
        }
    }
}