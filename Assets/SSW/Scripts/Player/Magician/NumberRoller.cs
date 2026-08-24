using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace SSW
{
    public class NumberRoller : JobModuleBehaviour
    {
        [SerializeField] Sprite[] _redRanks;
        [SerializeField] Sprite[] _blackRanks;
        [SerializeField] Sprite[] _spadeCards;
        [SerializeField] Sprite[] _heartCards;
        [SerializeField] Sprite[] _diamondCards;
        [SerializeField] Sprite[] _cloverCards;
        [SerializeField] Sprite _jokerSprite;
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
        [SerializeField] float _cooldownDuration = 3f;
        [SerializeField] float _mirrorScaleMultiplier = 0.6f;
        [SerializeField] CooldownCursorUI _cooldownUI;

        bool _rolling;
        float _tickTimer;
        float _cooldownTimer;
        int _gen;
        int _rankIndex;
        int _rankIndexB;
        InputAction _attackAction;
        MagicianAugmentController _augments;
        SpriteRenderer _secondRenderer;
        bool _inactiveUIHidden;

        public bool IsRolling => _rolling;
        public override PlayerJob Job => PlayerJob.Magician;

        protected override void Awake()
        {
            base.Awake();
            _attackAction = GetComponent<PlayerInput>().actions.FindAction("Attack");
            _augments = GetComponent<MagicianAugmentController>();
        }

        protected override void OnEnable()
        {
            _rolling = false;
            _gen = 0;
            _inactiveUIHidden = false;
            _anchor.DOKill();
            _renderer.DOKill();
            _anchor.localPosition = new Vector3(0.4f, 0f, 0f);
            _anchor.localScale = Vector3.one;
            SetAlphaInstant(_renderer, 0f);
            if (_secondRenderer != null) SetAlphaInstant(_secondRenderer, 0f);
            base.OnEnable();
        }

        float TickInterval => _tickInterval * (_augments != null ? _augments.TickIntervalMultiplier : 1f);

        bool DoubleDraw => _augments != null && _augments.Has(MagicianAugmentType.DoubleDraw);

        void OnAttack(InputValue value)
        {
            if (!IsJobActive)
            {
                HideForInactiveJob();
                return;
            }

            if (value.isPressed)
            {
                if (_cooldownTimer > 0f) return;
                BeginRoll();
            }
            else
            {
                EndRoll();
            }
        }

        public void ForceHide()
        {
            _gen++;
            _anchor.DOKill();
            _renderer.DOKill();
            _renderer.DOFade(0f, _fadeDuration);
            if (_secondRenderer != null)
            {
                _secondRenderer.DOKill();
                _secondRenderer.DOFade(0f, _fadeDuration);
            }
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

            if (DoubleDraw)
            {
                EnsureSecondRenderer();
                _secondRenderer.DOKill();
                _secondRenderer.DOFade(1f, _fadeDuration);
            }
            else if (_secondRenderer != null)
            {
                _secondRenderer.DOKill();
                _secondRenderer.DOFade(0f, _fadeDuration);
            }
        }

        void EndRoll()
        {
            if (!_rolling) return;
            _rolling = false;

            if (DoubleDraw && _secondRenderer != null)
            {
                Sprite[] ranks = IsRedSuit(_suitSelector.CurrentSuit) ? _redRanks : _blackRanks;
                if (_rankIndexB > _rankIndex) _rankIndex = _rankIndexB;
                if (_rankIndex < ranks.Length) _renderer.sprite = ranks[_rankIndex];
                _secondRenderer.DOKill();
                _secondRenderer.DOFade(0f, _fadeDuration);
            }

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

            Suit suit = _suitSelector.CurrentSuit;
            int rankIndex = _rankIndex;
            float fireDelay = _augments != null ? _augments.FireDelay : 0f;
            if (fireDelay > 0f)
                DOVirtual.DelayedCall(fireDelay, () => SpawnFlyingCard(suit, rankIndex), false);
            else
                SpawnFlyingCard(suit, rankIndex);

            _suitSelector.NotifyNumberRollEnded();

            _cooldownTimer = _cooldownDuration;
        }

        void Update()
        {
            if (!IsJobActive)
            {
                HideForInactiveJob();
                return;
            }

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(_cooldownTimer - Time.deltaTime, 0f);
                if (_cooldownUI != null) _cooldownUI.SetProgress(_cooldownTimer / _cooldownDuration);
            }

            if (!_rolling) return;
            if (_attackAction != null && !_attackAction.IsPressed())
            {
                EndRoll();
                return;
            }
            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer = TickInterval;
                Sprite[] ranks = IsRedSuit(_suitSelector.CurrentSuit) ? _redRanks : _blackRanks;
                _rankIndex = Random.Range(0, ranks.Length);
                _renderer.sprite = ranks[_rankIndex];

                if (DoubleDraw && _secondRenderer != null)
                {
                    _rankIndexB = Random.Range(0, ranks.Length);
                    _secondRenderer.sprite = ranks[_rankIndexB];
                }
            }
        }

        void EnsureSecondRenderer()
        {
            if (_secondRenderer != null) return;

            GameObject clone = Instantiate(_renderer.gameObject, _renderer.transform.parent);
            clone.name = "NumberDisplayB";
            clone.transform.localPosition = _renderer.transform.localPosition + new Vector3(0.55f, 0.3f, 0f);
            clone.transform.localScale = _renderer.transform.localScale * 0.8f;
            _secondRenderer = clone.GetComponent<SpriteRenderer>();
            SetAlphaInstant(_secondRenderer, 0f);
        }

        void SpawnFlyingCard(Suit suit, int rankIndex)
        {
            if (!IsJobActive) return;
            Sprite[] cards = CardsFor(suit);
            if (cards == null || cards.Length == 0) return;

            bool isJoker = _augments != null && _augments.ConsumeJoker();
            if (isJoker) rankIndex = Mathf.Min(9, cards.Length - 1);
            if (rankIndex >= cards.Length) return;

            Sprite sprite = isJoker && _jokerSprite != null ? _jokerSprite : cards[rankIndex];

            Camera mainCamera = Camera.main;
            if (mainCamera == null || Mouse.current == null) return;

            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = transform.position.z;
            Vector3 dir = (mouseWorld - transform.position).normalized;

            FlyingCard card = CreateCard(sprite, suit, rankIndex + 1, dir, _flyScale);
            if (isJoker) card.MarkJoker();
        }

        public void SpawnMirrorCard(Suit suit, int rankIndex, Vector2 dir, float effectMultiplier)
        {
            if (!IsJobActive) return;
            Sprite[] cards = CardsFor(suit);
            if (cards == null || rankIndex < 0 || rankIndex >= cards.Length) return;

            FlyingCard card = CreateCard(cards[rankIndex], suit, rankIndex + 1, dir, _flyScale * _mirrorScaleMultiplier);
            card.MarkMirror();
            card.SetEffectMultiplier(effectMultiplier);
        }

        FlyingCard CreateCard(Sprite sprite, Suit suit, int number, Vector2 dir, float scale)
        {
            GameObject go = new GameObject("FlyingCard");
            go.transform.position = transform.position;
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 15;
            if (_flyingCardMaterial != null) sr.sharedMaterial = _flyingCardMaterial;

            float facing = _playerController != null ? _playerController.FacingSign : 1f;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = _flyGravityScale;
            rb.linearVelocity = dir.normalized * _flySpeed;
            rb.angularVelocity = _flySpin * facing;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            Collider2D ownCollider = GetComponent<Collider2D>();
            if (ownCollider != null) Physics2D.IgnoreCollision(col, ownCollider);

            FlyingCard card = go.AddComponent<FlyingCard>();
            card.Configure(suit, number, GetComponent<IHealable>());
            card.SetAugments(_augments, transform);
            card.SetLifetime(_flyMaxLifetime);
            return card;
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

        protected override void OnJobActivated()
        {
            _inactiveUIHidden = false;
            if (_cooldownUI != null)
            {
                float progress = _cooldownDuration > 0f ? _cooldownTimer / _cooldownDuration : 0f;
                _cooldownUI.SetProgress(progress);
            }
        }

        protected override void OnJobDeactivated()
        {
            HideForInactiveJob();
        }

        void HideForInactiveJob()
        {
            if (_inactiveUIHidden) return;
            _inactiveUIHidden = true;
            _rolling = false;
            _gen++;
            _anchor.DOKill();
            _renderer.DOKill();
            SetAlphaInstant(_renderer, 0f);
            if (_secondRenderer != null)
            {
                _secondRenderer.DOKill();
                SetAlphaInstant(_secondRenderer, 0f);
            }
            if (_cooldownUI != null) _cooldownUI.gameObject.SetActive(false);
        }
    }
}
