using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class DamageNumberDisplay : MonoBehaviour
    {
        [SerializeField] Text _text;
        [SerializeField] float _riseDistance = 0.6f;
        [SerializeField] float _duration = 0.8f;
        [SerializeField] float _horizontalJitter = 0.4f;
        [SerializeField] float _verticalJitter = 0.25f;
        [SerializeField] Color _normalColor = Color.white;
        [SerializeField] Color _criticalColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] Color _healColor = new Color(0.25f, 1f, 0.4f);
        [SerializeField] float _criticalScale = 1.4f;

        public void Show(float amount, bool isCritical)
        {
            _text.text = Mathf.RoundToInt(amount).ToString();
            _text.color = isCritical ? _criticalColor : _normalColor;
            transform.localScale *= isCritical ? _criticalScale : 1f;
            Animate();
        }

        public void ShowHeal(float amount)
        {
            _text.text = "+" + amount.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
            _text.color = _healColor;
            Animate();
        }

        void Animate()
        {
            Vector3 baseScale = transform.localScale;

            Vector3 start = transform.position + new Vector3(Random.Range(-_horizontalJitter, _horizontalJitter), Random.Range(-_verticalJitter, _verticalJitter), 0f);
            transform.position = start;

            transform.DOMoveY(start.y + _riseDistance, _duration).SetEase(Ease.OutCubic);
            transform.DOPunchScale(baseScale * 0.25f, 0.2f, 6, 0.5f);

            Color fadeTarget = _text.color;
            fadeTarget.a = 0f;
            _text.DOColor(fadeTarget, _duration * 0.4f).SetDelay(_duration * 0.6f);

            Destroy(gameObject, _duration + 0.1f);
        }

        void OnDestroy()
        {
            transform.DOKill();
            _text.DOKill();
        }
    }
}
