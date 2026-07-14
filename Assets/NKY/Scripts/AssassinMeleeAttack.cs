using System.Collections;
using SSW;
using UnityEngine;

namespace NKY.Scripts
{
    public class AssassinMeleeAttack : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask;


        private void AttackScan(Vector2 hitboxSize, float damage)
        {
            Collider2D[] hits;
            hits = Physics2D.OverlapBoxAll((Vector2)transform.position + new Vector2(hitboxSize.x * 0.5f, 0), hitboxSize, 0, targetMask);

            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }
    }
}