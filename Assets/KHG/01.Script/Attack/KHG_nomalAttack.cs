using SSW;
using UnityEngine;

public class KHG_nomalAttack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D attackCollider;

    private float Damage = 30f;

    [SerializeField] private float attackDelay = 0.5f;
    private float lastAttackTime = -999f;

    private void Start()
    {
        attackCollider.enabled = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time < lastAttackTime + attackDelay)
                return;

            lastAttackTime = Time.time;

            animator.SetTrigger("SwordAttack");

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