using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_ChangeParent : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out PlayerController _))
            {
                collision.transform.SetParent(transform);
            }
        }
    
        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out PlayerController _))
            {
                collision.transform.SetParent(null);
            }
        }
    }
}
