using UnityEngine;
using UnityEngine.InputSystem;

public class SuitSelector : MonoBehaviour
{
    [SerializeField] Sprite[] _suitSprites;
    [SerializeField] Transform _strip;
    [SerializeField] SpriteRenderer _prev;
    [SerializeField] SpriteRenderer _current;
    [SerializeField] SpriteRenderer _next;
    [SerializeField] float _slideDuration = 0.2f;
    [SerializeField] float _slideOffset = 0.45f;
    [SerializeField] float _fadeDuration = 0.15f;
    [SerializeField] float _sideVisibleDuration = 0.9f;
    [SerializeField] float _centerVisibleDuration = 1.4f;
    [SerializeField] float _sideAlpha = 0.35f;

    int _index;
    float _slideT = 1f;

    float _prevShownAt = -999f, _prevHideAt = -999f;
    float _currentShownAt = -999f, _currentHideAt = -999f;
    float _nextShownAt = -999f, _nextHideAt = -999f;

    public Suit CurrentSuit => (Suit)_index;

    void OnEnable()
    {
        _index = 0;
        _slideT = 1f;
        _strip.localPosition = Vector3.zero;
        Refresh();
    }

    void OnCycleSuit(InputValue value)
    {
        if (!value.isPressed || _slideT < 1f) return;
        _index = (_index + 1) % _suitSprites.Length;
        _slideT = 0f;

        float now = Time.time;
        _prevShownAt = now;
        _prevHideAt = now + _sideVisibleDuration;
        _nextShownAt = now;
        _nextHideAt = now + _sideVisibleDuration;
        _currentShownAt = now;
        _currentHideAt = now + _centerVisibleDuration;
    }

    public void FlashCurrent()
    {
        float now = Time.time;
        _currentShownAt = now;
        _currentHideAt = now + _centerVisibleDuration;
    }

    void Update()
    {
        if (_slideT < 1f)
        {
            _slideT = Mathf.Min(_slideT + Time.deltaTime / _slideDuration, 1f);
            float e = Mathf.SmoothStep(0f, 1f, _slideT);
            _strip.localPosition = new Vector3(Mathf.Lerp(0f, -_slideOffset, e), 0f, 0f);
            if (_slideT >= 1f)
            {
                _strip.localPosition = Vector3.zero;
                Refresh();
            }
        }

        SetAlpha(_prev, _prevShownAt, _prevHideAt, _sideAlpha);
        SetAlpha(_current, _currentShownAt, _currentHideAt, 1f);
        SetAlpha(_next, _nextShownAt, _nextHideAt, _sideAlpha);
    }

    void SetAlpha(SpriteRenderer sr, float shownAt, float hideAt, float target)
    {
        float t = Time.time;
        float fadeIn = Mathf.Clamp01((t - shownAt) / _fadeDuration);
        float fadeOut = Mathf.Clamp01((hideAt - t) / _fadeDuration);
        Color c = sr.color;
        c.a = target * Mathf.Min(fadeIn, fadeOut);
        sr.color = c;
    }

    void Refresh()
    {
        int prevIndex = (_index - 1 + _suitSprites.Length) % _suitSprites.Length;
        int nextIndex = (_index + 1) % _suitSprites.Length;
        _prev.sprite = _suitSprites[prevIndex];
        _current.sprite = _suitSprites[_index];
        _next.sprite = _suitSprites[nextIndex];
    }
}
