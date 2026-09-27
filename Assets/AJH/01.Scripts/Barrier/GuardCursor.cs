using SSW;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GuardCursor : MonoBehaviour
{
    [SerializeField] MonoBehaviour _source;
    [SerializeField] RectTransform _ring;
    [SerializeField] Image _fill;

    ICooldownSource _cooldown;

    void Awake()
    {
        _cooldown = (ICooldownSource)_source;
    }

    void Update()
    {
        bool cooling = _cooldown.TryGetCooldown(out float remaining, out float duration);
        if (_ring.gameObject.activeSelf != cooling)
            _ring.gameObject.SetActive(cooling);
        if (!cooling) return;

        _fill.fillAmount = 1f - remaining / duration;
        _ring.position = Mouse.current.position.ReadValue();
    }
}
