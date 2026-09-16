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
        [SerializeField] Sprite[] _biteFrames;
        [SerializeField] float _frameInterval = 0.1f;
        [SerializeField] float _biteFrameInterval = 0.055f;
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

        [Header("Job Reward Bite")]
        [SerializeField] Vector2 _mouthOffset = new Vector2(43f, 74f);
        [SerializeField] float _rewardApexHeight = 190f;
        [SerializeField] float _rewardAnticipationDuration = 0.07f;
        [SerializeField] float _rewardRiseDuration = 0.17f;
        [SerializeField] float _rewardDiveDuration = 0.18f;
        [SerializeField] float _rewardReturnDuration = 0.3f;
        [SerializeField] float _rewardForwardAngle = 28f;
        [SerializeField] float _rewardBackAngle = 18f;

        System.Action<Augment> _onSelected;
        Sequence _dinoSeq;
        AugmentCardUI _currentCard;
        Vector2 _homePosition;
        float _frameTimer;
        int _frameIndex;
        int _dinoOriginalSiblingIndex;
        int _motionVersion;
        bool _moving;
        bool _biting;
        bool _frameLocked;
        bool _picking;
        bool _dinoReady;
        bool _rewardHovered;
        float _facing = 1f;

        void Awake()
        {
            EnsureEventSystem();
        }

        void Update()
        {
            Sprite[] frames = CurrentFrames();
            if (_frameLocked || frames == null || frames.Length == 0 || _dinoImage == null) return;

            _frameTimer += Time.unscaledDeltaTime;
            float interval = _biting ? _biteFrameInterval : _frameInterval;
            if (_frameTimer < interval) return;

            _frameTimer -= interval;
            _frameIndex = (_frameIndex + 1) % frames.Length;
            _dinoImage.sprite = frames[_frameIndex];
        }

        public override void Show(Augment[] choices, System.Action<Augment> onSelected)
        {
            _onSelected = onSelected;
            _picking = false;
            _rewardHovered = false;
            _dinoReady = false;
            if (PausesGame) Time.timeScale = 0f;

            if (JobRewardPanel != null)
            {
                JobRewardPanel.HoverEntered -= HandleJobRewardHoverEntered;
                JobRewardPanel.HoverExited -= HandleJobRewardHoverExited;
                JobRewardPanel.HoverEntered += HandleJobRewardHoverEntered;
                JobRewardPanel.HoverExited += HandleJobRewardHoverExited;
                JobRewardPanel.SetOwnerInteractionEnabled(false);
            }

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

            _homePosition = _dino.anchoredPosition;
            _dinoOriginalSiblingIndex = _dino.GetSiblingIndex();
            SetPose(false, false);
            KillDinoSeq();
            _dino.DOKill();
            _dino.anchoredPosition = _homePosition + new Vector2(0f, 900f);

            _dinoSeq = DOTween.Sequence().SetUpdate(true);
            _dinoSeq.AppendInterval(_showDuration + _cards.Length * _cardStagger + 0.5f);
            _dinoSeq.Append(_dino.DOAnchorPos(_homePosition, 0.7f).SetEase(Ease.OutBounce));
            _dinoSeq.AppendCallback(() =>
            {
                MarkDinoReady();
                StartHopLoop(_homePosition);
            });
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

            StopRewardMotion(true);
            _currentCard = card;
            Vector2 target = new Vector2(CardPos(card).x, PerchY(card, card.HoverScale));
            JumpTo(target);
        }

        void HandleHoverExited(AugmentCardUI card)
        {
            if (_picking || _currentCard != card) return;

            _currentCard = null;
            ++_motionVersion;
            Vector2 basePos = new Vector2(CardPos(card).x, PerchY(card, 1f));
            KillDinoSeq();
            _dino.DOKill();
            SetPose(true, false);
            _dinoSeq = DOTween.Sequence().SetUpdate(true);
            _dinoSeq.Append(_dino.DOAnchorPos(basePos, 0.12f).SetEase(Ease.OutQuad));
            _dinoSeq.Join(_dino.DOLocalRotate(Vector3.zero, 0.12f));
            _dinoSeq.AppendCallback(() =>
            {
                MarkDinoReady();
                StartHopLoop(basePos);
            });
        }

        void JumpTo(Vector2 target)
        {
            ++_motionVersion;
            KillDinoSeq();
            _dino.DOKill();
            RestoreDinoSibling();
            FaceTowards(target.x);
            SetPose(true, false);

            _dinoSeq = DOTween.Sequence().SetUpdate(true);
            _dinoSeq.Append(_dino.DOJumpAnchorPos(target, 130f, 1, _jumpDuration).SetEase(Ease.Linear));
            _dinoSeq.Join(_dino.DOLocalRotate(Vector3.zero, _jumpDuration * 0.5f));
            _dinoSeq.AppendCallback(() =>
            {
                MarkDinoReady();
                StartHopLoop(target);
            });
        }

        void StartHopLoop(Vector2 basePos)
        {
            if (_picking || _rewardHovered) return;

            KillDinoSeq();
            SetPose(false, false);
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

        void HandleJobRewardHoverEntered(JobRewardPanelUI rewardPanel)
        {
            if (_picking || !_dinoReady || rewardPanel == null) return;

            _currentCard = null;
            _rewardHovered = true;
            int version = ++_motionVersion;
            KillDinoSeq();
            _dino.DOKill();
            _dino.SetAsLastSibling();

            Vector2 bitePoint = RewardBitePointInDinoAnchors(rewardPanel);
            FaceTowards(bitePoint.x);
            SetPose(true, false);

            float forwardAngle = -_facing * _rewardForwardAngle;
            Vector2 biteTarget = DinoPivotForMouth(rewardPanel, 1.04f, 0.96f, forwardAngle);
            Vector2 start = _dino.anchoredPosition;
            Vector2 apex = Vector2.Lerp(start, biteTarget, 0.42f) + Vector2.up * _rewardApexHeight;

            _dinoSeq = DOTween.Sequence().SetUpdate(true);

            _dinoSeq.Append(_dino.DOScale(new Vector3(_facing * 1.18f, 0.72f, 1f), _rewardAnticipationDuration)
                .SetEase(Ease.InQuad));
            _dinoSeq.Join(_dino.DOLocalRotate(new Vector3(0f, 0f, _facing * 5f), _rewardAnticipationDuration));
            _dinoSeq.Append(_dino.DOAnchorPos(apex, _rewardRiseDuration).SetEase(Ease.OutCubic));
            _dinoSeq.Join(_dino.DOScale(new Vector3(_facing * 0.86f, 1.24f, 1f), _rewardRiseDuration));
            _dinoSeq.Join(_dino.DOLocalRotate(new Vector3(0f, 0f, -_facing * 12f), _rewardRiseDuration));
            _dinoSeq.Append(_dino.DOAnchorPos(biteTarget, _rewardDiveDuration).SetEase(Ease.InCubic));
            _dinoSeq.Join(_dino.DOScale(new Vector3(_facing * 1.04f, 0.96f, 1f), _rewardDiveDuration));
            _dinoSeq.Join(_dino.DOLocalRotate(new Vector3(0f, 0f, forwardAngle), _rewardDiveDuration)
                .SetEase(Ease.InBack));

            _dinoSeq.AppendCallback(() =>
            {
                if (!IsCurrentRewardMotion(version)) return;
                PinMouthToReward(rewardPanel);
                SetPose(false, true);
                rewardPanel.PlayBiteReveal();
            });
            _dinoSeq.Append(_dino.DOScale(new Vector3(_facing * 1.22f, 0.72f, 1f), rewardPanel.BiteDuration * 0.45f)
                .SetEase(Ease.InQuad));
            _dinoSeq.Join(_dino.DOPunchAnchorPos(new Vector2(_facing * 12f, 0f), rewardPanel.BiteDuration, 1, 0f));
            _dinoSeq.Append(_dino.DOScale(new Vector3(_facing * 0.96f, 1.1f, 1f), rewardPanel.BiteDuration * 0.55f)
                .SetEase(Ease.OutBack));
            _dinoSeq.AppendCallback(() =>
            {
                if (IsCurrentRewardMotion(version)) SetPose(false, true, true);
            });
            _dinoSeq.Append(DOVirtual.Float(0f, 1f, rewardPanel.PullDuration, _ =>
            {
                if (IsCurrentRewardMotion(version)) PinMouthToReward(rewardPanel);
            }).SetEase(Ease.OutBack));
            _dinoSeq.Join(_dino.DOLocalRotate(new Vector3(0f, 0f, _facing * _rewardBackAngle),
                rewardPanel.PullDuration * 0.7f).SetEase(Ease.OutBack));
            _dinoSeq.Join(_dino.DOScale(new Vector3(_facing * 1.08f, 1.04f, 1f),
                rewardPanel.PullDuration * 0.75f).SetEase(Ease.OutBack));
            _dinoSeq.AppendCallback(() =>
            {
                if (!IsCurrentRewardMotion(version)) return;
                PinMouthToReward(rewardPanel);
                SetPose(false, true, true);
            });
        }

        void HandleJobRewardHoverExited(JobRewardPanelUI rewardPanel)
        {
            if (_picking || !_rewardHovered) return;

            _rewardHovered = false;
            int version = ++_motionVersion;
            rewardPanel.PlayCollapse();
            KillDinoSeq();
            _dino.DOKill();
            SetPose(true, false);

            _dinoSeq = DOTween.Sequence().SetUpdate(true);
            _dinoSeq.Append(_dino.DOLocalRotate(new Vector3(0f, 0f, _facing * (_rewardBackAngle + 8f)), 0.06f)
                .SetEase(Ease.OutBack));
            _dinoSeq.Join(_dino.DOScale(new Vector3(_facing * 0.9f, 1.12f, 1f), 0.06f));
            _dinoSeq.Append(_dino.DOJumpAnchorPos(_homePosition, 110f, 1, _rewardReturnDuration)
                .SetEase(Ease.InOutQuad));
            _dinoSeq.Join(_dino.DOLocalRotate(Vector3.zero, _rewardReturnDuration).SetEase(Ease.OutCubic));
            _dinoSeq.Join(_dino.DOScale(new Vector3(_facing, 1f, 1f), _rewardReturnDuration)
                .SetEase(Ease.OutCubic));
            _dinoSeq.AppendCallback(() =>
            {
                if (_motionVersion != version || _picking || _rewardHovered) return;
                RestoreDinoSibling();
                MarkDinoReady();
                StartHopLoop(_homePosition);
            });
        }

        void HandlePicked(AugmentCardUI picked)
        {
            if (_picking) return;
            _picking = true;
            _rewardHovered = false;
            ++_motionVersion;

            JobRewardPanel?.LockAndCollapse();
            RestoreDinoSibling();
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
            SetPose(true, false);

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(_dino.DOAnchorPos(new Vector2(landPos.x, landY + _stompRise), _stompDuration * 0.45f).SetEase(Ease.OutQuad));
            seq.Join(_dino.DOScale(new Vector3(_facing * 0.8f, 1.3f, 1f), _stompDuration * 0.45f));
            seq.AppendInterval(0.06f);
            seq.Append(_dino.DOAnchorPos(landPos, _stompDuration * 0.25f).SetEase(Ease.InQuad));
            seq.AppendCallback(() =>
            {
                SetPose(false, false);
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
                if (PausesGame) Time.timeScale = 1f;
                _onSelected?.Invoke(picked.Augment);
                Destroy(gameObject);
            });
        }

        void StopRewardMotion(bool collapsePanel)
        {
            if (!_rewardHovered) return;
            _rewardHovered = false;
            ++_motionVersion;
            if (collapsePanel) JobRewardPanel?.PlayCollapse();
        }

        bool IsCurrentRewardMotion(int version)
        {
            return !_picking && _rewardHovered && _motionVersion == version;
        }

        Vector2 RewardBitePointInDinoAnchors(JobRewardPanelUI rewardPanel)
        {
            RectTransform parent = (RectTransform)_dino.parent;
            Vector2 biteInParent = rewardPanel.GetBitePoint(parent);
            Vector2 dinoPivotInParent = parent.InverseTransformPoint(_dino.position);
            return _dino.anchoredPosition + biteInParent - dinoPivotInParent;
        }

        Vector2 DinoPivotForMouth(JobRewardPanelUI rewardPanel, float scaleX, float scaleY, float angle)
        {
            return RewardBitePointInDinoAnchors(rewardPanel) - MouthOffsetForPose(scaleX, scaleY, angle);
        }

        void PinMouthToReward(JobRewardPanelUI rewardPanel)
        {
            float angle = _dino.localEulerAngles.z;
            if (angle > 180f) angle -= 360f;
            Vector3 scale = _dino.localScale;
            Vector2 mouthOffset = MouthOffsetForPose(Mathf.Abs(scale.x), scale.y, angle);
            _dino.anchoredPosition = RewardBitePointInDinoAnchors(rewardPanel) - mouthOffset;
        }

        Vector2 MouthOffsetForPose(float scaleX, float scaleY, float angle)
        {
            Vector2 offset = new Vector2(_mouthOffset.x * _facing * scaleX, _mouthOffset.y * scaleY);
            float radians = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(
                offset.x * cos - offset.y * sin,
                offset.x * sin + offset.y * cos);
        }

        void MarkDinoReady()
        {
            _dinoReady = true;
            JobRewardPanel?.SetOwnerInteractionEnabled(true);
        }

        void FaceTowards(float targetX)
        {
            if (Mathf.Abs(targetX - _dino.anchoredPosition.x) < 1f) return;
            _facing = targetX < _dino.anchoredPosition.x ? -1f : 1f;
            Vector3 scale = _dino.localScale;
            scale.x = Mathf.Abs(scale.x) * _facing;
            _dino.localScale = scale;
        }

        void SetPose(bool moving, bool biting, bool lockLastFrame = false)
        {
            _moving = moving;
            _biting = biting && _biteFrames != null && _biteFrames.Length > 0;
            _frameLocked = lockLastFrame && _biting;
            _frameTimer = 0f;

            Sprite[] frames = CurrentFrames();
            if (frames == null || frames.Length == 0 || _dinoImage == null) return;
            _frameIndex = _frameLocked ? frames.Length - 1 : 0;
            _dinoImage.sprite = frames[_frameIndex];
        }

        Sprite[] CurrentFrames()
        {
            if (_biting && _biteFrames != null && _biteFrames.Length > 0) return _biteFrames;
            return _moving ? _moveFrames : _idleFrames;
        }

        void RestoreDinoSibling()
        {
            if (_dino == null || _dino.parent == null) return;
            int maxIndex = _dino.parent.childCount - 1;
            _dino.SetSiblingIndex(Mathf.Clamp(_dinoOriginalSiblingIndex, 0, maxIndex));
        }

        void KillDinoSeq()
        {
            if (_dinoSeq != null && _dinoSeq.IsActive()) _dinoSeq.Kill();
            _dinoSeq = null;
        }

        void OnDestroy()
        {
            KillDinoSeq();
            if (_dino != null) _dino.DOKill();
            if (JobRewardPanel == null) return;
            JobRewardPanel.HoverEntered -= HandleJobRewardHoverEntered;
            JobRewardPanel.HoverExited -= HandleJobRewardHoverExited;
        }
    }
}
