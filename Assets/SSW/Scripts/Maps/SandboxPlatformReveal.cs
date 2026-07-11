using DG.Tweening;
using UnityEngine;

namespace SSW
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SandboxPlatformReveal : MonoBehaviour
    {
        [SerializeField] float _delay;
        [SerializeField] float _duration = 0.48f;

        Vector2 _finalSize;
        SpriteRenderer _renderer;
        Sequence _reveal;

        public void Configure(float delay)
        {
            _delay = delay;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _finalSize = _renderer.size;

            Color color = _renderer.color;
            _renderer.color = new Color(color.r, color.g, color.b, 0f);
            _renderer.size = new Vector2(_finalSize.x * 0.08f, _finalSize.y * 0.08f);
        }

        void Start()
        {
            _reveal = DOTween.Sequence().SetDelay(_delay).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _reveal.Append(DOTween.To(() => _renderer.size, value => _renderer.size = value, _finalSize, _duration).SetEase(Ease.OutBack));
            _reveal.Join(_renderer.DOFade(1f, _duration * 0.65f));
        }

        void OnDisable()
        {
            _reveal?.Kill();
            if (_renderer != null) _renderer.DOKill();
        }
    }
}
