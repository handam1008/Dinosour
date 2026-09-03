using SSW;
using System.Collections;
using UnityEngine;

public class KHG_nomalAttack : MonoBehaviour
{
    [SerializeField] private Collider2D attackCollider;

    private float Damage = 30f;

    [SerializeField] private float attackDelay = 0.5f;
    private float lastAttackTime = -999f;

    private void Start()
    {
        attackCollider.enabled = false;
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

            lastAttackTime = Time.time;

            attackCollider.enabled = true;

            Invoke(nameof(DisableAttackCollider), 0.15f);
        }
    }

    private void DisableAttackCollider()
    {
        attackCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Damage);
        }
    }
}