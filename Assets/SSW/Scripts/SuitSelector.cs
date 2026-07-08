using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class SuitSelector : MonoBehaviour
{
    [SerializeField] Sprite[] _suitSprites;
    [SerializeField] Transform _anchor;
    [SerializeField] SpriteRenderer _renderer;
    [SerializeField] NumberRoller _numberRoller;
    [SerializeField] float _tickInterval = 0.15f;
    [SerializeField] float _visibleDuration = 1.4f;
    [SerializeField] float _fadeDuration = 0.15f;
    [SerializeField] float _popScale = 1.1f;
    [SerializeField] float _rollOffset = 0.4f;

    int _index;
    bool _rolling;
    float _tickTimer;
    int _gen;
    InputAction _cycleAction;

    public Suit CurrentSuit => (Suit)_index;
    public bool IsRolling => _rolling;

    void Awake()
    {
        _cycleAction = GetComponent<PlayerInput>().actions.FindAction("CycleSuit");
    }

    void OnEnable()
    {
        _index = 0;
        _rolling = false;
        _gen = 0;
        _anchor.DOKill();
        _renderer.DOKill();
        _anchor.localPosition = Vector3.zero;
        _anchor.localScale = Vector3.one;
        _renderer.sprite = _suitSprites[_index];
        SetAlphaInstant(_renderer, 0f);
    }

    void OnCycleSuit(InputValue value)
    {
        if (value.isPressed)
        {
            if (_numberRoller != null && _numberRoller.IsRolling) return;
            BeginRoll();
        }
        else
        {
            EndRoll();
        }
    }

    void BeginRoll()
    {
        _rolling = true;
        _tickTimer = 0f;
        _gen++;
        _renderer.DOKill();
        _renderer.DOFade(1f, _fadeDuration);
    }

    void EndRoll()
    {
        _rolling = false;
        if (_numberRoller != null && _numberRoller.IsRolling) return;
        Finalize();
    }

    void Update()
    {
        if (!_rolling) return;
        if (_cycleAction != null && !_cycleAction.IsPressed())
        {
            EndRoll();
            return;
        }
        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickInterval;
            _index = Random.Range(0, _suitSprites.Length);
            _renderer.sprite = _suitSprites[_index];
        }
    }

    public void LockDuringNumberRoll()
    {
        _rolling = false;
    }

    public void ShiftForRoll()
    {
        _gen++;
        _anchor.DOKill();
        _anchor.DOLocalMoveX(-_rollOffset, _fadeDuration);
        _renderer.DOKill();
        _renderer.DOFade(1f, _fadeDuration);
    }

    public void NotifyNumberRollEnded()
    {
        if (_rolling) return;
        Finalize();
    }

    void Finalize()
    {
        _anchor.DOKill();
        _anchor.DOLocalMoveX(0f, _fadeDuration);
        Pop();
        ScheduleHide();
    }

    void Pop()
    {
        _anchor.localScale = Vector3.one;
        _anchor.DOPunchScale(Vector3.one * (_popScale - 1f), _fadeDuration * 2f, 1, 0.5f);
    }

    void ScheduleHide()
    {
        _gen++;
        int gen = _gen;
        DOVirtual.DelayedCall(_visibleDuration, () =>
        {
            if (gen != _gen) return;
            _renderer.DOKill();
            _renderer.DOFade(0f, _fadeDuration);
        });
    }

    static void SetAlphaInstant(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
