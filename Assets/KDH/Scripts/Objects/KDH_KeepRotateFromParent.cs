using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_KeepRotateFromParent : MonoBehaviour
    {
        private Quaternion _targetRotation = Quaternion.identity;
        
        private void LateUpdate()
        {
            transform.rotation = _targetRotation;
        }
    }
}
