using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class PlayerController : MonoBehaviour, ISlowable
    {
        [SerializeField] float _moveSpeed = 7f;
        [SerializeField] float _jumpForce = 12f;
        [SerializeField] LayerMask _whatIsGround;
        [SerializeField] Transform _visual;

        Rigidbody2D _rb;
        Collider2D _col;
        Camera _cam;
        Animator _animator;
        Vector2 _move;
        float _slowMultiplier = 1f;
        float _slowEndTime;

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
            if (value.isPressed && IsGrounded())
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
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
            Bounds b = _col.bounds;
            return Physics2D.OverlapCircle(new Vector2(b.center.x, b.min.y), 0.12f, _whatIsGround);
        }
    }
}
