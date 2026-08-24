using System.Collections;
using SSW;
using UnityEngine;

namespace NKY.Scripts
{
    public class AssassinMeleeAttack : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private float offset;
        [SerializeField] private Vector2 hitboxSize;
        [SerializeField] private float damage;
        [SerializeField] private float attackCooldown;

        private Vector2 _currentOffset;
        
        private float _currentCooldown = -99f;

        private Animator _attackAnim;
        private Animator _effectAnim;

        private float _currentAngle;

        private void Awake()
        {
            _attackAnim = transform.Find("Visual").GetComponent<Animator>();
            _effectAnim = transform.Find("SlashEffect").GetComponent<Animator>();
            
            _attackAnim.gameObject.SetActive(false);
            _effectAnim.gameObject.SetActive(false);

            _currentOffset = new Vector2(offset, 0);
        }

        public void FaceAttack(Vector2 direction)
        {
            _currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _currentOffset = direction * offset;
        }

        private IEnumerator AttackCoroutine()
        {
            _attackAnim.gameObject.SetActive(true);
            _effectAnim.gameObject.SetActive(true);
            _attackAnim.Play("Attack");
            _effectAnim.Play("Slash");
            yield return new WaitForSeconds(_effectAnim.GetCurrentAnimatorStateInfo(0).length);
            _attackAnim.gameObject.SetActive(false);
            _effectAnim.gameObject.SetActive(false);
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

            // 1. 기즈모가 그려질 중심 위치 계산
            Vector3 position = (Vector2)transform.position + _currentOffset;

            // 2. 원하는 회전값 계산 (2D 게임에서는 보통 Z축 회전을 사용합니다)
            Quaternion rotation = Quaternion.Euler(0, 0, _currentAngle);

            // 3. 크기 (스케일은 기본값인 1, 1, 1을 사용)
            Vector3 scale = hitboxSize;

            // 4. TRS(Position, Rotation, Scale) 매트릭스를 새로 생성하여 적용합니다.
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, scale);

            // 5. 기즈모를 그립니다.
            // ★ 중요 ★: 매트릭스 안에 이미 '위치(position)'와 '회전' 정보가 모두 들어갔기 때문에, 
            // 여기서는 기준점인 원점(Vector3.zero)을 넣어주어야 원하는 위치에 올바르게 그려집니다.
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, hitboxSize);

            Gizmos.matrix = originalMatrix;
        }
    }
}