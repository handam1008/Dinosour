using SSW;
using System.Collections;
using UnityEngine;

public class KHG_nomalAttack : MonoBehaviour
{
    [SerializeField] private Collider2D attackCollider;

    private float Damage = 30f;

    [SerializeField] private float attackDelay = 0.3f;

    private float lastAttackTime = -999f;
    private KHG_SwordAttack attackMotion;

    private bool _canAttack;

    // 평타 적중 이벤트
    // GameObject = 맞은 적
    // float = 평타 피해량
    public System.Action<GameObject, float> OnNormalAttackHit;

    private void Awake()
    {
        attackMotion = GetComponentInParent<KHG_SwordAttack>();

        if (attackMotion != null)
        {
            attackDelay = attackMotion.SwingTime + attackMotion.ReturnTime;
        }
    }

    private void Start()
    {
        StartCoroutine(Attackroutine());
    }

    private IEnumerator Attackroutine()
    {
        yield return new WaitForSeconds(attackDelay);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time < lastAttackTime + attackDelay)
                return;

            _canAttack = true;

            lastAttackTime = Time.time;

            Invoke(nameof(DisableAttackCollider), 0.3f);
        }
    }

    private void DisableAttackCollider()
    {
        _canAttack = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_canAttack)
            return;

        if (collision.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Damage);

            OnNormalAttackHit?.Invoke(
                collision.gameObject,
                Damage
            );
        }
    }
}