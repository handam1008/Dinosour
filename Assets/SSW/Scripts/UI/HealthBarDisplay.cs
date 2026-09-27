using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    [DefaultExecutionOrder(300)]
    public class HealthBarDisplay : MonoBehaviour
    {
        [SerializeField] Image _fillImage;
        [SerializeField] float _tweenDuration = 0.35f;
        [SerializeField] float _shakeDuration = 0.3f;
        [SerializeField] float _shakeStrength = 0.06f;
        [SerializeField] int _shakeVibrato = 12;
        
        [Header("BG Health Bar")]
        [SerializeField] Image _bgFillImage;
        [SerializeField] float fillDelay = 0.15f;

        Health _health;
        Tween _shake;
        Vector3 _offset;

        void Awake()
        {
            _health = GetComponentInParent<Health>();
        }

        void OnEnable()
        {
            _fillImage.DOKill();
            _bgFillImage.DOKill();
            _fillImage.fillAmount = _health.Current / _health.Max;
            _bgFillImage.fillAmount = _fillImage.fillAmount;
            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDamaged += HandleDamaged;
        }

        void OnDisable()
        {
            _fillImage.DOKill();
            _bgFillImage.DOKill();
            _shake.Kill(true);
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDamaged -= HandleDamaged;
        }

        void HandleHealthChanged(float current, float max)
        {
            _fillImage.DOKill();
            _fillImage.DOFillAmount(current / max, _tweenDuration);
            _bgFillImage.DOKill();
            _bgFillImage.DOFillAmount(current / max, _tweenDuration).SetDelay(fillDelay);
        }

        void HandleDamaged()
        {
            _shake.Kill(true);
            _offset = Vector3.zero;
            _shake = DOTween.Shake(() => _offset, value => _offset = value, _shakeDuration,
                    new Vector3(_shakeStrength, _shakeStrength, 0f), _shakeVibrato)
                .OnKill(() => { _shake = null; _offset = Vector3.zero; });
        }

        void LateUpdate()
        {
            transform.SetPositionAndRotation(_health.LabelPosition + _offset, Quaternion.identity);
        }
    }
}
