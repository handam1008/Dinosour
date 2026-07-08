using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SSW
{
    public class CooldownCursorUI : MonoBehaviour
    {
        [SerializeField] Image _fillImage;
        [SerializeField] Vector2 _cursorOffset = new Vector2(0f, -28f);

        public void SetProgress(float remainingFraction)
        {
            bool show = remainingFraction > 0f;
            gameObject.SetActive(show);
            if (!show) return;
            _fillImage.fillAmount = remainingFraction;
            transform.position = Mouse.current.position.ReadValue() + _cursorOffset;
        }
    }
}
