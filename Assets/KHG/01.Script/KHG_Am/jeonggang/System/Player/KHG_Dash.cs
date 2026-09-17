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
    [SerializeField] private float hitRadius = 0.8f;

    [Header("대쉬 쿨타임 실시간 관리")]
    private float _currentCooldown = 0f;

    private float _damageMultiplier = 1f;

    private Rigidbody2D _rb;
    private PlayerController _playerController;
    private KHG_DashSpeed _dashSpeedBoost;
    private Transform _visual;

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

        _visual = transform.Find("Visual");

        if (_visual == null && transform.childCount > 0)
            _visual = transform.GetChild(0);
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

        _playerController.enabled = false;

        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0f;

        int playerLayer = LayerMask.NameToLayer("Player");

        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayer.value & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, true);
        }

        PlayerInput playerInput = GetComponent<PlayerInput>();

        Vector2 moveInput = Vector2.zero;

        if (playerInput != null)
        {
            InputAction moveAction =
                playerInput.actions.FindAction("Move");

            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();
        }

        float dashDirection;

        if (moveInput.x != 0)
            dashDirection = Mathf.Sign(moveInput.x);
        else
            dashDirection = Mathf.Sign(_visual.localScale.x);

        _rb.linearVelocity = new Vector2(
            dashDirection * _dashSpeed,
            0f
        );

        float timer = 0f;

        while (timer < _dashDuration)
        {
            CheckDashDamage();

            timer += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = originalGravity;

        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayer.value & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, false);
        }

        _playerController.enabled = true;

        if (_dashSpeedBoost != null)
            _dashSpeedBoost.ActivateSpeedBoost();

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

    private void CheckDashDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            hitRadius,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            ApplyDashDamage(hit.gameObject);
        }
    }

    private void ApplyDashDamage(GameObject target)
    {
        if (target.transform.root == transform.root)
            return;

        if (!target.TryGetComponent<IDamageable>(out var enemy))
            return;

        if (_hitEnemies.Contains(enemy))
            return;

        _hitEnemies.Add(enemy);

        float finalDamage = damage * _damageMultiplier;

        enemy.TakeDamage(finalDamage);

        OnDashHitEnemy?.Invoke(target, finalDamage);
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

        _rb.linearVelocity = Vector2.zero;

        _playerController.enabled = true;

        int playerLayer = LayerMask.NameToLayer("Player");

        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayer.value & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, false);
        }

        _hitEnemies.Clear();
    }
}