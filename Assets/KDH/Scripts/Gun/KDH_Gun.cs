using System;
using KDH.Scripts.Ammo;
using KDH.Scripts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KDH.Scripts.Gun
{
    public class KDH_Gun : MonoBehaviour
    {
        public event Action PlayerShoot;

        [Header("Others")]
        [field: SerializeField] public PlayerInput PlayerInput { get; private set; }    
        [field: SerializeField] public Camera Cam { get; private set; }

        [Header("Gun Parts")]
        [field: SerializeField] public Transform Visual { get; private set; }
        [field: SerializeField] public KDH_SpawnBullet SpawnBullet { get; private set; }
        [field: SerializeField] public Transform GunPos { get; private set; }
        
        [Header("Bullet Info Controls")]
        [field: SerializeField] public GameObject BulletPrefab { get; private set; }
        [field: SerializeField] public GameObject[]  AmmoPrefabs { get; private set; }
        [field: SerializeField] public int MaxAmmo { get; private set; }
        [field: SerializeField] public float AttackSpeed { get; private set; }
        [field: SerializeField] public float ReloadSpeed { get; private set; } // 한 번에 모든 Ammo를 장정하는 형식
        [field: SerializeField] public float ChargeSpeed { get; private set; }
        public KDH_Ammo[] Ammos {get; set;}
        
        [Header("Modules")]
        [SerializeField] private KDH_PlayerAttackModule playerAttackModule;
        [field: SerializeField] public KDH_Tanchang Tanchang {get; private set;}

        [field: SerializeField] public int CurrentAmmo { get; set; } = 0;
        [field: SerializeField] public float ChargeTimer { get; set; } = 0f;

        [field: SerializeField] public bool CanAttack { get; private set; }
        
        private void Awake()
        {
            Cam = Camera.main;
        }

        private void Start()
        {
            Tanchang.Init(this);
            SpawnBullet.CreateBullet(BulletPrefab, this);
        }
        
        private void OnEnable()
        {
            PlayerShoot += HandleShoot;
            PlayerShoot += HandleChangeAmmo;
        }


        private void OnDisable()
        {
            PlayerShoot -= HandleShoot;
            PlayerShoot -= HandleChangeAmmo;
        }

        private void Update()
        {
            playerAttackModule.FollowMouse(Cam, Visual);

            ChargeAmmo();
            CheckCanAttack();
        }

        private void ChargeAmmo()
        {
            ChargeTimer += Time.deltaTime;

            if (ChargeTimer >= ChargeSpeed && CurrentAmmo < Ammos.Length)
            {
                AmmoPrefabs[CurrentAmmo].SetActive(true);
                CurrentAmmo++;
                ChargeTimer = 0f;
            }
        }

        private void CheckCanAttack()
        {
            CanAttack = CurrentAmmo > 0;
        }
        
        public void OnAttack()
        {
            PlayerShoot?.Invoke();
        }

        private void HandleShoot()
        {
            if (CanAttack)
            {
                bool isUpgraded = Ammos[CurrentAmmo - 1].WasUpgraded; 
                playerAttackModule.Shoot(SpawnBullet, this, Cam, Visual, GunPos, isUpgraded);
            }
        }

        private void HandleChangeAmmo()
        {
            if (CanAttack)
                Tanchang.UseAmmo(this);
        }
    }
}   
