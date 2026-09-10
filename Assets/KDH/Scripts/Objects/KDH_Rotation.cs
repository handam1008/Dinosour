using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_Rotation : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed; // 초당 회전 각도
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