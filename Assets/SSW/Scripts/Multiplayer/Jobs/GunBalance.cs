using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Game/Gun Balance", fileName = "GunBalance")]
    public sealed class GunBalance : ScriptableObject
    {
        [SerializeField] float _fireDamage;
        [SerializeField] float _gravityForce;
        [SerializeField] float _iceSlow;
        [SerializeField] float _poisonDamage;
        [SerializeField] float _shurikenDamage;

        public float FireDamage => _fireDamage;
        public float GravityForce => _gravityForce;
        public float IceSlow => _iceSlow;
        public float PoisonDamage => _poisonDamage;
        public float ShurikenDamage => _shurikenDamage;

#if UNITY_EDITOR
        public bool Replace(float fire, float gravity, float ice, float poison, float shuriken)
        {
            if (_fireDamage.Equals(fire) && _gravityForce.Equals(gravity) && _iceSlow.Equals(ice)
                && _poisonDamage.Equals(poison) && _shurikenDamage.Equals(shuriken)) return false;
            _fireDamage = fire;
            _gravityForce = gravity;
            _iceSlow = ice;
            _poisonDamage = poison;
            _shurikenDamage = shuriken;
            return true;
        }
#endif
    }
}
