using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class KDH_ShakeEffect : MonoBehaviour
{
    [SerializeField] private float shakeMinDuration;
    [SerializeField] private float shakeMaxDuration;
    [SerializeField] private float shakeMinAmount;
    [SerializeField] private float shakeMaxAmount;

    private Sequence seq;

    [SerializeField] private UnityEvent onShake;
    
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
        if (shakeMinDuration <= 0f || shakeMinAmount <= 0f)
        {
            return;
        }

        seq?.Kill();

        float shakeAmount = Random.Range(shakeMinAmount, shakeMaxAmount);
        float shakeDuration = Random.Range(shakeMinDuration, shakeMaxDuration);
        
        seq = DOTween.Sequence()
            .Append(transform.DOShakePosition(shakeDuration, shakeAmount))
            .AppendCallback(() => onShake?.Invoke())
            .SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject);
    }
}
