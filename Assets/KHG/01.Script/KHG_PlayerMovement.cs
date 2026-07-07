using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_PlayerMovement : MonoBehaviour
{
   private Rigidbody2D _rb;
   private float _speed = 7f;
   private Vector2 _moveDir;

   private void Awake()
   {
      _rb = GetComponent<Rigidbody2D>();
   }

   private void FixedUpdate()
   {
      _rb.linearVelocity = _moveDir * _speed;
   }

   private void OnMove(InputValue value)
   {
      _moveDir = value.Get<Vector2>();
   }
}
