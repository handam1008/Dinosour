using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SSW;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(KHG_DashSpeed))]
public class KHG_Dash : MonoBehaviour
{
    [Header("대쉬 설정")]
    [SerializeField] public float _dashSpeed = 20f;
    [SerializeField] public float _dashDuration = 0.2f;
    [SerializeField] public float _dashCooldown = 1f;

    [Header("대쉬 데미지 설정")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D _rb;
    private Transform _visual;
    private PlayerController _playerController;
    private KHG_DashSpeed _dashSpeedBoost;
    private Collider2D _playerCollider;

    private bool _canDash = true;
    private bool _isDashing;

    private HashSet<IDamageable> _hitEnemies = new HashSet<IDamageable>();

    public bool IsDashing => _isDashing;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerController = GetComponent<PlayerController>();
        _dashSpeedBoost = GetComponent<KHG_DashSpeed>();
        _playerCollider = GetComponent<Collider2D>();

        _visual = transform.Find("Visual");
        if (_visual == null && transform.childCount > 0)
        {
            _visual = transform.GetChild(0);
        }
    }

    void OnCycleSuit(InputValue value)
    {
        if (!_canDash || _isDashing)
            return;

        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        _canDash = false;
        _isDashing = true;
        _hitEnemies.Clear();

        if (_playerController != null)
            _playerController.enabled = false;

        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0f;

        bool originalIsTrigger = false;
        if (_playerCollider != null)
        {
            originalIsTrigger = _playerCollider.isTrigger;
            _playerCollider.isTrigger = true;
        }

        Vector2 moveInput = Vector2.zero;
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            InputAction moveAction = playerInput.actions.FindAction("Move");
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();
        }

        float dashDirection = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : Mathf.Sign(_visual.localScale.x);
        _rb.linearVelocity = new Vector2(dashDirection * _dashSpeed, 0f);

        yield return new WaitForSeconds(_dashDuration);

        if (_playerCollider != null)
        {
            _playerCollider.isTrigger = originalIsTrigger;
        }

        _rb.gravityScale = originalGravity;

        if (_playerController != null)
            _playerController.enabled = true;

        if (_dashSpeedBoost != null)
        {
            _dashSpeedBoost.ActivateSpeedBoost();
        }

        _isDashing = false;

        yield return new WaitForSeconds(_dashCooldown);
        _canDash = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isDashing) return;

        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                if (!_hitEnemies.Contains(damageable))
                {
                    damageable.TakeDamage(damage);
                    _hitEnemies.Add(damageable);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isDashing) return;

        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
            {
                if (!_hitEnemies.Contains(damageable))
                {
                    damageable.TakeDamage(damage);
                    _hitEnemies.Add(damageable);
                }
            }
        }
    }
}