using UnityEngine;

namespace KDH.Scripts.Bullet
{
    public class KDH_BulletMovement : MonoBehaviour
    {
        public void Movement(Rigidbody2D rigid)
        {
            float angle = Mathf.Atan2(rigid.linearVelocity.y, rigid.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
