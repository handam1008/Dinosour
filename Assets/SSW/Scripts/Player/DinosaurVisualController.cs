using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;

namespace SSW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(SpriteResolver))]
    [RequireComponent(typeof(Animator))]
    public sealed class DinosaurVisualController : MonoBehaviour, ISpriteLibraryReceiver
    {
        const string Idle = "Idle";
        const string Move = "Move";
        const string Hurt = "Hurt";
        const string Dead = "Dead";
        const string AirborneIdle = "AirborneIdle";
        const string EggMove = "EggMove";

        [SerializeField] bool _playHatchOnStart = true;
        [SerializeField, Min(0.1f)] float _hatchDuration = 3f;
        [SerializeField, Min(0.05f)] float _hurtDuration = 0.35f;

        SpriteLibrary _spriteLibrary;
        SpriteResolver _resolver;
        Animator _animator;
        Rigidbody2D _body;
        PlayerController _player;
        PlayerInput _input;
        Health _health;
        SpriteRenderer[] _accessories;
        Coroutine _hatchRoutine;
        string _animation;
        float _hurtUntil;
        float _normalAnimatorSpeed;
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
            _animator = GetComponent<Animator>();
            _body = GetComponentInParent<Rigidbody2D>();
            _player = GetComponentInParent<PlayerController>();
            _input = GetComponentInParent<PlayerInput>();
            _health = GetComponentInParent<Health>();
            SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
            _accessories = GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer != mainRenderer)
                .ToArray();
            _normalAnimatorSpeed = _animator.speed;
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _restScale = transform.localScale;
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
            if (_playHatchOnStart && HasAnimation(EggMove))
            {
                _hatchRoutine = StartCoroutine(PlayHatch());
                yield break;
            }

            PlayAnimation(Idle, true);
        }

        void Update()
        {
            if (_hatching)
            {
                SetAccessoriesVisible(false);
                return;
            }

            if (_dead) return;

            if (Time.time < _hurtUntil)
                PlayAnimation(Hurt);
            else if (_player != null && !_player.IsGrounded)
                PlayAnimation(AirborneIdle);
            else if (_body != null && Mathf.Abs(_body.linearVelocity.x) > 0.08f)
                PlayAnimation(Move);
            else
                PlayAnimation(Idle);
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

            if (_animator != null)
                _animator.speed = _normalAnimatorSpeed;

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
            _resolver?.ResolveSpriteToSpriteRenderer();
        }

        public void PlayHurt()
        {
            if (_dead || _hatching) return;
            _hurtUntil = Time.time + _hurtDuration;
            PlayAnimation(Hurt, true);
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
            if (_animator != null)
                _animator.speed = _normalAnimatorSpeed;
            RestoreTransform();
            PlayAnimation(Dead, true);
        }

        IEnumerator PlayHatch()
        {
            _hatching = true;
            SetAccessoriesVisible(false);
            bool restoreInput = _input != null && _input.enabled && _input.inputIsActive;
            if (restoreInput) _input.DeactivateInput();
            if (_body != null)
                _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);

            _animator.speed = _normalAnimatorSpeed * (3f / _hatchDuration);
            PlayAnimation(EggMove, true);
            yield return new WaitForSeconds(_hatchDuration);

            _animator.speed = _normalAnimatorSpeed;
            RestoreTransform();
            _hatching = false;
            _hatchRoutine = null;
            PlayAnimation(Idle, true);
            SetAccessoriesVisible(true);
            if (restoreInput && _input != null && _input.enabled)
                _input.ActivateInput();
        }

        void PlayAnimation(string animationName, bool restart = false)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            if (!restart && _animation == animationName) return;

            int stateHash = Animator.StringToHash(animationName);
            if (!_animator.HasState(0, stateHash)) return;

            _animation = animationName;
            _animator.Play(stateHash, 0, 0f);
        }

        bool HasAnimation(string animationName)
        {
            return _animator != null
                && _animator.runtimeAnimatorController != null
                && _animator.HasState(0, Animator.StringToHash(animationName));
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
