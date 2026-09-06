using SSW;
using System.Collections;
using UnityEngine;

public class KHG_nomalAttack : MonoBehaviour
{
    [SerializeField] private Collider2D attackCollider;

    private float Damage = 30f;

    [SerializeField] private float attackDelay = 0.3f; // KHG_SwordAttack의 공속과 연결됨

    private float lastAttackTime = -999f;
    private KHG_SwordAttack attackMotion;

    private bool _canAttack;

    private void Awake()
    {
        attackMotion = GetComponentInParent<KHG_SwordAttack>();
        attackDelay = attackMotion.SwingTime + attackMotion.ReturnTime;
    }

    private void Start()
    {
        //attackCollider.enabled = false;
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

            //attackCollider.enabled = true;

            Invoke(nameof(DisableAttackCollider), 0.3f);
        }
    }

    private void DisableAttackCollider()
    {
        _canAttack = false;
        //attackCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IDamageable>(out var damageable) && _canAttack)
        {
            damageable.TakeDamage(Damage);
        }
    }
}