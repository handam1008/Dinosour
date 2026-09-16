using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class AugmentCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] Image _icon;
        [SerializeField] Text _nameText;
        [SerializeField] Text _descriptionText;
        [SerializeField] GameObject _cover;
        [SerializeField] float _hoverScale = 1.08f;
        [SerializeField] float _hoverDuration = 0.15f;
        [SerializeField] float _dealDistance = 900f;
        [SerializeField] float _dealDuration = 0.45f;
        [SerializeField] float _dealTilt = 18f;
        [SerializeField] float _flipDuration = 0.16f;
        [SerializeField] float _discardDistance = 500f;
        [SerializeField] float _discardDuration = 0.4f;

        Augment _augment;
        System.Action<AugmentCardUI> _onClick;
        bool _locked;
        RectTransform _rt;
        Vector2 _slotPos;
        bool _slotCaptured;

        public Augment Augment => _augment;
        public float HoverScale => _hoverScale;
        public float HalfHeight => ((RectTransform)transform).rect.height * 0.5f;
        public event System.Action<AugmentCardUI> HoverEntered;
        public event System.Action<AugmentCardUI> HoverExited;

        public void Set(Augment augment, System.Action<AugmentCardUI> onClick)
        {
            _augment = augment;
            _onClick = onClick;

            if (augment != null)
            {
                _nameText.text = augment.displayName;
                _descriptionText.text = augment.description;
                _icon.enabled = augment.icon != null;
                _icon.sprite = augment.icon;
            }
            else
            {
                _nameText.text = "???";
                _descriptionText.text = "아직 준비되지 않은 증강";
                _icon.enabled = false;
            }
        }

        public void PlayDeal(float delay)
        {
            CaptureSlot();
            _locked = true;
            if (_cover != null) _cover.SetActive(true);

            transform.DOKill();
            transform.localScale = Vector3.one;
            _rt.anchoredPosition = _slotPos + new Vector2(0f, -_dealDistance);
            _rt.localRotation = Quaternion.Euler(0f, 0f, -_dealTilt);

            Sequence seq = DOTween.Sequence().SetTarget(transform).SetLink(gameObject).SetUpdate(true).SetDelay(delay);
            seq.Append(_rt.DOAnchorPos(_slotPos, _dealDuration).SetEase(Ease.OutCubic));
            seq.Join(_rt.DOLocalRotate(Vector3.zero, _dealDuration).SetEase(Ease.OutBack));
            seq.AppendInterval(0.05f);
            seq.Append(transform.DOScaleX(0f, _flipDuration).SetEase(Ease.InQuad));
            seq.AppendCallback(() => { if (_cover != null) _cover.SetActive(false); });
            seq.Append(transform.DOScaleX(1f, _flipDuration).SetEase(Ease.OutQuad));
            seq.Append(transform.DOPunchScale(Vector3.one * 0.06f, 0.15f, 1, 0.5f));
            seq.AppendCallback(() => _locked = false);
        }

        public void PlayPicked(float punch, float duration)
        {
            CaptureSlot();
            _locked = true;
            transform.DOKill();
            _rt.DOAnchorPos(_slotPos + new Vector2(0f, 40f), duration * 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);
            transform.DOPunchScale(Vector3.one * punch, duration, 4, 0.5f).SetUpdate(true);
        }

        public void PlayDiscard()
        {
            CaptureSlot();
            _locked = true;
            transform.DOKill();
            float tilt = _rt.anchoredPosition.x < 0f ? _dealTilt : -_dealTilt;
            if (Mathf.Approximately(_rt.anchoredPosition.x, 0f)) tilt = _dealTilt;
            _rt.DOAnchorPos(_slotPos + new Vector2(0f, -_discardDistance), _discardDuration).SetEase(Ease.InBack).SetUpdate(true);
            _rt.DOLocalRotate(new Vector3(0f, 0f, tilt), _discardDuration).SetUpdate(true);
        }

        public void PlayStomped()
        {
            CaptureSlot();
            _locked = true;
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f).SetUpdate(true);
            _rt.DOShakeAnchorPos(0.35f, 18f, 25, 90f, false, true).SetUpdate(true)
                .OnComplete(() => _rt.anchoredPosition = _slotPos);
        }

        public void PlayCrushed(float squashX, float squashY, float duration)
        {
            CaptureSlot();
            _locked = true;
            transform.DOKill();
            transform.localScale = Vector3.one;
            _rt.pivot = new Vector2(0.5f, 0f);
            _rt.anchoredPosition = new Vector2(_slotPos.x, _slotPos.y - HalfHeight);
            transform.DOScale(new Vector3(squashX, squashY, 1f), duration).SetEase(Ease.OutCubic).SetUpdate(true);
            _rt.DOShakeAnchorPos(duration, new Vector2(12f, 0f), 20, 90f, false, true).SetUpdate(true);
        }

        public void Lock()
        {
            _locked = true;
        }

        void OnDisable()
        {
            transform.DOKill();
        }

        void CaptureSlot()
        {
            if (_slotCaptured) return;
            _rt = (RectTransform)transform;
            _slotPos = _rt.anchoredPosition;
            _slotCaptured = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_locked) return;
            transform.DOKill();
            transform.DOScale(_hoverScale, _hoverDuration).SetUpdate(true);
            HoverEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_locked) return;
            transform.DOKill();
            transform.DOScale(1f, _hoverDuration).SetUpdate(true);
            HoverExited?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_locked) return;
            _onClick?.Invoke(this);
        }
    }
}
