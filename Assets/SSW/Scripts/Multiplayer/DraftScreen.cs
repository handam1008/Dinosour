using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public sealed class DraftScreen : AugmentDraftUIBase
    {
        [SerializeField] RectTransform _screen;
        [SerializeField] CanvasGroup _body;
        [SerializeField] UnityEngine.UI.GraphicRaycaster _raycaster;
        [SerializeField] UnityEngine.UI.Text _title;
        [SerializeField] AugmentCardUI[] _cards;
        [SerializeField] UnityEngine.UI.Image[] _lights;
        [SerializeField] DraftPortrait _portrait;
        [SerializeField] DraftTrail _trail;
        [SerializeField] CardMark[] _marks;
        [SerializeField] DraftFx _fx;
        [SerializeField] Vector2 _space = new Vector2(1920f, 1080f);
        Action<Augment> _selected;
        readonly DraftPointer _pointer = new DraftPointer();
        MatchUI _menu;
        Sequence _motion;
        bool _picking;
        bool _closing;
        float _nextPoint;
        int _hover = -1;

        public bool Spectating { get; private set; }
        public int Hover => _hover;
        public Vector2 Cursor { get; private set; } = -Vector2.one;
        public int Particles => _trail.Particles;
        public bool PortraitOnRight => _portrait.OnRight;
        public event Action<Vector2, int> Pointed;

        void Awake() => EnsureEventSystem();

        public void SetPlayer(PlayerJob job, bool spectator, MatchUI menu)
        {
            _menu = menu;
            Spectating = spectator;
            _body.blocksRaycasts = !spectator;
            _body.interactable = !spectator;
            _raycaster.enabled = !spectator;
            _portrait.Show(job, spectator);
            _title.text = spectator ? "상대의 증강 선택" : "증강을 선택하세요";
            foreach (AugmentCardUI card in _cards) card.ReadOnly = spectator;
        }

        public override void Show(Augment[] choices, Action<Augment> onSelected)
        {
            _selected = onSelected;
            _body.alpha = 0f;
            _body.DOFade(1f, 0.25f).SetUpdate(true);
            _fx.Deal();
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].Set(choices[i], PickedCard);
                _marks[i].Show(choices[i]);
                _marks[i].gameObject.SetActive(choices[i] == null || choices[i].icon == null);
                _cards[i].PlayDeal(0.15f + i * 0.1f);
                _lights[i].color = new Color(0.88f, 0.95f, 1f, 0f);
            }
        }

        void Update()
        {
            if (Spectating || _picking || _closing || Mouse.current == null) return;
            Vector2 pointer = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, pointer, null, out Vector2 point);
            if (!_screen.rect.Contains(point) || _menu.IsOpen)
            {
                _pointer.Dispose();
                Cursor = -Vector2.one;
                _trail.Stop();
                Highlight(-1);
                SendPoint();
                return;
            }
            if (Application.isFocused) _pointer.Hide();
            Cursor = new Vector2(Mathf.Clamp01(point.x / _space.x + 0.5f), Mathf.Clamp01(point.y / _space.y + 0.5f));
            _trail.Move(Vector2.Scale(Cursor - Vector2.one * 0.5f, _space), false);
            int hover = -1;
            for (int i = 0; i < _cards.Length; i++)
                if (!_cards[i].Locked && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_cards[i].transform, pointer))
                { hover = i; break; }
            Highlight(hover);
            SendPoint();
        }

        void SendPoint()
        {
            if (Time.unscaledTime < _nextPoint) return;
            _nextPoint = Time.unscaledTime + 0.05f;
            Pointed?.Invoke(Cursor, _hover);
        }

        public void Apply(DraftPose pose)
        {
            if (!Spectating || _picking || _closing) return;
            Cursor = pose.Cursor;
            if (Cursor.x >= 0f) _trail.Move(Vector2.Scale(Cursor - Vector2.one * 0.5f, _space), true);
            else _trail.Stop();
            Highlight(pose.Hover);
            if (pose.Pick >= 0) AnimatePick(_cards[pose.Pick]);
        }

        void Highlight(int slot)
        {
            if (_hover == slot) return;
            _hover = slot;
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].PreviewHover(i == slot);
                _lights[i].DOKill();
                _lights[i].DOFade(i == slot ? 0.28f : 0f, 0.15f).SetUpdate(true);
            }
            _portrait.Hover(slot);
        }

        void PickedCard(AugmentCardUI card)
        {
            if (Spectating || _picking || _closing) return;
            NotifyPicked(card.Augment);
            AnimatePick(card);
        }

        void AnimatePick(AugmentCardUI picked)
        {
            _picking = true;
            _pointer.Dispose();
            _body.blocksRaycasts = false;
            _trail.Stop();
            _portrait.Pick();
            _fx.Pick((RectTransform)picked.transform);
            foreach (AugmentCardUI card in _cards)
            {
                card.Lock();
                if (card == picked) card.PlayPicked(0.12f, 0.32f);
                else card.PlayDiscard();
            }
            _motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _motion.AppendInterval(0.3f);
            _motion.Append(_body.DOFade(0f, 0.3f));
            _motion.AppendCallback(() => { if (!Spectating) _selected(picked.Augment); });
            _motion.AppendInterval(0.2f);
            _motion.OnComplete(() => Destroy(gameObject));
        }

        public void Close()
        {
            if (_closing) return;
            _closing = true;
            _pointer.Dispose();
            _motion?.Kill();
            _body.DOKill();
            _body.blocksRaycasts = false;
            _trail.Stop();
            _motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _motion.Append(_body.DOFade(0f, 0.25f));
            _motion.AppendInterval(0.5f);
            _motion.OnComplete(() => Destroy(gameObject));
        }

        void OnDestroy()
        {
            _pointer.Dispose();
            _motion?.Kill();
            _body.DOKill();
            foreach (UnityEngine.UI.Image light in _lights) light.DOKill();
        }

        void OnDisable() => _pointer.Dispose();

        void OnApplicationFocus(bool focused)
        {
            if (!focused) _pointer.Dispose();
        }
    }
}
