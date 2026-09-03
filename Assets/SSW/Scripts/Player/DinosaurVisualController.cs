using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;

namespace SSW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(SpriteResolver))]
    public sealed class DinosaurVisualController : MonoBehaviour, ISpriteLibraryReceiver
    {
        const string Idle = "Idle";
        const string Move = "Move";
        const string Hurt = "Hurt";
        const string Dead = "Dead";
        const string Jump = "Jump";
        const string EggMove = "EggMove";
        const string EggCrack = "EggCrack";
        const string EggHatch = "EggHatch";

        [SerializeField] bool _playHatchOnStart = true;
        [SerializeField, Min(0.1f)] float _hatchDuration = 3f;
        [SerializeField, Min(1f)] float _framesPerSecond = 12f;
        [SerializeField, Min(0.05f)] float _hurtDuration = 0.35f;
        [SerializeField] float _eggLift = 0.08f;
        [SerializeField] float _eggTilt = 7f;

        readonly Dictionary<string, int> _frameCounts = new Dictionary<string, int>();

        SpriteLibrary _spriteLibrary;
        SpriteResolver _resolver;
        Rigidbody2D _body;
        PlayerController _player;
        PlayerInput _input;
        Health _health;
        SpriteRenderer[] _accessories;
        Coroutine _hatchRoutine;
        string _state;
        int _frame;
        float _frameTimer;
        float _hurtUntil;
        bool _dead;
        bool _hatching;
        Vector3 _restPosition;
        Quaternion _restRotation;
        Vector3 _restScale;

        public SpriteLibraryAsset CurrentSpriteLibrary => _spriteLibrary != null
            ? _spriteLibrary.spriteLibraryAsset
            : null;

        void Awake()
        {
            _spriteLibrary = GetComponentInParent<SpriteLibrary>();
            _resolver = GetComponent<SpriteResolver>();
            _body = GetComponentInParent<Rigidbody2D>();
            _player = GetComponentInParent<PlayerController>();
            _input = GetComponentInParent<PlayerInput>();
            _health = GetComponentInParent<Health>();
            SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
            _accessories = GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer != mainRenderer)
                .ToArray();
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _restScale = transform.localScale;
            CacheFrameCounts();
        }

        void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDamaged += PlayHurt;
                _health.OnDied += PlayDead;
            }
        }

        IEnumerator Start()
        {
            if (_playHatchOnStart && HasState(EggMove) && HasState(EggCrack) && HasState(EggHatch))
            {
                _hatchRoutine = StartCoroutine(PlayHatch());
                yield break;
            }

            SetState(Idle);
        }

        void Update()
        {
            if (_hatching)
            {
                SetAccessoriesVisible(false);
                return;
            }
            if (_dead)
            {
                AdvanceFrame(Time.deltaTime, false);
                return;
            }

            if (Time.time < _hurtUntil)
                SetState(Hurt);
            else if (_player != null && !_player.IsGrounded)
                SetState(Jump);
            else if (_body != null && Mathf.Abs(_body.linearVelocity.x) > 0.08f)
                SetState(Move);
            else
                SetState(Idle);

            AdvanceFrame(Time.deltaTime, _state == Idle || _state == Move || _state == Hurt);
        }

        void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDamaged -= PlayHurt;
                _health.OnDied -= PlayDead;
            }

            if (_hatchRoutine != null)
                StopCoroutine(_hatchRoutine);

            RestoreTransform();
            _hatchRoutine = null;
            _hatching = false;
        }

        public void SetSpriteLibrary(SpriteLibraryAsset library)
        {
            if (_spriteLibrary == null)
                _spriteLibrary = GetComponentInParent<SpriteLibrary>();
            if (_spriteLibrary == null) return;

            _spriteLibrary.spriteLibraryAsset = library;
            CacheFrameCounts();
            string nextState = HasState(_state) ? _state : Idle;
            _state = null;
            SetState(nextState);
        }

        public void PlayHurt()
        {
            if (_dead || _hatching) return;
            _hurtUntil = Time.time + _hurtDuration;
            SetState(Hurt);
        }

        public void PlayDead()
        {
            if (_dead) return;
            _dead = true;
            if (_hatchRoutine != null)
            {
                StopCoroutine(_hatchRoutine);
                _hatchRoutine = null;
            }
            _hatching = false;
            RestoreTransform();
            SetState(Dead);
        }

        IEnumerator PlayHatch()
        {
            _hatching = true;
            SetAccessoriesVisible(false);
            bool restoreInput = _input != null && _input.enabled && _input.inputIsActive;
            if (restoreInput) _input.DeactivateInput();
            if (_body != null)
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);

            float moveDuration = _hatchDuration * 0.4f;
            float crackDuration = _hatchDuration * 0.27f;
            float hatchDuration = Mathf.Max(0.05f, _hatchDuration - moveDuration - crackDuration);

            yield return PlayEggMove(moveDuration);
            yield return PlaySequence(EggCrack, crackDuration);
            yield return PlaySequence(EggHatch, hatchDuration);

            RestoreTransform();
            _hatching = false;
            _hatchRoutine = null;
            SetState(Idle);
            SetAccessoriesVisible(true);
            if (restoreInput && _input != null && _input.enabled)
                _input.ActivateInput();
        }

        IEnumerator PlayEggMove(float duration)
        {
            SetState(EggMove);
            float elapsed = 0f;
            while (elapsed < duration && !_dead)
            {
                elapsed += Time.deltaTime;
                float wave = Mathf.Sin(elapsed * Mathf.PI * 4f);
                transform.localPosition = _restPosition + Vector3.up * (Mathf.Abs(wave) * _eggLift);
                transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, wave * _eggTilt);
                AdvanceFrame(Time.deltaTime, true);
                yield return null;
            }
        }

        IEnumerator PlaySequence(string state, float duration)
        {
            SetState(state);
            int count = GetFrameCount(state);
            float step = duration / Mathf.Max(1, count);
            for (int i = 0; i < count && !_dead; i++)
            {
                ShowFrame(i);
                yield return new WaitForSeconds(step);
            }
        }

        void SetState(string state)
        {
            if (string.IsNullOrEmpty(state) || _state == state) return;
            if (!HasState(state)) state = Idle;
            if (!HasState(state)) return;

            _state = state;
            _frame = 0;
            _frameTimer = 0f;
            ShowFrame(0);
        }

        void AdvanceFrame(float deltaTime, bool loop)
        {
            int count = GetFrameCount(_state);
            if (count <= 1) return;

            _frameTimer += deltaTime;
            float frameDuration = 1f / _framesPerSecond;
            while (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                if (_frame + 1 < count)
                    _frame++;
                else if (loop)
                    _frame = 0;
                ShowFrame(_frame);
            }
        }

        void ShowFrame(int frame)
        {
            if (_resolver == null || string.IsNullOrEmpty(_state)) return;
            _resolver.SetCategoryAndLabel(_state, frame.ToString());
        }

        void CacheFrameCounts()
        {
            _frameCounts.Clear();
            SpriteLibraryAsset asset = CurrentSpriteLibrary;
            if (asset == null) return;

            foreach (string category in asset.GetCategoryNames())
                _frameCounts[category] = asset.GetCategoryLabelNames(category).Count();
        }

        bool HasState(string state)
        {
            return !string.IsNullOrEmpty(state) && GetFrameCount(state) > 0;
        }

        int GetFrameCount(string state)
        {
            return !string.IsNullOrEmpty(state) && _frameCounts.TryGetValue(state, out int count)
                ? count
                : 0;
        }

        void RestoreTransform()
        {
            transform.localPosition = _restPosition;
            transform.localRotation = _restRotation;
            Vector3 scale = transform.localScale;
            scale.y = _restScale.y;
            scale.z = _restScale.z;
            transform.localScale = scale;
        }

        void SetAccessoriesVisible(bool visible)
        {
            if (_accessories == null) return;
            foreach (SpriteRenderer renderer in _accessories)
            {
                if (renderer != null)
                    renderer.enabled = visible && renderer.sprite != null;
            }
        }
    }
}
