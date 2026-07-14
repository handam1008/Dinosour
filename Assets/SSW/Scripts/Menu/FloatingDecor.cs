using UnityEngine;
using DG.Tweening;

namespace SSW
{
    public class FloatingDecor : MonoBehaviour
    {
        [SerializeField] float _bobDistance = 14f;
        [SerializeField] float _bobDuration = 2.2f;
        [SerializeField] float _tiltAngle = 10f;
        [SerializeField] float _startDelay;

        void Start()
        {
            RectTransform rt = (RectTransform)transform;
            rt.DOAnchorPosY(rt.anchoredPosition.y + _bobDistance, _bobDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(_startDelay);
            rt.DOLocalRotate(new Vector3(0f, 0f, _tiltAngle), _bobDuration * 1.3f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(_startDelay * 0.5f);
        }
    }
}
