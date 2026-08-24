using UnityEngine;

namespace NKY.Scripts
{
    public abstract class AbstractMeleeWeapon : AbstractWeapon
    {
        [SerializeField] protected float offset;
        [SerializeField] protected Vector2 hitboxSize;
        
        protected Vector2 _currentOffset;

        public override void FaceAttack(Vector2 direction)
        {
            base.FaceAttack(direction);
            _currentOffset = direction * offset;
        }
    }
}