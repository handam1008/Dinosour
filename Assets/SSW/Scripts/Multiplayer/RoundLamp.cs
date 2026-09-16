using DG.Tweening;
using UnityEngine;

namespace SSW
{
    public sealed class RoundLamp : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Image _fill;
        [SerializeField] UnityEngine.UI.Image _glow;
        [SerializeField] UnityEngine.UI.Image _rim;
        [SerializeField] Color _empty = new Color(0.45f, 0.48f, 0.5f, 0.8f);
        [SerializeField] Color _won = new Color(1f, 0.86f, 0.32f, 1f);
        bool _lit;
        bool _set;
        Sequence _pulse;

        public void Set(bool lit, bool animate)
        {
            if (_set && _lit == lit) return;
            _set = true;
            _lit = lit;
            _pulse?.Kill();
            _rim.color = lit ? _won : _empty;
            _fill.fillAmount = lit ? 1f : 0f;
            _glow.color = new Color(_won.r, _won.g, _won.b, lit ? 0.5f : 0f);
            transform.localScale = Vector3.one;
            if (!lit || !animate) return;
            _fill.fillAmount = 0f;
            _glow.color = new Color(1f, 0.9f, 0.45f, 0f);
            _pulse = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _pulse.Append(_fill.DOFillAmount(1f, 0.28f).SetEase(Ease.OutCubic));
            _pulse.Join(_glow.DOFade(1f, 0.2f));
            _pulse.Join(transform.DOScale(1.18f, 0.2f).SetEase(Ease.OutBack));
            _pulse.Append(_glow.DOFade(0.5f, 0.6f));
            _pulse.Join(transform.DOScale(1f, 0.4f).SetEase(Ease.OutCubic));
        }

        void OnDestroy() => _pulse?.Kill();
    }
}
