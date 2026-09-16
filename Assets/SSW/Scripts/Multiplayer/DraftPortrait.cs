using DG.Tweening;
using UnityEngine;

namespace SSW
{
    public sealed class DraftPortrait : MonoBehaviour
    {
        [SerializeField] JobCatalog _jobs;
        [SerializeField] MenuDinosaurPreview _preview;
        [SerializeField] RectTransform _pose;
        [SerializeField] CanvasGroup _group;
        bool _right;

        public bool OnRight => _right;

        public void Show(PlayerJob job, bool watching)
        {
            if (!_jobs.TryGet(job, out JobDefinition definition)) throw new System.ArgumentException("직업 정보를 찾을 수 없습니다.");
            _right = watching;
            _pose.anchoredPosition = new Vector2(watching ? 735f : -735f, -65f);
            _group.alpha = 0.28f;
            _preview.Show(definition);
            RectTransform hat = _preview.HatImage.rectTransform;
            Rect sprite = definition.HatSprite.rect;
            hat.sizeDelta = new Vector2(hat.sizeDelta.x, hat.sizeDelta.x * sprite.height / sprite.width);
            _preview.SetFacing(!watching);
        }

        public void Hover(int slot)
        {
            _pose.DOKill();
            _group.DOKill();
            _pose.DOLocalRotate(new Vector3(0f, 0f, slot < 0 ? 0f : _right ? 2f : -2f), 0.2f).SetUpdate(true);
            _group.DOFade(slot < 0 ? 0.28f : 0.38f, 0.2f).SetUpdate(true);
        }

        public void Pick()
        {
            _pose.DOKill();
            _pose.DOPunchScale(Vector3.one * 0.055f, 0.35f, 1, 0.4f).SetUpdate(true);
        }

        void OnDestroy()
        {
            _pose.DOKill();
            _group.DOKill();
        }
    }
}
