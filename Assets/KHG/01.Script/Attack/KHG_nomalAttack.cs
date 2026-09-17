using SSW;
using System.Collections;
using UnityEngine;

namespace NKY.Scripts
{
    public class KHG_nomalAttack : AbstractMeleeWeapon
    {
        [SerializeField] private float attackDelay = 0.3f;

        private KHG_SwordAttack attackMotion;

        public System.Action<GameObject, float> OnNormalAttackHit;

        protected override void Awake()
        {
            base.Awake();

            attackMotion = GetComponentInParent<KHG_SwordAttack>();

            if (attackMotion != null)
            {
                attackDelay = attackMotion.SwingTime + attackMotion.ReturnTime;
            }

            _currentOffset = new Vector2(offset, 0);
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetMouseButtonDown(0))
            {
                Attack();
            }
        }

        public override void Attack()
        {
            if (Time.time - _currentCooldown < attackCooldown)
                return;

            _currentCooldown = Time.time;

            StartCoroutine(AttackCoroutine());

            AttackScan();
        }

        private IEnumerator AttackCoroutine()
        {
            isAttacking = true;

            yield return new WaitForSeconds(attackDelay);

            isAttacking = false;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 originalMatrix = Gizmos.matrix;

            Vector3 position =
                (Vector2)transform.position + _currentOffset;

            Quaternion rotation =
                Quaternion.Euler(0, 0, _currentAngle);

            Gizmos.matrix =
                Matrix4x4.TRS(position, rotation, hitboxSize);

            Gizmos.color = Color.green;

            Gizmos.DrawWireCube(
                Vector3.zero,
                hitboxSize
            );

            Gizmos.matrix = originalMatrix;
        }
    }
}