using System.Collections;
using SSW;
using UnityEngine;

namespace NKY.Scripts
{
    public class AssassinMeleeAttack : AbstractMeleeWeapon
    {


        private void Awake()
        {
            _attackAnim = transform.Find("Visual").GetComponent<Animator>();
            _effectAnim = transform.Find("SlashEffect").GetComponent<Animator>();
            
            _attackAnim.gameObject.SetActive(false);
            _effectAnim.gameObject.SetActive(false);

            _currentOffset = new Vector2(offset, 0);
        }

        private void Update()
        {
            RotateWeapon();
        }

        private void RotateWeapon()
        {
            if(isAttacking) return;
            float angle = _currentAngle;
            
            bool isParentFlipped = transform.parent != null && transform.parent.lossyScale.x < 0;
            
            bool isLookingLeft = Mathf.Abs(angle) > 90f;
            
            float scaleX = isParentFlipped ? -1f : 1f;
            float scaleY = isLookingLeft ? -1f : 1f;

            transform.localScale = new Vector3(scaleX, scaleY, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }



        private IEnumerator AttackCoroutine()
        {
            isAttacking = true;
            _attackAnim.gameObject.SetActive(true);
            _effectAnim.gameObject.SetActive(true);
            _attackAnim.Play("Attack");
            _effectAnim.Play("Slash");
            yield return new WaitForSeconds(_effectAnim.GetCurrentAnimatorStateInfo(0).length);
            _attackAnim.gameObject.SetActive(false);
            _effectAnim.gameObject.SetActive(false);
            isAttacking = false;
        }

        public void AssassinAttack()
        {
            if(Time.time - _currentCooldown < attackCooldown) return;
            
            _currentCooldown = Time.time;
            StartCoroutine(AttackCoroutine());
            AttackScan();
        }
        
        private void AttackScan()
        {
            Collider2D[] hits;
            
            hits = Physics2D.OverlapBoxAll((Vector2)transform.position + _currentOffset, hitboxSize, _currentAngle, targetMask);
            
            foreach (Collider2D hit in hits)
            {
                if(hit.transform.root == transform.root) continue;
                
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 originalMatrix = Gizmos.matrix;
            
            Vector3 position = (Vector2)transform.position + _currentOffset;
            
            Quaternion rotation = Quaternion.Euler(0, 0, _currentAngle);
            
            Vector3 scale = hitboxSize;
            
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, scale);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, hitboxSize);

            Gizmos.matrix = originalMatrix;
        }
    }
}