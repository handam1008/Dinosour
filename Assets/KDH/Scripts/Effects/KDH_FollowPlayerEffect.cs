using UnityEngine;

namespace KDH.Scripts.Effects
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KDH_FollowPlayerEffect : MonoBehaviour
    {
        public bool MoveEffect { get; set; }

        private Rigidbody2D _rigid;
        private Transform _target;

        private void Awake()
        {
            _rigid = GetComponent<Rigidbody2D>();
        }
        
        private void FixedUpdate()
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
            _rigid.MovePosition(_target.position);
        }
    }
}