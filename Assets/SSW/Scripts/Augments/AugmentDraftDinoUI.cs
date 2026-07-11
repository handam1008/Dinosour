using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class AugmentDraftDinoUI : AugmentDraftUIBase
    {
        [SerializeField] AugmentCardUI[] _cards;
        [SerializeField] CanvasGroup _group;
        [SerializeField] RectTransform _title;
        [SerializeField] RectTransform _dino;
        [SerializeField] Image _dinoImage;
        [SerializeField] Sprite[] _idleFrames;
        [SerializeField] Sprite[] _moveFrames;
        [SerializeField] float _frameInterval = 0.1f;
        [SerializeField] float _showDuration = 0.25f;
        [SerializeField] float _cardStagger = 0.15f;
        [SerializeField] float _closeDuration = 0.45f;
        [SerializeField] float _footOffset = -14f;
        [SerializeField] float _crushSquashX = 0.82f;
        [SerializeField] float _crushSquashY = 0.15f;
        [SerializeField] float _crushDuration = 0.22f;
        [SerializeField] float _hopHeight = 45f;
        [SerializeField] float _hopDuration = 0.16f;
        [SerializeField] float _jumpDuration = 0.35f;
        [SerializeField] float _stompRise = 300f;
        [SerializeField] float _stompDuration = 0.55f;
        [SerializeField] float _stompSquashX = 1.45f;
        [SerializeField] float _stompSquashY = 0.35f;

        System.Action<Augment> _onSelected;
        Sequence _dinoSeq;
        AugmentCardUI _currentCard;
        float _frameTimer;
        int _frameIndex;
        bool _moving;
        bool _picking;
        float _facing = 1f;

        void Awake()
        {
            EnsureEventSystem();
        }

        void Update()
        {
            Sprite[] frames = _moving ? _moveFrames : _idleFrames;
            if (frames == null || frames.Length == 0 || _dinoImage == null) return;

            _frameTimer += Time.unscaledDeltaTime;
            if (_frameTimer >= _frameInterval)
            {
                _frameTimer -= _frameInterval;
                _frameIndex = (_frameIndex + 1) % frames.Length;
                _dinoImage.sprite = frames[_frameIndex];
            }
        }

        public override void Show(Augment[] choices, System.Action<Augment> onSelected)
        {
            _onSelected = onSelected;
            Time.timeScale = 0f;

            _group.alpha = 0f;
            _group.DOFade(1f, _showDuration).SetUpdate(true);

            if (_title != null)
            {
                Vector2 titlePos = _title.anchoredPosition;
                _title.anchoredPosition = titlePos + new Vector2(0f, 120f);
                _title.DOAnchorPos(titlePos, _showDuration * 1.6f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                Augment choice = choices != null && i < choices.Length ? choices[i] : null;
                _cards[i].Set(choice, HandlePicked);
                _cards[i].PlayDeal(_showDuration + i * _cardStagger);
                _cards[i].HoverEntered += HandleHoverEntered;
                _cards[i].HoverExited += HandleHoverExited;
            }

            Vector2 home = _dino.anchoredPosition;
            _dino.anchoredPosition = home + new Vector2(0f, 900f);
            _dino.DOAnchorPos(home, 0.7f)
                .SetEase(Ease.OutBounce)
                .SetUpdate(true)
                .SetDelay(_showDuration + _cards.Length * _cardStagger + 0.5f)
                .OnComplete(() => StartHopLoop(home));
        }

        Vector2 CardPos(AugmentCardUI card)
        {
            return ((RectTransform)card.transform).anchoredPosition;
        }

        float PerchY(AugmentCardUI card, float cardScale)
        {
            return CardPos(card).y + card.HalfHeight * cardScale + _footOffset;
        }

        void HandleHoverEntered(AugmentCardUI card)
        {
            if (_picking) return;
            _currentCard = card;
            Vector2 target = new Vector2(CardPos(card).x, PerchY(card, card.HoverScale));
            JumpTo(target);
        }

        void HandleHoverExited(AugmentCardUI card)
        {
            if (_picking || _currentCard != card) return;

            Vector2 basePos = new Vector2(CardPos(card).x, PerchY(card, 1f));
            KillDinoSeq();
            _dino.DOKill();
            _dinoSeq = DOTween.Sequence().SetUpdate(true);
            _dinoSeq.Append(_dino.DOAnchorPos(basePos, 0.12f).SetEase(Ease.OutQuad));
            _dinoSeq.AppendCallback(() => StartHopLoop(basePos));
        }

        void JumpTo(Vector2 target)
        {
            KillDinoSeq();
            _dino.DOKill();
            FaceTowards(target.x);
            _moving = true;

            _dinoSeq = DOTween.Sequence().SetUpdate(true);
            _dinoSeq.Append(_dino.DOJumpAnchorPos(target, 130f, 1, _jumpDuration).SetEase(Ease.Linear));
            _dinoSeq.AppendCallback(() =>
            {
                _moving = false;
                StartHopLoop(target);
            });
        }

        void StartHopLoop(Vector2 basePos)
        {
            KillDinoSeq();
            float f = _facing;
            _dinoSeq = DOTween.Sequence().SetUpdate(true).SetLoops(-1);
            _dinoSeq.Append(_dino.DOAnchorPosY(basePos.y + _hopHeight, _hopDuration).SetEase(Ease.OutQuad));
            _dinoSeq.Join(_dino.DOScale(new Vector3(f, 1.06f, 1f), _hopDuration));
            _dinoSeq.Join(_dino.DOLocalRotate(new Vector3(0f, 0f, -f * 6f), _hopDuration));
            _dinoSeq.Append(_dino.DOAnchorPosY(basePos.y, _hopDuration).SetEase(Ease.InQuad));
            _dinoSeq.Join(_dino.DOLocalRotate(Vector3.zero, _hopDuration));
            _dinoSeq.Append(_dino.DOScale(new Vector3(f * 1.05f, 0.92f, 1f), 0.06f));
            _dinoSeq.Append(_dino.DOScale(new Vector3(f, 1f, 1f), 0.08f));
            _dinoSeq.AppendInterval(0.12f);
        }

        void HandlePicked(AugmentCardUI picked)
        {
            if (_picking) return;
            _picking = true;

            foreach (AugmentCardUI card in _cards) card.Lock();

            picked.transform.DOKill();
            picked.transform.localScale = Vector3.one;

            Vector2 cardPos = CardPos(picked);
            float landY = PerchY(picked, 1f);
            Vector2 landPos = new Vector2(cardPos.x, landY);
            float cardBottomY = cardPos.y - picked.HalfHeight;
            float crushedTopY = cardBottomY + picked.HalfHeight * 2f * _crushSquashY + _footOffset;

            KillDinoSeq();
            _dino.DOKill();
            _dino.localRotation = Quaternion.identity;
            FaceTowards(landPos.x);
            _moving = true;

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(_dino.DOAnchorPos(new Vector2(landPos.x, landY + _stompRise), _stompDuration * 0.45f).SetEase(Ease.OutQuad));
            seq.Join(_dino.DOScale(new Vector3(_facing * 0.8f, 1.3f, 1f), _stompDuration * 0.45f));
            seq.AppendInterval(0.06f);
            seq.Append(_dino.DOAnchorPos(landPos, _stompDuration * 0.25f).SetEase(Ease.InQuad));
            seq.AppendCallback(() =>
            {
                _moving = false;
                picked.PlayCrushed(_crushSquashX, _crushSquashY, _crushDuration);
                _dino.localScale = new Vector3(_facing * _stompSquashX, _stompSquashY, 1f);
            });
            seq.Append(_dino.DOAnchorPosY(crushedTopY, _crushDuration).SetEase(Ease.OutCubic));
            seq.Append(_dino.DOScale(new Vector3(_facing, 1f, 1f), 0.22f).SetEase(Ease.OutBack));
            seq.AppendCallback(() =>
            {
                foreach (AugmentCardUI card in _cards)
                {
                    if (card != picked) card.PlayDiscard();
                }
                _group.DOFade(0f, _closeDuration).SetDelay(_closeDuration * 0.6f).SetUpdate(true);
            });
            seq.AppendInterval(_closeDuration * 1.7f);
            seq.AppendCallback(() =>
            {
                Time.timeScale = 1f;
                _onSelected?.Invoke(picked.Augment);
                Destroy(gameObject);
            });
        }

        void FaceTowards(float targetX)
        {
            if (Mathf.Abs(targetX - _dino.anchoredPosition.x) < 1f) return;
            _facing = targetX < _dino.anchoredPosition.x ? -1f : 1f;
            Vector3 scale = _dino.localScale;
            scale.x = Mathf.Abs(scale.x) * _facing;
            _dino.localScale = scale;
        }

        void KillDinoSeq()
        {
            if (_dinoSeq != null && _dinoSeq.IsActive()) _dinoSeq.Kill();
            _dinoSeq = null;
        }
    }
}
