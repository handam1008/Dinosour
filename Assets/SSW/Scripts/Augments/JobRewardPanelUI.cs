using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class JobRewardPanelUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image _icon;
        [SerializeField] Text _nameText;
        [SerializeField] Text _descriptionText;
        [SerializeField] float _slideDistance = 420f;
        [SerializeField] float _showDelay = 1.1f;
        [SerializeField] float _duration = 0.4f;
        [Header("Hover Reveal")]
        [SerializeField] Vector2 _expandedSize = new Vector2(520f, 260f);
        [SerializeField] float _expandedScale = 1.04f;
        [SerializeField] float _biteDuration = 0.08f;
        [SerializeField] float _expandDuration = 0.22f;
        [SerializeField] float _settleDuration = 0.09f;
        [SerializeField] float _collapseDuration = 0.16f;
        [SerializeField, Range(0f, 1f)] float _biteHeight = 0.68f;
        [SerializeField] Color _expandedColor = new Color(0.07f, 0.07f, 0.1f, 0.99f);

        public event Action<JobRewardPanelUI> HoverEntered;
        public event Action<JobRewardPanelUI> HoverExited;

        public float BiteDuration => _biteDuration;
        public float PullDuration => _expandDuration + _settleDuration;

        RectTransform _rect;
        RectTransform _labelRect;
        RectTransform _iconRect;
        RectTransform _nameRect;
        RectTransform _descriptionRect;
        Image _background;
        CanvasGroup _descriptionGroup;
        Sequence _visualSequence;
        Augment _augment;
        Vector2 _collapsedSize;
        Vector2 _shownPosition;
        Color _collapsedColor;
        RectLayout _collapsedLabelLayout;
        RectLayout _collapsedIconLayout;
        RectLayout _collapsedNameLayout;
        RectLayout _collapsedDescriptionLayout;
        readonly Vector3[] _worldCorners = new Vector3[4];
        bool _initialized;
        bool _ready;
        bool _hovered;
        bool _locked;
        bool _ownerInteractionEnabled = true;

        void Awake()
        {
            Initialize();
        }

        public void Show(Augment augment)
        {
            gameObject.SetActive(true);
            Initialize();
            KillVisualSequence();

            _augment = augment;
            _hovered = false;
            _locked = false;
            _ready = false;

            if (augment != null)
            {
                _nameText.text = augment.displayName;
                _icon.enabled = augment.icon != null;
                _icon.sprite = augment.icon;
                _descriptionText.text = string.IsNullOrWhiteSpace(augment.description)
                    ? "설명이 아직 없습니다."
                    : augment.description;
            }
            else
            {
                _nameText.text = "???";
                _icon.enabled = false;
                _descriptionText.text = "아직 준비되지 않은 증강입니다.";
            }

            ResetVisuals();
            _rect.anchoredPosition = _shownPosition + new Vector2(_slideDistance, 0f);
            _visualSequence = DOTween.Sequence().SetUpdate(true);
            _visualSequence.AppendInterval(_showDelay);
            _visualSequence.Append(_rect.DOAnchorPos(_shownPosition, _duration).SetEase(Ease.OutBack));
            _visualSequence.OnComplete(() =>
            {
                _visualSequence = null;
                _ready = true;
                RefreshRaycast();
            });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_ready || _locked || _augment == null || _hovered) return;

            _hovered = true;
            if (HoverEntered != null) HoverEntered.Invoke(this);
            else PlayBiteReveal();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_hovered) return;

            _hovered = false;
            if (HoverExited != null) HoverExited.Invoke(this);
            else PlayCollapse();
        }

        public void SetOwnerInteractionEnabled(bool enabled)
        {
            Initialize();
            _ownerInteractionEnabled = enabled;
            RefreshRaycast();
        }

        public void LockAndCollapse()
        {
            Initialize();
            _locked = true;
            _hovered = false;
            RefreshRaycast();
            PlayCollapse();
        }

        public void PlayBiteReveal()
        {
            Initialize();
            KillVisualSequence();

            _visualSequence = DOTween.Sequence().SetUpdate(true);
            _visualSequence.Append(_rect.DOScale(new Vector3(0.92f, 1.08f, 1f), _biteDuration).SetEase(Ease.InQuad));
            _visualSequence.Join(_rect.DOLocalRotate(new Vector3(0f, 0f, 2.5f), _biteDuration).SetEase(Ease.InQuad));
            _visualSequence.Append(_rect.DOSizeDelta(ExpandedSize, _expandDuration).SetEase(Ease.OutBack));
            _visualSequence.Join(_rect.DOScale(Vector3.one * _expandedScale, _expandDuration).SetEase(Ease.OutBack));
            _visualSequence.Join(_rect.DOLocalRotate(new Vector3(0f, 0f, -1.5f), _expandDuration).SetEase(Ease.OutBack));
            _visualSequence.Join(_background.DOColor(_expandedColor, _expandDuration));
            _visualSequence.Join(_descriptionGroup.DOFade(1f, _expandDuration * 0.75f)
                .SetDelay(_expandDuration * 0.25f));
            JoinExpandedContentLayout(_visualSequence, _expandDuration);
            _visualSequence.Append(_rect.DOLocalRotate(Vector3.zero, _settleDuration).SetEase(Ease.OutBack));
            _visualSequence.OnComplete(() => _visualSequence = null);
        }

        public void PlayCollapse()
        {
            Initialize();
            KillVisualSequence();

            _visualSequence = DOTween.Sequence().SetUpdate(true);
            _visualSequence.Append(_descriptionGroup.DOFade(0f, Mathf.Min(0.08f, _collapseDuration)));
            _visualSequence.Join(_rect.DOSizeDelta(_collapsedSize, _collapseDuration).SetEase(Ease.OutCubic));
            _visualSequence.Join(_rect.DOScale(Vector3.one, _collapseDuration).SetEase(Ease.OutCubic));
            _visualSequence.Join(_rect.DOLocalRotate(Vector3.zero, _collapseDuration).SetEase(Ease.OutCubic));
            _visualSequence.Join(_background.DOColor(_collapsedColor, _collapseDuration));
            JoinCollapsedContentLayout(_visualSequence, _collapseDuration);
            _visualSequence.OnComplete(() => _visualSequence = null);
        }

        public Vector2 GetBitePoint(RectTransform targetSpace)
        {
            Initialize();
            _rect.GetWorldCorners(_worldCorners);
            Vector3 bitePoint = Vector3.Lerp(_worldCorners[0], _worldCorners[1], _biteHeight);
            return targetSpace.InverseTransformPoint(bitePoint);
        }

        void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _rect = (RectTransform)transform;
            _background = GetComponent<Image>();
            _collapsedSize = _rect.sizeDelta;
            _shownPosition = _rect.anchoredPosition;
            _collapsedColor = _background != null ? _background.color : Color.white;

            EnsureDescription();
            CacheContentLayout();
            ConfigureText();
            RefreshRaycast();
        }

        void EnsureDescription()
        {
            if (_descriptionText == null)
            {
                GameObject descriptionObject = new GameObject(
                    "Description",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text),
                    typeof(CanvasGroup));
                descriptionObject.layer = gameObject.layer;
                descriptionObject.transform.SetParent(transform, false);

                RectTransform descriptionRect = (RectTransform)descriptionObject.transform;
                descriptionRect.anchorMin = new Vector2(0f, 0f);
                descriptionRect.anchorMax = new Vector2(1f, 0f);
                descriptionRect.pivot = new Vector2(0.5f, 0f);
                descriptionRect.anchoredPosition = new Vector2(0f, 16f);
                descriptionRect.sizeDelta = new Vector2(-44f, 86f);

                _descriptionText = descriptionObject.GetComponent<Text>();
                _descriptionText.font = _nameText != null ? _nameText.font : null;
                _descriptionText.fontSize = 18;
                _descriptionText.resizeTextForBestFit = true;
                _descriptionText.resizeTextMinSize = 14;
                _descriptionText.resizeTextMaxSize = 18;
                _descriptionText.alignment = TextAnchor.UpperLeft;
                _descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
                _descriptionText.lineSpacing = 1.05f;
                _descriptionText.color = new Color(0.82f, 0.82f, 0.88f, 1f);
                _descriptionText.raycastTarget = false;
            }

            _descriptionGroup = _descriptionText.GetComponent<CanvasGroup>();
            if (_descriptionGroup == null) _descriptionGroup = _descriptionText.gameObject.AddComponent<CanvasGroup>();
            _descriptionGroup.alpha = 0f;
            _descriptionGroup.interactable = false;
            _descriptionGroup.blocksRaycasts = false;
        }

        void CacheContentLayout()
        {
            Transform label = transform.Find("Label");
            _labelRect = label != null ? label as RectTransform : null;
            _iconRect = _icon != null ? _icon.rectTransform : null;
            _nameRect = _nameText != null ? _nameText.rectTransform : null;
            _descriptionRect = _descriptionText != null ? _descriptionText.rectTransform : null;

            _collapsedLabelLayout = new RectLayout(_labelRect);
            _collapsedIconLayout = new RectLayout(_iconRect);
            _collapsedNameLayout = new RectLayout(_nameRect);
            _collapsedDescriptionLayout = new RectLayout(_descriptionRect);
        }

        void ConfigureText()
        {
            if (_nameText != null)
            {
                _nameText.alignment = TextAnchor.MiddleLeft;
                _nameText.resizeTextForBestFit = true;
                _nameText.resizeTextMinSize = 18;
                _nameText.resizeTextMaxSize = 28;
                _nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _nameText.verticalOverflow = VerticalWrapMode.Truncate;
            }

            if (_descriptionText == null) return;
            _descriptionText.fontSize = 18;
            _descriptionText.resizeTextForBestFit = true;
            _descriptionText.resizeTextMinSize = 14;
            _descriptionText.resizeTextMaxSize = 18;
            _descriptionText.alignment = TextAnchor.UpperLeft;
            _descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
            _descriptionText.lineSpacing = 1f;
        }

        void ResetVisuals()
        {
            _rect.sizeDelta = _collapsedSize;
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;
            _descriptionGroup.alpha = 0f;
            ApplyLayout(_labelRect, _collapsedLabelLayout);
            ApplyLayout(_iconRect, _collapsedIconLayout);
            ApplyLayout(_nameRect, _collapsedNameLayout);
            ApplyLayout(_descriptionRect, _collapsedDescriptionLayout);
            if (_background != null) _background.color = _collapsedColor;
            RefreshRaycast();
        }

        void JoinExpandedContentLayout(Sequence sequence, float duration)
        {
            Vector2 size = ExpandedSize;
            float centerY = size.y * 0.5f;
            float descriptionHeight = Mathf.Max(76f, size.y - 158f);

            JoinLayout(sequence, _labelRect,
                new RectLayout(new Vector2(0f, centerY - 26f), new Vector2(size.x - 40f, 28f)), duration);
            JoinLayout(sequence, _iconRect,
                new RectLayout(new Vector2(-size.x * 0.5f + 54f, centerY - 88f), new Vector2(64f, 64f)), duration);
            JoinLayout(sequence, _nameRect,
                new RectLayout(new Vector2(38f, centerY - 88f), new Vector2(size.x - 116f, 64f)), duration);
            JoinLayout(sequence, _descriptionRect,
                new RectLayout(new Vector2(0f, 18f), new Vector2(-40f, descriptionHeight)), duration);
        }

        void JoinCollapsedContentLayout(Sequence sequence, float duration)
        {
            JoinLayout(sequence, _labelRect, _collapsedLabelLayout, duration);
            JoinLayout(sequence, _iconRect, _collapsedIconLayout, duration);
            JoinLayout(sequence, _nameRect, _collapsedNameLayout, duration);
            JoinLayout(sequence, _descriptionRect, _collapsedDescriptionLayout, duration);
        }

        static void JoinLayout(Sequence sequence, RectTransform rect, RectLayout layout, float duration)
        {
            if (rect == null) return;
            sequence.Join(rect.DOAnchorPos(layout.Position, duration).SetEase(Ease.OutCubic));
            sequence.Join(rect.DOSizeDelta(layout.Size, duration).SetEase(Ease.OutCubic));
        }

        static void ApplyLayout(RectTransform rect, RectLayout layout)
        {
            if (rect == null) return;
            rect.anchoredPosition = layout.Position;
            rect.sizeDelta = layout.Size;
        }

        void RefreshRaycast()
        {
            if (_background != null)
            {
                _background.raycastTarget = _ready && !_locked && _ownerInteractionEnabled;
            }
        }

        void KillVisualSequence()
        {
            if (_visualSequence != null && _visualSequence.IsActive()) _visualSequence.Kill();
            _visualSequence = null;
            if (_rect != null) _rect.DOKill();
            if (_labelRect != null) _labelRect.DOKill();
            if (_iconRect != null) _iconRect.DOKill();
            if (_nameRect != null) _nameRect.DOKill();
            if (_descriptionRect != null) _descriptionRect.DOKill();
            if (_background != null) _background.DOKill();
            if (_descriptionGroup != null) _descriptionGroup.DOKill();
        }

        Vector2 ExpandedSize
        {
            get
            {
                return new Vector2(
                    Mathf.Max(_expandedSize.x, _collapsedSize.x),
                    Mathf.Max(_expandedSize.y, _collapsedSize.y));
            }
        }

        readonly struct RectLayout
        {
            public readonly Vector2 Position;
            public readonly Vector2 Size;

            public RectLayout(RectTransform rect)
            {
                Position = rect != null ? rect.anchoredPosition : Vector2.zero;
                Size = rect != null ? rect.sizeDelta : Vector2.zero;
            }

            public RectLayout(Vector2 position, Vector2 size)
            {
                Position = position;
                Size = size;
            }
        }

        void OnDestroy()
        {
            KillVisualSequence();
        }
    }
}
