using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_RideBlock : MonoBehaviour
    {
        void Update()
        {
            transform.rotation = Quaternion.identity;
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            collision.transform.SetParent(transform);
            collision.transform.localScale = new Vector3(1, 1, 1);
            collision.transform.rotation = Quaternion.identity;
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            collision.transform.SetParent(null);
        }
    }
}
