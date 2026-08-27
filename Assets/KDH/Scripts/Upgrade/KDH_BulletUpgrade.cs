using System.Collections.Generic;
using UnityEngine;

namespace KDH.Scripts.Upgrade
{
    public class KDH_BulletUpgrade : MonoBehaviour
    {
        public List<KDH_AbstractBullet> upgradedBullets;

        public void HitBullet(Collider2D collision)
        {
            foreach (var upgradedBullet in upgradedBullets)
            {
                upgradedBullet.Ability(collision);
            }
        }
    }
}