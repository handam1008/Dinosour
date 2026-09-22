using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_PlayerFollowBlock : MonoBehaviour
    {
        // private void CalcualtePosition(Vector2 )
        // {
        //     
        // }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent(out PlayerController _))
            {
                collision.transform.SetParent(transform);
            }
        }
    
        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent(out PlayerController _))
            {
                collision.transform.SetParent(null);
            }
        }
    }
}
