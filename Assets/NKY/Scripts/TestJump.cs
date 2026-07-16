using UnityEngine;
using UnityEngine.InputSystem;

using SSW;
namespace NKY.Scripts
{
    public class TestJump : MonoBehaviour
    {
        [SerializeField] private float jumpForce;
        [SerializeField] private Vector2 checkBoxPosition;
        [SerializeField] private Vector2 boxSize;
        [SerializeField] private LayerMask whatIsGround;
		[SerializeField] float _moveSpeed = 7f;
    	[SerializeField] Transform _visual;

        [SerializeField] private AbstractPlayerSkillSo skillData;
        private AssassinMeleeAttack _attack;
        
    	Camera _cam;
    	Animator _animator;
    	Vector2 _move;

        public Rigidbody2D Rb { get; private set; }
        private float _moveDir;
        private float _currentSkillCool;

        private bool IsGrounded()
        {
            Collider2D hit = Physics2D.OverlapBox(transform.position + (Vector3)checkBoxPosition, boxSize,  0, whatIsGround);
            if (hit != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Vector3 center = transform.position + (Vector3)checkBoxPosition;
            Gizmos.DrawWireCube(center, boxSize);
        }
        
        private void OnJump(InputValue value)
        {
            if (IsGrounded())
            {
                Debug.Log("Jump");
                Rb.linearVelocityY = 0;
                Rb.AddForce(Vector2.up * jumpForce * 100);
            }
        }

        private void OnAttack(InputValue value)
        {
            _attack.AssassinAttack();
        }

        private void OnSkill(InputValue value)
        {
            skillData.StartSkill(this);
        }

    void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
        _animator = _visual.GetComponent<Animator>();
        _attack = GetComponentInChildren<AssassinMeleeAttack>();
        GetComponent<Health>().OnDamaged += () => _animator.SetTrigger("GetDamage");
        skillData.Init();
    }

    void OnMove(InputValue value)
    {
        _move = value.Get<Vector2>();
    }

    void Update()
    {
        Rb.linearVelocity = new Vector2(_move.x * _moveSpeed, Rb.linearVelocity.y);
        _animator.SetFloat("Speed", Mathf.Abs(_move.x));
        _animator.SetBool("IsGrounded", IsGrounded());
        FaceMouse();

        if (_currentSkillCool > 0)
        {
            _currentSkillCool -= Time.deltaTime;
        }
        else
        {
            _currentSkillCool = 0;
        }
    }

    void FaceMouse()
    {
        Vector3 world = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3 scale = _visual.localScale;
        scale.x = world.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        _attack.FaceAttack(world.x > transform.position.x);
        _visual.localScale = scale;
    }
    }
}