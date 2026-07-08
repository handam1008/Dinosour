using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class NumberRoller : MonoBehaviour
{
    [SerializeField] Sprite[] _redRanks;
    [SerializeField] Sprite[] _blackRanks;
    [SerializeField] Sprite[] _spadeCards;
    [SerializeField] Sprite[] _heartCards;
    [SerializeField] Sprite[] _diamondCards;
    [SerializeField] Sprite[] _cloverCards;
    [SerializeField] Transform _anchor;
    [SerializeField] SpriteRenderer _renderer;
    [SerializeField] SuitSelector _suitSelector;
    [SerializeField] PlayerController _playerController;
    [SerializeField] Material _flyingCardMaterial;
    [SerializeField] float _tickInterval = 0.15f;
    [SerializeField] float _visibleDuration = 1.4f;
    [SerializeField] float _fadeDuration = 0.15f;
    [SerializeField] float _popScale = 1.1f;
    [SerializeField] float _lockedOffset = 0.22f;
    [SerializeField] float _flyScale = 0.3f;
    [SerializeField] float _flySpeed = 12f;
    [SerializeField] float _flyGravityScale = 0.1f;
    [SerializeField] float _flySpin = 720f;
    [SerializeField] float _flyMaxLifetime = 3f;

    bool _rolling;
    float _tickTimer;
    int _gen;
    int _rankIndex;
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
        _anchor.localPosition = new Vector3(0.4f, 0f, 0f);
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
        _anchor.DOKill();
        _anchor.DOLocalMoveX(0.4f, _fadeDuration);
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
        _anchor.DOLocalMoveX(_lockedOffset, _fadeDuration);
        _anchor.DOPunchScale(Vector3.one * (_popScale - 1f), _fadeDuration * 2f, 1, 0.5f);

        _gen++;
        int gen = _gen;
        DOVirtual.DelayedCall(_visibleDuration, () =>
        {
            if (gen != _gen) return;
            _renderer.DOKill();
            _renderer.DOFade(0f, _fadeDuration);
        });

        SpawnFlyingCard(_suitSelector.CurrentSuit, _rankIndex);
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
            _rankIndex = Random.Range(0, ranks.Length);
            _renderer.sprite = ranks[_rankIndex];
        }
    }

    void SpawnFlyingCard(Suit suit, int rankIndex)
    {
        Sprite[] cards = CardsFor(suit);
        if (cards == null || rankIndex >= cards.Length) return;

        GameObject go = new GameObject("FlyingCard");
        go.transform.position = _anchor.position;
        go.transform.localScale = Vector3.one * _flyScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cards[rankIndex];
        sr.sortingOrder = 15;
        if (_flyingCardMaterial != null) sr.sharedMaterial = _flyingCardMaterial;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorld.z = go.transform.position.z;
        Vector3 dir = (mouseWorld - go.transform.position).normalized;

        float facing = _playerController != null ? _playerController.FacingSign : 1f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = _flyGravityScale;
        rb.linearVelocity = dir * _flySpeed;
        rb.angularVelocity = _flySpin * facing;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;

        Collider2D ownCollider = GetComponent<Collider2D>();
        if (ownCollider != null) Physics2D.IgnoreCollision(col, ownCollider);

        go.AddComponent<FlyingCard>();

        Object.Destroy(go, _flyMaxLifetime);
    }

    Sprite[] CardsFor(Suit suit)
    {
        switch (suit)
        {
            case Suit.Spade: return _spadeCards;
            case Suit.Heart: return _heartCards;
            case Suit.Diamond: return _diamondCards;
            default: return _cloverCards;
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
