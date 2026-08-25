using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_Bullet : MonoBehaviour
    {
        [Header("Bullet Modules")]
        [field: SerializeField] public KDH_BulletMovement MoveComp { get; private set; }
        
        [Header("Bullet Settings")]
        [field: SerializeField] public float Speed { get; private set; } = 20f;
        
        public Vector2 MoveDir { get; private set; }
        public Rigidbody2D Rigid { get; private set; }

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
