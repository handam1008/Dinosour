using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace SSW
{
    public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] float _hoverScale = 1.06f;
        [SerializeField] float _duration = 0.12f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(_hoverScale, _duration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(1f, _duration);
        }
    }
}
