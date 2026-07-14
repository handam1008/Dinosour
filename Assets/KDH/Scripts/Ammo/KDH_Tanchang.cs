using UnityEngine;

public class KDH_Tanchang : MonoBehaviour
{
    [SerializeField] private GameObject[] ammoPrefabs;
    private KDH_Ammo[] _ammos = new KDH_Ammo[6];

    private void Awake()
    {
        for (int i = 0; i < ammoPrefabs.Length; i++)
        {
            ammoPrefabs[i].GetComponent<KDH_Ammo>();
        }
    }
}
