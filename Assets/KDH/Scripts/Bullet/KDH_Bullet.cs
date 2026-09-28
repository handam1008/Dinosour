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
        [SerializeField] Sprite normalBulletSprite; // 추가
        [SerializeField] Sprite upgradedBulletSprite; // 추가
        
        public Vector2 MoveDir { get; private set; }
        private SpriteRenderer bulletSpriteRenderer; // 추가
        public Rigidbody2D Rigid { get; private set; }

        public bool IsUpgraded { get; set; }

        public KDH_Gun PlayerGun {get; private set;}
        
        private void Awake()
        {
            bulletSpriteRenderer = GetComponent<SpriteRenderer>(); // 추가
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
            
            if (IsUpgraded) // 추가
                bulletSpriteRenderer.sprite = upgradedBulletSprite; // 추가
            else // 추가
                bulletSpriteRenderer.sprite = normalBulletSprite; // 추가
        }

        private void FixedUpdate()
        {
            MoveComp.Movement(Rigid);
        }

        private void InitDirection()
        {
            Rigid.linearVelocity = MoveDir * Speed;
        }
        
        public void Init(float amount)
        {
            float angle = amount * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            MoveDir = direction;
        }
    }
}
