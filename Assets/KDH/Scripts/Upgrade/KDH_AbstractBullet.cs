using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public abstract class KDH_AbstractBullet : MonoBehaviour
    {
        public abstract void Ability(Collider2D collision);
    }
}