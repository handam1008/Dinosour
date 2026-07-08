using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SSW
{
    public class CooldownCursorUI : MonoBehaviour
    {
        [SerializeField] Image _fillImage;
        [SerializeField] Vector2 _cursorOffset = new Vector2(0f, -28f);

        bool _visible;

        public void SetProgress(float remainingFraction)
        {
            _visible = remainingFraction > 0f;
            gameObject.SetActive(_visible);
            if (!_visible) return;
            _fillImage.fillAmount = remainingFraction;
            transform.position = Mouse.current.position.ReadValue() + _cursorOffset;
        }

        void LateUpdate()
        {
            if (!_visible) return;
            transform.position = Mouse.current.position.ReadValue() + _cursorOffset;
        }
    }
}
