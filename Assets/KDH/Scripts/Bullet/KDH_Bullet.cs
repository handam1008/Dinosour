using System;
using KDH.Scripts.Upgrade;
using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_Bullet : MonoBehaviour
    {
        [Header("Bullet Modules")]
        [field: SerializeField] public KDH_BulletMovement MoveComp { get; private set; }
        [field: SerializeField] public KDH_DamageCaster DamageCasterCompo { get; private set; }
        
        [field: SerializeField] public KDH_BulletUpgrade BulletUpgrade { get; private set; }
        [Header("Bullet Settings")]
        [field: SerializeField] public float Speed { get; private set; } = 20f;
        [field: SerializeField] public float Damage { get; private set; } = 15f;
        
        public Vector2 MoveDir { get; private set; }
        public Rigidbody2D Rigid { get; private set; }

        public bool IsUpgraded { get; set; }
        
        private void Awake()
        {
            Rigid = GetComponent<Rigidbody2D>();
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
