using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Jobs;

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
        
        [SerializeField] private float skillCooldown;
        [SerializeField] private AssassinNormalSkill skillPrefab;

    	Collider2D _col;
    	Camera _cam;
    	Animator _animator;
    	Vector2 _move;

        private Rigidbody2D _rb;
        private float _moveDir;
        private float _currentSkillCool;

        private bool _skillReUse;

        private AssassinNormalSkill _skill;

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
                _rb.linearVelocityY = 0;
                _rb.AddForce(Vector2.up * jumpForce * 100);
            }
        }

        private void OnSkill(InputValue value)
        {
            Debug.Log("Skill");
            if (_skillReUse && _skill != null)
            {
                Debug.Log("Re");
                ReSkillCoroutine(_skill);
                return;
            }
            if (_currentSkillCool <= 0)
            {
                Debug.Log("Start");
                SkillCoroutine();
                _currentSkillCool = skillCooldown;
            }
        }

        private void SkillCoroutine()
        {
            _skill = null;
            
            Vector3 mouseWorldPos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
            mouseWorldPos.z = 0; 
            
            Vector3 direction = mouseWorldPos - transform.position;
            
            direction.Normalize();
            _skill = Instantiate(skillPrefab, transform.position, Quaternion.identity);
            _skill.transform.up = direction; 
    
            _skillReUse = true;
        }
        
        private void ReSkillCoroutine(AssassinNormalSkill skill)
        {
            _skillReUse = false;
            transform.position = skill.transform.position;
            Destroy(skill.gameObject);
        }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _cam = Camera.main;
        _animator = _visual.GetComponent<Animator>();
        GetComponent<Health>().OnDamaged += () => _animator.SetTrigger("GetDamage");
    }

    void OnMove(InputValue value)
    {
        _move = value.Get<Vector2>();
    }

    void Update()
    {
        _rb.linearVelocity = new Vector2(_move.x * _moveSpeed, _rb.linearVelocity.y);
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
        _visual.localScale = scale;
    }
    }
}