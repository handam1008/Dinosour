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
        [SerializeField] float _horizontalJitter = 0.2f;
        [SerializeField] Color _normalColor = Color.white;
        [SerializeField] Color _criticalColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] float _criticalScale = 1.4f;

        public void Show(float amount, bool isCritical)
        {
            _text.text = Mathf.RoundToInt(amount).ToString();
            _text.color = isCritical ? _criticalColor : _normalColor;
            transform.localScale *= isCritical ? _criticalScale : 1f;
            Vector3 baseScale = transform.localScale;

            Vector3 start = transform.position + new Vector3(Random.Range(-_horizontalJitter, _horizontalJitter), 0f, 0f);
            transform.position = start;

            transform.DOMoveY(start.y + _riseDistance, _duration).SetEase(Ease.OutCubic);
            transform.DOPunchScale(baseScale * 0.25f, 0.2f, 6, 0.5f);

            Color fadeTarget = _text.color;
            fadeTarget.a = 0f;
            _text.DOColor(fadeTarget, _duration * 0.4f).SetDelay(_duration * 0.6f);

            Destroy(gameObject, _duration + 0.1f);
        }
    }
}
