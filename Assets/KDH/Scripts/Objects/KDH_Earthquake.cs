using DG.Tweening;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_Earthquake : MonoBehaviour
    {
        [SerializeField] private float slowAmount;
        [SerializeField] private float slowDuration;
        [SerializeField] private float shakeDuration;
        [SerializeField] private float shakeAmount;

        private Sequence seq;
        private bool canDamage;

        private void Start()
        {
            Shake();
        }

        private void OnDestroy()
        {
            seq?.Kill();
        }

        private void Shake()
        {
            if (shakeDuration <= 0f || shakeAmount <= 0f)
            {
                return;
            }

            seq?.Kill();

            seq = DOTween.Sequence()
                .AppendCallback(() => canDamage = true)
                .Append(transform.DOShakePosition(shakeDuration, shakeAmount))
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!canDamage) return;

            if (collision.gameObject.TryGetComponent<ISlowable>(out var slowable))
                slowable.ApplySlow(slowAmount, slowDuration);
        }
    }
}