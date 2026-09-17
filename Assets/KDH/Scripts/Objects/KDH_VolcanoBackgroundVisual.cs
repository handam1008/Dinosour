using DG.Tweening;

namespace KDH.Scripts.Objects
{
    public class KDH_VolcanoBackgroundVisual : KDH_BackgroundVisual
    {
        public override void FadeColor()
        {
            Sequence seq = DOTween.Sequence();

            seq.Append(_spriteRenderer.DOColor(startColor, 0).SetEase(Ease.OutExpo));
            seq.Append(_spriteRenderer.DOColor(endColor, transitionTime).SetEase(Ease.OutExpo));
        }
    }
}
