using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float _moveSpeed = 7f;
    [SerializeField] float _jumpForce = 12f;
    [SerializeField] LayerMask _whatIsGround;

    Rigidbody2D _rb;
    Collider2D _col;
    Camera _cam;
    Vector2 _move;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _cam = Camera.main;
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
        _rb.linearVelocity = new Vector2(_move.x * _moveSpeed, _rb.linearVelocity.y);
        FaceMouse();
    }

    void FaceMouse()
    {
        Vector3 world = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3 scale = transform.localScale;
        scale.x = world.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    bool IsGrounded()
    {
        Bounds b = _col.bounds;
        return Physics2D.OverlapCircle(new Vector2(b.center.x, b.min.y), 0.12f, _whatIsGround);
    }
}
