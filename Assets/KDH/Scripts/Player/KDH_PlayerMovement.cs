using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class KDH_PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _range;
    [SerializeField] private LayerMask _whatIsFloor;
    public Vector2 MoveDir {get ; private set;}
    private Rigidbody2D _rigid;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CheckGround();
    }

    private void FixedUpdate()
    {
        _rigid.linearVelocityX = MoveDir.x * _speed;
    }

    private void OnMove(InputValue value)
    {
        MoveDir = value.Get<Vector2>();
    }

    private void OnJump()
    {
        if (CheckGround().collider != null)
        {
            _rigid.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        }
    }

    private RaycastHit2D CheckGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, _range, _whatIsFloor);
        Debug.DrawRay(transform.position, Vector2.down * _range,Color.red, _whatIsFloor);
        return hit;
    }
}
