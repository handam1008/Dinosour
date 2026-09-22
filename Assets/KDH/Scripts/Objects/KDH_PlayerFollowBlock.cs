using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    [RequireComponent(typeof(KDH_GearChildRotate))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class KDH_PlayerFollowBlock : MonoBehaviour
    {
        [SerializeField] private float topContactMinNormalY = 0.5f;

        private KDH_GearChildRotate _platform;
        private Rigidbody2D _platformRb;
        private readonly HashSet<Rigidbody2D> _riders = new HashSet<Rigidbody2D>();

        private void Awake()
        {
            _platform = GetComponent<KDH_GearChildRotate>();
            _platformRb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (_riders.Count == 0) return;

            float deltaAngle = _platform.RotationSpeed * Time.fixedDeltaTime;
            Vector2 pivot = _platformRb.position;

            foreach (var rider in _riders)
            {
                if (rider == null) continue;

                Vector2 offset = rider.position - pivot;
                Vector2 rotatedOffset = Rotate(offset, deltaAngle);
                rider.MovePosition(pivot + rotatedOffset);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.rigidbody == null) return;
            if (!collision.gameObject.TryGetComponent(out PlayerController _)) return;
            if (!IsLandedOnTop(collision)) return;

            _riders.Add(collision.rigidbody);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.rigidbody != null)
            {
                _riders.Remove(collision.rigidbody);
            }
        }

        private bool IsLandedOnTop(Collision2D collision)
        {
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y >= topContactMinNormalY) return true;
            }
            return false;
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