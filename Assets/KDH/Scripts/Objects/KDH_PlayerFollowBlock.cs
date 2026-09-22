using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_PlayerFollowBlock : MonoBehaviour
    {
        [SerializeField] private Transform parent; // 원점
        
        private void CalcualtePosition(Vector3 playerPosition, Transform playerTrm)
        {
            Vector2 dir = parent.position - playerPosition;
            float dis = dir.magnitude;
            float angle = parent.eulerAngles.z;
            float triY = Mathf.Sin(angle) * dis;
            float triX = Mathf.Cos(angle) * dis;
            float distance = Mathf.Pow(triX, 2) + Mathf.Pow(triY, 2);
            
            
            playerTrm.position += new Vector3(dir.x, 0, dir.y) * distance;
        }
        
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
