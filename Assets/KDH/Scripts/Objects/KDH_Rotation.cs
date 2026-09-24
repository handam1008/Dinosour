using UnityEngine;

namespace KDH.Scripts.Objects
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KDH_Rotation : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed;

        private Rigidbody2D _rb;
        
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