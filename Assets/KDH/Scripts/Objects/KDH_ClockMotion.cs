using DG.Tweening;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_ClockMotion : MonoBehaviour
    {
        private SpriteRenderer SRCompo;
        
        [SerializeField] private float minSize;
        [SerializeField] private float maxSize;

        void Awake()
        {
            SRCompo = GetComponent<SpriteRenderer>();
        }
        
        public void ClockMotion()
        {
            Sequence seq = DOTween.Sequence();

            seq.Append(SRCompo.DOFade(0.2f, 0)).SetUpdate(true);
            seq.Join(transform.DOScale(maxSize, 1.5f).SetEase(Ease.Linear)).SetUpdate(true);
            seq.Join(SRCompo.DOFade(0, 1.5f)).SetEase(Ease.Linear).SetUpdate(true);
            seq.AppendCallback(() => transform.localScale = Vector3.zero);
        }
    }
}