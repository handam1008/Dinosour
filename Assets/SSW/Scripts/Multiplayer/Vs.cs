using System;
using DG.Tweening;
using UnityEngine;

namespace SSW
{
    public sealed class Vs : MonoBehaviour
    {
        [SerializeField] RectTransform _screen;
        [SerializeField] RectTransform _band;
        [SerializeField] RectTransform _mark;
        [SerializeField] CanvasGroup _markGroup;
        [SerializeField] Blur _blur;
        [SerializeField] CanvasGroup _back;
        [SerializeField] VsCard _left;
        [SerializeField] VsCard _right;
        [SerializeField] AudioSource _audio;
        [SerializeField] AudioClip _slide;
        [SerializeField] AudioClip _hit;
        [SerializeField, Min(0.3f)] float _hold = 0.65f;
        Sequence _motion;
        Vector2 _leftHome;
        Vector2 _rightHome;

        public float Duration => 0.75f + _hold;
        public string LeftName => _left.Name;
        public string RightName => _right.Name;
        public bool IsClosing { get; private set; }

        public void SetView(Camera view)
        {
            _blur.Show(view);
        }

        public void Show(NetPlayer local, NetPlayer opponent, Action ready)
        {
            Show(local.Info, local.Job, local.Side > 0 ? 1 : 2,
                opponent.Info, opponent.Job, opponent.Side > 0 ? 1 : 2, ready);
        }

        public void Show(Fighter local, PlayerJob localJob, int localSlot,
            Fighter opponent, PlayerJob opponentJob, int opponentSlot, Action ready)
        {
            _left.Show(local, localJob, true, localSlot);
            _right.Show(opponent, opponentJob, false, opponentSlot);
            Canvas.ForceUpdateCanvases();
            _leftHome = _left.Rect.anchoredPosition;
            _rightHome = _right.Rect.anchoredPosition;
            float width = _screen.rect.width;
            _left.Rect.anchoredPosition = _leftHome + Vector2.left * width;
            _right.Rect.anchoredPosition = _rightHome + Vector2.right * width;
            _band.localScale = new Vector3(1f, 0.002f, 1f);
            _mark.localScale = Vector3.one * 2.4f;
            _mark.localRotation = Quaternion.Euler(0f, 0f, -14f);
            _markGroup.alpha = 0f;
            _back.alpha = 0f;
            _motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _motion.Insert(0f, _back.DOFade(1f, 0.24f));
            _motion.Insert(0.06f, _band.DOScaleY(1f, 0.25f).SetEase(Ease.OutExpo));
            _motion.InsertCallback(0.16f, () => _audio.PlayOneShot(_slide));
            _motion.Insert(0.16f, _left.Rect.DOAnchorPos(_leftHome, 0.48f).SetEase(Ease.OutExpo));
            _motion.Insert(0.20f, _right.Rect.DOAnchorPos(_rightHome, 0.48f).SetEase(Ease.OutExpo));
            _motion.Insert(0.43f, _markGroup.DOFade(1f, 0.08f));
            _motion.Insert(0.43f, _mark.DOScale(1f, 0.25f).SetEase(Ease.OutBack, 1.3f));
            _motion.Insert(0.43f, _mark.DOLocalRotate(Vector3.zero, 0.25f).SetEase(Ease.OutExpo));
            _motion.InsertCallback(0.47f, () => _audio.PlayOneShot(_hit));
            _motion.Insert(0.47f, _band.DOPunchAnchorPos(Vector2.up * 7f, 0.26f, 9, 0.4f));
            _motion.InsertCallback(Duration, () => ready?.Invoke());
        }

        public void Close()
        {
            if (IsClosing) return;
            IsClosing = true;
            _motion.Kill();
            _motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            float width = _screen.rect.width;
            _audio.PlayOneShot(_slide, 0.65f);
            _motion.Insert(0f, _left.Rect.DOAnchorPosX(_leftHome.x - width, 0.25f).SetEase(Ease.InExpo));
            _motion.Insert(0f, _right.Rect.DOAnchorPosX(_rightHome.x + width, 0.25f).SetEase(Ease.InExpo));
            _motion.Insert(0.04f, _mark.DOScale(0.65f, 0.2f).SetEase(Ease.InExpo));
            _motion.Insert(0.04f, _markGroup.DOFade(0f, 0.17f));
            _motion.Insert(0.14f, _band.DOScaleY(0f, 0.23f).SetEase(Ease.InExpo));
            _motion.Insert(0.16f, _back.DOFade(0f, 0.24f).SetEase(Ease.InOutSine));
            _motion.OnComplete(() => Destroy(gameObject));
        }

        void OnDestroy()
        {
            _motion?.Kill();
        }
    }
}