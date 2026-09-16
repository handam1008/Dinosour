using System;
using DG.Tweening;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_BackgroundVisual : MonoBehaviour
    {
        [SerializeField] private Color startColor;
        [SerializeField] private Color endColor;
        [SerializeField] private float transitionTime;
        [SerializeField] private float rotationTime;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            FadeColor();
        }

        private void FadeColor()
        {
            Sequence seq = DOTween.Sequence();

            seq.Append(_spriteRenderer.DOColor(startColor, 0).SetEase(Ease.OutExpo));
            seq.Append(_spriteRenderer.DOColor(endColor, transitionTime).SetEase(Ease.OutExpo));
            seq.AppendInterval(rotationTime);
            seq.SetLoops(-1);
        }
    }
}
