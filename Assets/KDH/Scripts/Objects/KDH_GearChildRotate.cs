using UnityEngine;

namespace KDH.Scripts.Objects
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KDH_GearChildRotate : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed;

        private Rigidbody2D _rb;

        public float RotationSpeed => rotationSpeed;
        public Vector2 Pivot => _rb.position;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            _rb.MoveRotation(_rb.rotation + rotationSpeed * Time.fixedDeltaTime);
        }
    }
}