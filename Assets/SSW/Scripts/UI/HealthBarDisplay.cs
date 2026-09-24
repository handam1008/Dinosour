using Cysharp.Threading.Tasks;
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
        
        [Header("BG Health Bar")]
        [SerializeField] Image _bgFillImage;
        [SerializeField] float fillDelay = 0.15f;

        Health _health;
        Tween _shake;

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
            _shake.Kill(true);
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDamaged -= HandleDamaged;
        }

        void HandleHealthChanged(float current, float max)
        {
            _fillImage.DOKill();
            _fillImage.DOFillAmount(current / max, _tweenDuration);
            _ = BgFillAmount(current, max);
        }

        async UniTask BgFillAmount(float current, float max)
        {
            await UniTask.WaitForSeconds(fillDelay);
            _bgFillImage.DOKill();
            _bgFillImage.DOFillAmount(current / max, _tweenDuration);
        }

        void HandleDamaged()
        {
            _shake.Kill(true);
            _shake = transform.DOShakePosition(_shakeDuration, new Vector3(_shakeStrength, _shakeStrength, 0f), _shakeVibrato)
                .OnKill(() => _shake = null);
        }
    }
}
