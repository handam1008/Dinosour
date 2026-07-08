using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KHG_Dash : MonoBehaviour
{
    [SerializeField] float _dashSpeed = 20f;      // 대쉬 속도
    [SerializeField] float _dashDuration = 0.2f;   // 대쉬 지속 시간
    [SerializeField] float _dashCooldown = 1f;    // 대쉬 재사용 대기시간

    Rigidbody2D _rb;
    Transform _visual;
    PlayerController _playerController;

    bool _canDash = true;
    bool _isDashing;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerController = GetComponent<PlayerController>();
        
        _visual = transform.Find("Visual"); 
        if (_visual == null)
        {
            _visual = transform.GetChild(0); 
        }
    }

    void OnCycleSuit(InputValue value)
    {
        
            StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        _canDash = false;
        _isDashing = true;

        if (_playerController != null) _playerController.enabled = false;

        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0f;

        Vector2 moveInput = Vector2.zero;
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            var moveAction = playerInput.actions.FindAction("Move");
            if (moveAction != null) moveInput = moveAction.ReadValue<Vector2>();
        }

        float dashDirection = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : Mathf.Sign(_visual.localScale.x);

        _rb.linearVelocity = new Vector2(dashDirection * _dashSpeed, 0f);

        yield return new WaitForSeconds(_dashDuration);

        _rb.gravityScale = originalGravity;
        
        if (_playerController != null) _playerController.enabled = true;
        
        _isDashing = false;

        yield return new WaitForSeconds(_dashCooldown);
        _canDash = true;
    }

}
