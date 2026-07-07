using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Dash : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.8f;
    [SerializeField] private float dashCoolTime = 0.1f;
    
    private Rigidbody2D _rb;
    [SerializeField] private float _speed = 5f;
    
    private bool _isDashing;
    private bool _canDash = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Keyboard.current.shiftKey.wasPressedThisFrame)
        {
            //Vector2 dashDir = _playerMovement.GetLastDir();
           // if (dashDir != Vector2.zero)
            {
                //StartCoroutine(DashRoutine())
            }
        }
    }

    private IEnumerator DashRoutine(Vector2 dir)
    {
        _canDash = false;
        _isDashing = true;

        //if (playerMovement != null) _playerMovement.SetMove(false);
        
        _rb.linearVelocity = dir.normalized * dashSpeed;


        yield return new WaitForSeconds(dashDuration);

       
        _isDashing = false;
        
        // 대시가 끝나면 다시 움직일 수 있게 켭니다.
       // if (playerMovement != null) _playerMovement.SetMove(true);

        yield return new WaitForSeconds(dashCoolTime);
        _canDash = true;
    }

    public bool IsDashing => _isDashing; 
}
