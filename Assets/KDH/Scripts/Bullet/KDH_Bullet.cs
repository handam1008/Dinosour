using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_Bullet : MonoBehaviour
    {
        [Header("Bullet Modules")] // 여기서 증강으로 먹은 속성의 능력을 모듈로 추가해서 적용
        [field: SerializeField] public KDH_BulletMovement MoveComp { get; private set; }
        
        [Header("Bullet Settings")]
        [field: SerializeField] public float Speed { get; private set; } = 20f;
        
        public Vector2 MoveDir { get; private set; }
        public Rigidbody2D Rigid { get; private set; }

        public bool IsUpgraded { get; set; }
        
        private void Awake()
        {
            Rigid = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            Rigid.linearVelocity = MoveDir * Speed;
        }

        private void FixedUpdate()
        {
            MoveComp.Movement(Rigid);
        }

        public void Init(Vector2 moveDir)
        {
            MoveDir = moveDir;
        }
    }
}
