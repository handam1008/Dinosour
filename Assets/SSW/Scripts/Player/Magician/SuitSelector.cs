using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace SSW
{
    public class SuitSelector : MonoBehaviour
    {
        [SerializeField] Sprite[] _suitSprites;
        [SerializeField] Transform _anchor;
        [SerializeField] SpriteRenderer _renderer;
        [SerializeField] SpriteRenderer _prevRenderer;
        [SerializeField] SpriteRenderer _nextRenderer;
        [SerializeField] NumberRoller _numberRoller;
        [SerializeField] float _tickInterval = 0.15f;
        [SerializeField] float _sideTickInterval = 0.3f;
        [SerializeField] float _visibleDuration = 1.4f;
        [SerializeField] float _fadeDuration = 0.15f;
        [SerializeField] float _popScale = 1.1f;
        [SerializeField] float _rollOffset = 0.4f;
        [SerializeField] float _lockedOffset = 0.22f;
        [SerializeField] float _sideAlpha = 0.35f;

        int _index;
        bool _rolling;
        float _tickTimer;
        float _sideTickTimer;
        int _gen;
        InputAction _cycleAction;
        MagicianAugmentController _augments;
        PlayerIdentity _identity;
        bool _nonMagicianUIHidden;

        public Suit CurrentSuit => (Suit)_index;
        public bool IsRolling => _rolling;

        float TickMultiplier => _augments != null ? _augments.TickIntervalMultiplier : 1f;
        bool IsMagician
        {
            get
            {
                if (_identity == null) _identity = GetComponent<PlayerIdentity>();
                return _identity != null && _identity.Job == PlayerJob.Magician;
            }
        }

        void Awake()
        {
            _cycleAction = GetComponent<PlayerInput>().actions.FindAction("CycleSuit");
            _augments = GetComponent<MagicianAugmentController>();
            _identity = GetComponent<PlayerIdentity>();
        }

        void OnEnable()
        {
            _index = 0;
            _rolling = false;
            _gen = 0;
            _nonMagicianUIHidden = false;
            _anchor.DOKill();
            _renderer.DOKill();
            _anchor.localPosition = Vector3.zero;
            _anchor.localScale = Vector3.one;
            _renderer.sprite = _suitSprites[_index];
            SetAlphaInstant(_renderer, 0f);
            SetAlphaInstant(_prevRenderer, 0f);
            SetAlphaInstant(_nextRenderer, 0f);
            if (!IsMagician) HideForNonMagician();
        }

        void OnCycleSuit(InputValue value)
        {
            if (!IsMagician)
            {
                HideForNonMagician();
                return;
            }

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
            _sideTickTimer = 0f;
            _gen++;
            if (_numberRoller != null) _numberRoller.ForceHide();
            _anchor.DOKill();
            _anchor.DOLocalMoveX(0f, _fadeDuration);
            _renderer.DOKill();
            _renderer.DOFade(1f, _fadeDuration);
            _prevRenderer.DOKill();
            _prevRenderer.DOFade(_sideAlpha, _fadeDuration);
            _nextRenderer.DOKill();
            _nextRenderer.DOFade(_sideAlpha, _fadeDuration);
        }

        void EndRoll()
        {
            _rolling = false;
            _prevRenderer.DOKill();
            _prevRenderer.DOFade(0f, _fadeDuration);
            _nextRenderer.DOKill();
            _nextRenderer.DOFade(0f, _fadeDuration);
            if (_numberRoller != null && _numberRoller.IsRolling) return;
            Finalize();
        }

        void Update()
        {
            if (!IsMagician)
            {
                HideForNonMagician();
                return;
            }

            if (!_rolling) return;
            if (_cycleAction != null && !_cycleAction.IsPressed())
            {
                EndRoll();
                return;
            }
            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _tickInterval * TickMultiplier;
                _index = Random.Range(0, _suitSprites.Length);
                _renderer.sprite = _suitSprites[_index];
            }

            _sideTickTimer -= Time.deltaTime;
            if (_sideTickTimer <= 0f)
            {
                _sideTickTimer = _sideTickInterval * TickMultiplier;
                _prevRenderer.sprite = _suitSprites[Random.Range(0, _suitSprites.Length)];
                _nextRenderer.sprite = _suitSprites[Random.Range(0, _suitSprites.Length)];
            }
        }

        public void LockDuringNumberRoll()
        {
            _rolling = false;
            _prevRenderer.DOKill();
            _prevRenderer.DOFade(0f, _fadeDuration);
            _nextRenderer.DOKill();
            _nextRenderer.DOFade(0f, _fadeDuration);
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
            Finalize(-_lockedOffset);
        }

        void Finalize()
        {
            Finalize(0f);
        }

        void Finalize(float targetX)
        {
            _anchor.DOKill();
            _anchor.DOLocalMoveX(targetX, _fadeDuration);
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

        void HideForNonMagician()
        {
            if (_nonMagicianUIHidden) return;
            _nonMagicianUIHidden = true;
            _rolling = false;
            _gen++;
            _anchor.DOKill();
            _renderer.DOKill();
            _prevRenderer.DOKill();
            _nextRenderer.DOKill();
            SetAlphaInstant(_renderer, 0f);
            SetAlphaInstant(_prevRenderer, 0f);
            SetAlphaInstant(_nextRenderer, 0f);
        }
    }
}
