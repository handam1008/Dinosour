using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public abstract class KDH_AbstractBulletAbility : MonoBehaviour
    {
        public abstract void BulletAbility(Collider2D collision);
    }
}