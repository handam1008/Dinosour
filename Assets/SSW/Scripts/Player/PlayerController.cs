using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class PlayerController : MonoBehaviour, ISlowable, ISpeedable, IWeakenable, IOutgoingDamageModifier, IForceReceiver
    {
        [SerializeField] float _moveSpeed = 7f;
        [SerializeField] float _jumpForce = 13f;
        [SerializeField, Min(0f)] float _coyoteTime = 0.12f;
        [SerializeField] float _externalVelocityDecay = 12f;
        [SerializeField] float _counterMoveBrake = 30f;
        [SerializeField] LayerMask _whatIsGround;
        [SerializeField] Transform _visual;

        IPlayerDrive _drive;
        IForceReceiver _networkForce;
        bool _simulated = true;
        Rigidbody2D _rb;
        Collider2D _col;
        GroundProbe _ground;
        float _coyoteLeft;
        Camera _cam;
        Vector2 _move;
        float _slowMultiplier = 1f;
        float _slowEndTime;
        float _speedMultiplier = 1f;
        float _speedEndTime;
        float _outgoingDamageMultiplier = 1f;
        float _attackWeakenEndTime;
        float _externalVelocityX;
        Coroutine _dropThroughRoutine;
        Collider2D _ignoredPlatform;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _ground = new GroundProbe(_col, _rb, _whatIsGround);
            _cam = Camera.main;
        }

        public float SpeedFactor { get; set; } = 1f;
        public float MoveSpeed => _moveSpeed * CurrentMoveSpeedMultiplier * SpeedFactor;
        public MotionRate Rate => new MotionRate
        {
            Base = _moveSpeed * SpeedFactor,
            Slow = _slowMultiplier,
            Haste = _speedMultiplier,
            SlowTime = Mathf.Max(0f, _slowEndTime - Time.time),
            HasteTime = Mathf.Max(0f, _speedEndTime - Time.time)
        };
        public bool Predicted => _networkForce != null;
        public Vector2 Velocity => _networkForce is IMotionSource source ? source.Velocity : _rb.linearVelocity;
        public float JumpSpeed => _jumpForce;
        public float CoyoteTime => _coyoteTime;
        public LayerMask GroundMask => _whatIsGround;
        public float KnockbackDecay => _externalVelocityDecay;
        public float CounterBrake => _counterMoveBrake;
        public bool Simulated
        {
            get => _simulated;
            set
            {
                if (_simulated == value) return;
                _simulated = value;
                if (value) return;
                _move = Vector2.zero;
                _externalVelocityX = 0f;
                _coyoteLeft = 0f;
                _rb.linearVelocity = Vector2.zero;
            }
        }

        public void Bind(IPlayerDrive drive, bool simulated, IForceReceiver networkForce = null)
        {
            _drive = drive;
            _networkForce = networkForce;
            Simulated = simulated;
        }

        public void Move(Vector2 value)
        {
            _move = value;
        }

        public void Teleport(Vector2 position)
        {
            _coyoteLeft = 0f;
            _rb.position = position;
        }

        public float FacingSign => Mathf.Sign(_visual.localScale.x);
        public bool IsGrounded => _networkForce is IMotionSource source ? source.Grounded : GetGroundCollider() != null;
        public float CurrentMoveSpeedMultiplier
        {
            get
            {
                float slow = Time.time < _slowEndTime ? _slowMultiplier : 1f;
                float speed = Time.time < _speedEndTime ? _speedMultiplier : 1f;
                return slow * speed;
            }
        }
        public float CurrentOutgoingDamageMultiplier => Time.time < _attackWeakenEndTime ? _outgoingDamageMultiplier : 1f;
        public int Priority => 0;

        public void ApplySlow(float amount, float duration)
        {
            _slowMultiplier = Mathf.Clamp01(1f - amount);
            _slowEndTime = Time.time + duration;
        }

        public void ApplySpeed(float amount, float duration)
        {
            _speedMultiplier = 1f + Mathf.Max(0f, amount);
            _speedEndTime = Time.time + Mathf.Max(0f, duration);
        }

        public void ApplyAttackWeaken(float amount, float duration)
        {
            float multiplier = Mathf.Clamp01(1f - amount);
            if (Time.time >= _attackWeakenEndTime)
                _outgoingDamageMultiplier = multiplier;
            else
                _outgoingDamageMultiplier = Mathf.Min(_outgoingDamageMultiplier, multiplier);

            _attackWeakenEndTime = Mathf.Max(_attackWeakenEndTime, Time.time + duration);
        }

        public float ModifyOutgoingDamage(float amount)
        {
            if (Time.time >= _attackWeakenEndTime)
                _outgoingDamageMultiplier = 1f;

            return Mathf.Max(0f, amount) * _outgoingDamageMultiplier;
        }

        public void ApplyForce(Vector2 force, ForceMode2D mode)
        {
            if (_networkForce != null)
            {
                _networkForce.ApplyForce(force, mode);
                return;
            }
            if (_rb == null) return;

            float mass = Mathf.Max(0.0001f, _rb.mass);
            float timeStep = mode == ForceMode2D.Impulse ? 1f : Time.fixedDeltaTime;
            float pushedVelocity = force.x / mass * timeStep;
            if (!Mathf.Approximately(pushedVelocity, 0f))
                _externalVelocityX = pushedVelocity;

            if (!Mathf.Approximately(force.y, 0f))
                _rb.AddForce(Vector2.up * force.y, mode);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            StopKnockbackAtWall(collision);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            StopKnockbackAtWall(collision);
        }

        void StopKnockbackAtWall(Collision2D collision)
        {
            if (Mathf.Approximately(_externalVelocityX, 0f)) return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                float normalX = collision.GetContact(i).normal.x;
                if (Mathf.Abs(normalX) < 0.55f) continue;
                if (Mathf.Sign(_externalVelocityX) == -Mathf.Sign(normalX))
                {
                    _externalVelocityX = 0f;
                    return;
                }
            }
        }

        void OnMove(InputValue value)
        {
            if (_drive != null) _drive.Move(value.Get<Vector2>());
            else Move(value.Get<Vector2>());
        }

        void OnJump(InputValue value)
        {
            if (!value.isPressed) return;
            if (_drive != null) _drive.Jump();
            else Jump();
        }

        public void Jump()
        {
            Collider2D ground = GetGroundCollider();
            if (ground == null && _coyoteLeft <= 0.0001f) return;
            _coyoteLeft = 0f;

            if (_move.y < -0.5f && ground != null && GroundProbe.IsPlatform(ground))
            {
                if (_dropThroughRoutine == null)
                    _dropThroughRoutine = StartCoroutine(DropThroughPlatform(ground));
                return;
            }

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
        }

        IEnumerator DropThroughPlatform(Collider2D platform)
        {
            _ignoredPlatform = platform;
            Physics2D.IgnoreCollision(_col, platform, true);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -2f);

            float timeout = Time.time + 0.5f;
            while (Time.time < timeout && _col.bounds.max.y > platform.bounds.min.y)
                yield return new WaitForFixedUpdate();

            Physics2D.IgnoreCollision(_col, platform, false);
            _ignoredPlatform = null;
            _dropThroughRoutine = null;
        }

        void OnDisable()
        {
            if (_dropThroughRoutine != null)
                StopCoroutine(_dropThroughRoutine);
            if (_ignoredPlatform != null)
                Physics2D.IgnoreCollision(_col, _ignoredPlatform, false);
            _ignoredPlatform = null;
            _dropThroughRoutine = null;
            _externalVelocityX = 0f;
            _coyoteLeft = 0f;
        }

        void FixedUpdate()
        {
            if (!Simulated) return;
            _coyoteLeft = GetGroundCollider() != null ? _coyoteTime
                : _rb.linearVelocity.y > 0f ? 0f : Mathf.Max(0f, _coyoteLeft - Time.fixedDeltaTime);
        }

        void Update()
        {
            if (!Simulated) return;
            if (Time.time >= _slowEndTime) _slowMultiplier = 1f;
            if (Time.time >= _speedEndTime) _speedMultiplier = 1f;
            if (Time.time >= _attackWeakenEndTime) _outgoingDamageMultiplier = 1f;

            float brake = _externalVelocityDecay;
            if (!Mathf.Approximately(_externalVelocityX, 0f)
                && Mathf.Abs(_move.x) > 0.01f
                && Mathf.Sign(_move.x) != Mathf.Sign(_externalVelocityX))
            {
                brake += _counterMoveBrake * Mathf.Abs(_move.x);
            }

            _externalVelocityX = Mathf.MoveTowards(
                _externalVelocityX,
                0f,
                brake * Time.deltaTime);

            float controlledVelocity = _move.x * _moveSpeed * CurrentMoveSpeedMultiplier * SpeedFactor;
            _rb.linearVelocity = new Vector2(controlledVelocity + _externalVelocityX, _rb.linearVelocity.y);
            FaceMouse();
        }

        void FaceMouse()
        {
            if (_drive != null || Mouse.current == null || _cam == null) return;
            Vector3 world = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3 scale = _visual.localScale;
            scale.x = world.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            _visual.localScale = scale;
        }

        Collider2D GetGroundCollider() => _ground.Read(_ignoredPlatform);
    }
}
