using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class HealthBarDisplay : MonoBehaviour
    {
        [SerializeField] Image _fillImage;
        [SerializeField] float _tweenDuration = 0.35f;
        [SerializeField] float _shakeDuration = 0.3f;
        [SerializeField] float _shakeStrength = 0.06f;
        [SerializeField] int _shakeVibrato = 12;

        Health _health;

        void Awake()
        {
            _health = GetComponentInParent<Health>();
        }

        void OnEnable()
        {
            _fillImage.DOKill();
            _fillImage.fillAmount = 1f;
            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDamaged += HandleDamaged;
        }

        void OnDisable()
        {
            _fillImage.DOKill();
            transform.DOKill();
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDamaged -= HandleDamaged;
        }

        void HandleHealthChanged(float current, float max)
        {
            _fillImage.DOKill();
            _fillImage.DOFillAmount(current / max, _tweenDuration);
        }

        void HandleDamaged()
        {
            transform.DOKill();
            transform.DOShakePosition(_shakeDuration, _shakeStrength, _shakeVibrato);
        }
    }
}
