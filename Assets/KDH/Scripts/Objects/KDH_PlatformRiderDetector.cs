using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_PlatformRiderDetector : MonoBehaviour
    {
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.TryGetComponent(out PlayerController _))
            {
                other.transform.SetParent(transform);
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (other.gameObject.TryGetComponent(out PlayerController _))
            {
                other.transform.SetParent(null);
            }
        }
    }
}
