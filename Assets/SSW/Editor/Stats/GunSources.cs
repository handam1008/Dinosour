using KDH.Scripts.Upgrade.Instances.Bullets;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Game/Gun Sources", fileName = "GunSources")]
    public sealed class GunSources : ScriptableObject
    {
        [SerializeField] GunBalance _balance;
        [SerializeField] KDH_FireBullet _fire;
        [SerializeField] KDH_GravityBullet _gravity;
        [SerializeField] KDH_IceBullet _ice;
        [SerializeField] KDH_PoisonBullet _poison;
        [SerializeField] KDH_ShurikenBullet _shuriken;

        public GunBalance Balance => _balance;
        public KDH_FireBullet Fire => _fire;
        public KDH_GravityBullet Gravity => _gravity;
        public KDH_IceBullet Ice => _ice;
        public KDH_PoisonBullet Poison => _poison;
        public KDH_ShurikenBullet Shuriken => _shuriken;
    }
}
