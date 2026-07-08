using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class NumberRoller : MonoBehaviour
{
    [SerializeField] Sprite[] _redRanks;
    [SerializeField] Sprite[] _blackRanks;
    [SerializeField] Transform _anchor;
    [SerializeField] SpriteRenderer _renderer;
    [SerializeField] SuitSelector _suitSelector;
    [SerializeField] float _tickInterval = 0.15f;
    [SerializeField] float _visibleDuration = 1.4f;
    [SerializeField] float _fadeDuration = 0.15f;
    [SerializeField] float _popScale = 1.1f;

    bool _rolling;
    float _tickTimer;
    int _gen;
    InputAction _attackAction;

    public bool IsRolling => _rolling;

    void Awake()
    {
        _attackAction = GetComponent<PlayerInput>().actions.FindAction("Attack");
    }

    void OnEnable()
    {
        _rolling = false;
        _gen = 0;
        _anchor.DOKill();
        _renderer.DOKill();
        _anchor.localScale = Vector3.one;
        SetAlphaInstant(_renderer, 0f);
    }

    void OnAttack(InputValue value)
    {
        if (value.isPressed) BeginRoll();
        else EndRoll();
    }

    void BeginRoll()
    {
        _rolling = true;
        _tickTimer = 0f;
        _gen++;
        _suitSelector.LockDuringNumberRoll();
        _suitSelector.ShiftForRoll();
        _renderer.DOKill();
        _renderer.DOFade(1f, _fadeDuration);
    }

    void EndRoll()
    {
        _rolling = false;

        _anchor.DOKill();
        _anchor.localScale = Vector3.one;
        _anchor.DOPunchScale(Vector3.one * (_popScale - 1f), _fadeDuration * 2f, 1, 0.5f);

        _gen++;
        int gen = _gen;
        DOVirtual.DelayedCall(_visibleDuration, () =>
        {
            if (gen != _gen) return;
            _renderer.DOKill();
            _renderer.DOFade(0f, _fadeDuration);
        });

        _suitSelector.NotifyNumberRollEnded();
    }

    void Update()
    {
        if (!_rolling) return;
        if (_attackAction != null && !_attackAction.IsPressed())
        {
            EndRoll();
            return;
        }
        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickInterval;
            Sprite[] ranks = IsRedSuit(_suitSelector.CurrentSuit) ? _redRanks : _blackRanks;
            _renderer.sprite = ranks[Random.Range(0, ranks.Length)];
        }
    }

    static bool IsRedSuit(Suit suit) => suit == Suit.Heart || suit == Suit.Diamond;

    static void SetAlphaInstant(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
