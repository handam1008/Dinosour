using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SSW;

public class KHG_Dash : MonoBehaviour
{
    [Header("대쉬 설정")]
    [SerializeField] public float _dashSpeed = 20f;
    [SerializeField] public float _dashDuration = 0.2f;
    [SerializeField] public float _dashCooldown = 1f;

    [Header("대쉬 데미지 설정")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private float _damageMultiplier = 1f;

    [Header("대쉬 쿨타임 실시간 관리")]
    private float _currentCooldown = 0f;

    private Rigidbody2D _rb;
    private Transform _visual;
    private PlayerController _playerController;
    private KHG_DashSpeed _dashSpeedBoost;
    private Collider2D _playerCollider;

    private bool _canDash = true;
    private bool _isDashing;

    private HashSet<IDamageable> _hitEnemies = new HashSet<IDamageable>();

    public System.Action<GameObject, float> OnDashHitEnemy;

    public bool IsDashing => _isDashing;

    private void Awake()
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
    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    private void OnCycleSuit(InputValue value)
    {
        if (!_canDash || _isDashing)
            return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
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
            InputAction moveAction =
                playerInput.actions.FindAction("Move");

            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }
        }

        float dashDirection;

        if (moveInput.x != 0)
        {
            dashDirection = Mathf.Sign(moveInput.x);
        }
        else
        {
            dashDirection = Mathf.Sign(_visual.localScale.x);
        }

        _rb.linearVelocity = new Vector2(
            dashDirection * _dashSpeed,
            0f
        );

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

        _damageMultiplier = 1f;

        _currentCooldown = _dashCooldown;

        while (_currentCooldown > 0f)
        {
            _currentCooldown -= Time.deltaTime;
            yield return null;
        }

        _canDash = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isDashing)
            return;

        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            ApplyDashDamage(collision.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isDashing)
            return;

        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            ApplyDashDamage(collision.gameObject);
        }
    }

    private void ApplyDashDamage(GameObject target)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            if (!_hitEnemies.Contains(damageable))
            {
                _hitEnemies.Add(damageable);

                float finalDamage = damage * _damageMultiplier;

                damageable.TakeDamage(finalDamage);

                OnDashHitEnemy?.Invoke(target, finalDamage);
            }
        }
    }

    public void ReduceCooldown(float seconds)
    {
        if (!_canDash)
        {
            _currentCooldown = Mathf.Max(
                0f,
                _currentCooldown - seconds
            );
        }
    }

    public void ResetDashState()
    {
        _isDashing = false;
        _canDash = true;

        if (_playerController != null)
            _playerController.enabled = true;

        if (_playerCollider != null)
            _playerCollider.isTrigger = false;
    }
}