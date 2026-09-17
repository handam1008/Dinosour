using DG.Tweening;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_BackgroundVisual : MonoBehaviour
    {
        [SerializeField] protected Color startColor;
        [SerializeField] protected Color endColor;
        [SerializeField] protected float transitionTime;
        [SerializeField] private float rotationTime;
        protected SpriteRenderer _spriteRenderer;

        protected void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected void Start()
        {
            FadeColor();
        }

        public virtual void FadeColor()
        {
            Sequence seq = DOTween.Sequence();

            seq.Append(_spriteRenderer.DOColor(startColor, 0).SetEase(Ease.OutExpo));
            seq.Append(_spriteRenderer.DOColor(endColor, transitionTime).SetEase(Ease.OutExpo));
            seq.AppendInterval(rotationTime);
            seq.SetLoops(-1);
        }
    }
}
