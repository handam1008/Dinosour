using UnityEngine;

namespace KDH.Scripts.Ammo
{
    public class KDH_Ammo : MonoBehaviour
    {
        [SerializeField] private KDH_Tanchang tanchang;
        [SerializeField] private Transform ammoPivot;
        private float gageValue = 0.1f;
        private float maxGageValue = 3;

        public bool WasUpgraded { get; private set; }
        
        private void OnEnable()
        {
            gageValue = 0;
        }

        private void Update()
        {
            gageValue += Time.deltaTime;
            gageValue = Mathf.Min(gageValue, maxGageValue);
            Gage();

            WasUpgraded = gageValue >= maxGageValue;
        }

        private void Gage()
        {
            Vector3 size = ammoPivot.localScale;
            size.x = GagePer();
            ammoPivot.localScale = size;
        }

        private float GagePer()
        {
            return gageValue / maxGageValue;
        }
    }
}
