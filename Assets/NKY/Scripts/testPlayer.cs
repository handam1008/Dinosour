using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKY.Scripts
{
    public class testPlayer : MonoBehaviour
    {
        [SerializeField] private float jumpForce;
        [SerializeField] private float moveSpeed;
        [SerializeField] private Vector2 checkBoxPosition;
        [SerializeField] private Vector2 boxSize;
        [SerializeField] private LayerMask whatIsGround;
        private Rigidbody2D _rb;
        private float _moveDir;

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

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            _rb.linearVelocityX = _moveDir * moveSpeed;
        }

        private void OnMove(InputValue value)
        {
            _moveDir = value.Get<Vector2>().x;
            _moveDir *= moveSpeed;
        }

        private void OnJump(InputValue value)
        {
            Debug.Log("Jump");
            if (IsGrounded())
            {
                _rb.linearVelocityY = 0;
                _rb.AddForce(Vector2.up * jumpForce * 100);
            }
        }
    }
}