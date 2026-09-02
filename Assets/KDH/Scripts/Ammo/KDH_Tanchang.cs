using KDH.Scripts.Gun;
using UnityEngine;

namespace KDH.Scripts.Ammo
{
    public class KDH_Tanchang : MonoBehaviour
    {
        public void Init(KDH_Gun gun)
        {
            gun.Ammos = new KDH_Ammo[gun.MaxAmmo];
            
            for (int i = 0; i < gun.AmmoPrefabs.Length; i++)
                gun.Ammos[i] = gun.Tanchang.gameObject.transform.GetChild(i).GetComponent<KDH_Ammo>();
        }

        public void UseAmmo(KDH_Gun gun)
        {
            if (gun.CurrentAmmo <= 0) return;

            gun.CurrentAmmo--;
            gun.AmmoPrefabs[gun.CurrentAmmo].gameObject.SetActive(false);
        }
    }
}
