using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class PlayerController : MonoBehaviour, ISlowable
    {
        [SerializeField] float _moveSpeed = 7f;
        [SerializeField] float _jumpForce = 13f;
        [SerializeField] LayerMask _whatIsGround;
        [SerializeField] Transform _visual;

        Rigidbody2D _rb;
        Collider2D _col;
        Camera _cam;
        Animator _animator;
        Vector2 _move;
        float _slowMultiplier = 1f;
        float _slowEndTime;
        Coroutine _dropThroughRoutine;
        Collider2D _ignoredPlatform;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _cam = Camera.main;
            _animator = _visual.GetComponent<Animator>();
            GetComponent<Health>().OnDamaged += () => _animator.SetTrigger("GetDamage");
        }

        public float FacingSign => Mathf.Sign(_visual.localScale.x);

        public void ApplySlow(float amount, float duration)
        {
            _slowMultiplier = Mathf.Clamp01(1f - amount);
            _slowEndTime = Time.time + duration;
        }

        void OnMove(InputValue value)
        {
            _move = value.Get<Vector2>();
        }

        void OnJump(InputValue value)
        {
            if (!value.isPressed) return;

            Collider2D ground = GetGroundCollider();
            if (ground == null) return;

            if (_move.y < -0.5f && ground.GetComponent<PlatformEffector2D>() != null)
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
        }

        void Update()
        {
            if (Time.time >= _slowEndTime) _slowMultiplier = 1f;
            _rb.linearVelocity = new Vector2(_move.x * _moveSpeed * _slowMultiplier, _rb.linearVelocity.y);
            _animator.SetFloat("Speed", Mathf.Abs(_move.x));
            _animator.SetBool("IsGrounded", IsGrounded());
            FaceMouse();
        }

        void FaceMouse()
        {
            Vector3 world = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3 scale = _visual.localScale;
            scale.x = world.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            _visual.localScale = scale;
        }

        bool IsGrounded()
        {
            return GetGroundCollider() != null;
        }

        Collider2D GetGroundCollider()
        {
            Bounds b = _col.bounds;
            return Physics2D.OverlapCircle(new Vector2(b.center.x, b.min.y), 0.12f, _whatIsGround);
        }
    }
}
