using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_KeepRotateFromParent : MonoBehaviour
    {
        [SerializeField] private Vector3 targetEulerAngles = Vector3.zero;

        private Transform _parent;
        private Rigidbody2D _rb;
        private Vector2 _offsetFromParent;
        private float _lastParentAngle;

        private void Awake()
        {
            _parent = transform.parent;
            _rb = GetComponent<Rigidbody2D>();

            if (_parent != null)
            {
                _lastParentAngle = _parent.eulerAngles.z;
                _offsetFromParent = (Vector2)transform.position - (Vector2)_parent.position;
            }
        }

        private void FixedUpdate()
        {
            if (_parent == null) return;

            float currentParentAngle = _parent.eulerAngles.z;
            float deltaAngle = Mathf.DeltaAngle(_lastParentAngle, currentParentAngle);
            _lastParentAngle = currentParentAngle;

            if (Mathf.Approximately(deltaAngle, 0f)) return;

            _offsetFromParent = Rotate(_offsetFromParent, deltaAngle);
            Vector2 newPosition = (Vector2)_parent.position + _offsetFromParent;

            if (_rb != null)
            {
                _rb.MovePosition(newPosition);
            }
            else
            {
                transform.position = newPosition;
            }
        }

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(targetEulerAngles);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}