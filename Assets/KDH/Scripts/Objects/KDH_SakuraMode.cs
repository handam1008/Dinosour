using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_SakuraMode : MonoBehaviour
    {
        [SerializeField] private float timeApplySpeed;
        [SerializeField] private float speedAmount;

        void OnParticleCollision(GameObject go)
        {
            if (go.TryGetComponent(out ISpeedable speedable))
            {
                speedable.ApplySpeed(speedAmount, timeApplySpeed);
            }
        }
    }
}
