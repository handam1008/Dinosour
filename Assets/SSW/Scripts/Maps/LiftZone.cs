using UnityEngine;

namespace SSW
{
    public sealed class LiftZone : MonoBehaviour, ILiftZone
    {
        [SerializeField, Min(0f)] float _riseSpeed = 8f;
        [SerializeField, Min(0f)] float _acceleration = 90f;

        public bool Active => isActiveAndEnabled;
        public float RiseSpeed => _riseSpeed;
        public float Acceleration => _acceleration;

        void OnTriggerStay2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;
            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.MoveTowards(velocity.y, _riseSpeed, _acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }
    }
}
