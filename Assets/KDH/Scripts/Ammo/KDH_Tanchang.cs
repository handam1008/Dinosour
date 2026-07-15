using System;
using UnityEngine;

public class KDH_Tanchang : MonoBehaviour
{
    public GameObject[] ammoPrefabs { get; private set; }
    [SerializeField] private KDH_Shoot playerShoot;
    [SerializeField] private int maxAmmo;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float reloadSpeed; // 한 번에 모든 Ammo를 장정하는 형식
    [SerializeField] private float chargeSpeed;
    private KDH_Ammo[] _ammos;
    private int currentAmmo = 0;
    private float _timer = 0f;

    private void Awake()
    {
        for(int i = 0; i < ammoPrefabs.Length; i++)
            ammoPrefabs[i].GetComponent<KDH_Ammo>();
    }

    private void OnEnable()
    {
        playerShoot.PlayerShoot += UseAmmo;
    }

    private void OnDisable()
    {
        playerShoot.PlayerShoot -= UseAmmo;
    }

    private void Start()
    {
        _ammos = new KDH_Ammo[maxAmmo];
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= chargeSpeed)
        {
            ammoPrefabs[currentAmmo].SetActive(true);
            _timer = 0f;
        }
    }

    private void UseAmmo()
    {
        ammoPrefabs[maxAmmo - currentAmmo - 1].SetActive(false);
        KDH_Ammo ammoScript =  ammoPrefabs[currentAmmo].GetComponent<KDH_Ammo>();
        
        currentAmmo++;
    }
}
