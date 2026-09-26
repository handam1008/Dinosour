using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Objects
{
    public class KDH_Waterfall : MonoBehaviour
    {
        [SerializeField] private float relationTime;
        [SerializeField] private float duration;
        [SerializeField] private float force;
        private Vector2 _direction;

        private float _mainTimer;
        private float _suvTimer;
        
        public bool CanWaterfall { get; private set; }
        
        private void Awake()
        {
            _direction.y = force;
        }

        void Update()
        {
            _mainTimer += Time.deltaTime;

            if (_mainTimer > relationTime)
            {
                CanWaterfall = true;
                _suvTimer +=  Time.deltaTime;

                if (_suvTimer > duration && CanWaterfall)
                {
                    _suvTimer = 0;
                    _mainTimer = 0;
                    CanWaterfall = false;
                }
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!CanWaterfall) return;
            
            if (collision.TryGetComponent(out Rigidbody2D rigidBody))
                rigidBody.AddForceY(_direction.y,  ForceMode2D.Impulse);
        }
    }
}
