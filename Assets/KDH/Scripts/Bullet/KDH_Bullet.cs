using KDH.Scripts.Gun;
using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_Bullet : MonoBehaviour
    {
        [Header("Bullet Modules")]
        [field: SerializeField] public KDH_BulletMovement MoveComp { get; private set; }
        [field: SerializeField] public KDH_DamageCaster DamageCasterCompo { get; private set; }
        [Header("Bullet Settings")]
        [field: SerializeField] public float Speed { get; private set; } = 20f;
        [field: SerializeField] public float Damage { get; set; } = 15f;
        [field: SerializeField] public float UpgradValue { get; private set; } = 2f;
        
        public Vector2 MoveDir { get; private set; }
        public Rigidbody2D Rigid { get; private set; }

        public bool IsUpgraded { get; set; }

        public KDH_Gun PlayerGun {get; private set;}
        
        private void Awake()
        {
            Rigid = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            if (PlayerGun == null)
                PlayerGun = FindAnyObjectByType<KDH_Gun>();
        }
        
        private void OnEnable()
        {
            DamageCasterCompo.Init(this);

            InitDirection();
        }

        private void FixedUpdate()
        {
            MoveComp.Movement(Rigid);
        }

        private void InitDirection()
        {
            Rigid.linearVelocity = MoveDir * Speed;
        }
        
        public void Init(Vector2 moveDir)
        {
            MoveDir = moveDir;
        }
    }
}
