using UnityEngine;
using DG.Tweening;

namespace SSW
{
    public class AugmentDraftUI : AugmentDraftUIBase
    {
        [SerializeField] AugmentCardUI[] _cards;
        [SerializeField] CanvasGroup _group;
        [SerializeField] RectTransform _title;
        [SerializeField] float _showDuration = 0.25f;
        [SerializeField] float _cardStagger = 0.15f;
        [SerializeField] float _pickedPunch = 0.15f;
        [SerializeField] float _closeDuration = 0.45f;

        System.Action<Augment> _onSelected;

        void Awake()
        {
            EnsureEventSystem();
        }

        public override void Show(Augment[] choices, System.Action<Augment> onSelected)
        {
            _onSelected = onSelected;
            if (PausesGame) Time.timeScale = 0f;

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
            }
        }

        void HandlePicked(AugmentCardUI picked)
        {
            NotifyPicked(picked.Augment);
            foreach (AugmentCardUI card in _cards)
            {
                card.Lock();
                if (card != picked) card.PlayDiscard();
            }

            picked.PlayPicked(_pickedPunch, _closeDuration);
            _group.DOFade(0f, _closeDuration).SetDelay(_closeDuration).SetUpdate(true);

            DOVirtual.DelayedCall(_closeDuration * 2f, () =>
            {
                if (PausesGame) Time.timeScale = 1f;
                _onSelected?.Invoke(picked.Augment);
                Destroy(gameObject);
            }, true).SetLink(gameObject);
        }

        void OnDestroy()
        {
            _group.DOKill();
            _title.DOKill();
        }
    }
}
