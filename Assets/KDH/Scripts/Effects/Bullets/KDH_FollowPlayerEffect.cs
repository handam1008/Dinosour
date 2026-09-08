using UnityEngine;

namespace KDH.Scripts.Effects.Bullets
{
    public class KDH_FollowPlayerEffect : MonoBehaviour
    {
        public bool MoveEffect { get; set; }
        private Transform _target;

        private void Update()
        {
            if (MoveEffect)
                Move();
        }

        public void Init(Transform target)
        {
            _target = target;
        }
        
        private void Move()
        {
            transform.position = _target.position;
        }
    }
}