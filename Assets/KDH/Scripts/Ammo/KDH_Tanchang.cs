using UnityEngine;

public class KDH_Tanchang : MonoBehaviour
{
    [SerializeField] private GameObject[] ammoPrefabs;
    [SerializeField] private int maxAmmo;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float reloadSpeed; // 한 번에 모든 Ammo를 장정하는 형식
    [SerializeField] private float chargeSpeed;
    private KDH_Ammo[] _ammos;

    private void Awake()
    {
        for (int i = 0; i < ammoPrefabs.Length; i++)
        {
            ammoPrefabs[i].GetComponent<KDH_Ammo>();
        }
    }

    private void Start()
    {
        _ammos = new KDH_Ammo[maxAmmo];
    }
}
