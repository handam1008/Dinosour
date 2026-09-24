using DG.Tweening;
using UnityEngine;

namespace SSW
{
    public sealed class RoundWipe : MonoBehaviour
    {
        [SerializeField] RectTransform _screen;
        [SerializeField] RectTransform _band;
        [SerializeField] UnityEngine.UI.Text _title;
        [SerializeField] CanvasGroup _words;
        [SerializeField] AudioClip _slide;
        [SerializeField] AudioClip _hit;
        Sequence _motion;
        bool _closing;
        float _width;

        public string Title => _title.text;
        public bool Visible => gameObject.activeSelf;

        public void Show(string title)
        {
            _motion?.Kill();
            gameObject.SetActive(true);
            _closing = true;
            Canvas.ForceUpdateCanvases();
            _width = _screen.rect.width;
            _band.anchoredPosition = Vector2.left * (_width + 400f);
            _title.text = title;
            _title.rectTransform.anchoredPosition = Vector2.left * 95f;
            _title.rectTransform.localScale = Vector3.one * 1.16f;
            _words.alpha = 0f;
            GameAudio.GetOrCreate().PlaySfx(_slide, 0.7f);
            _motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _motion.Insert(0f, _band.DOAnchorPosX(0f, 0.32f).SetEase(Ease.OutExpo));
            _motion.Insert(0.22f, _words.DOFade(1f, 0.12f));
            _motion.Insert(0.22f, _title.rectTransform.DOAnchorPosX(0f, 0.25f).SetEase(Ease.OutCubic));
            _motion.Insert(0.22f, _title.rectTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            _motion.InsertCallback(0.24f, () => GameAudio.GetOrCreate().PlaySfx(_hit, 0.55f));
        }

        public void Reveal()
        {
            if (!_closing) return;
            _closing = false;
            _motion?.Kill();
            _motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _motion.Insert(0f, _words.DOFade(0f, 0.16f));
            _motion.Insert(0f, _title.rectTransform.DOAnchorPosX(90f, 0.18f).SetEase(Ease.InCubic));
            _motion.InsertCallback(0.12f, () => GameAudio.GetOrCreate().PlaySfx(_slide, 0.55f));
            _motion.Insert(0.12f, _band.DOAnchorPosX(_width + 400f, 0.35f).SetEase(Ease.InExpo));
            _motion.OnComplete(Hide);
        }

        public void Hide()
        {
            _closing = false;
            gameObject.SetActive(false);
        }

        void OnDestroy() => _motion?.Kill();
    }
}
