using UnityEngine;

namespace SSW
{
    public sealed class MotionCast
    {
        readonly CapsuleCollider2D _shape;
        readonly ContactFilter2D _filter;
        readonly RaycastHit2D[] _hits = new RaycastHit2D[16];
        Collider2D _lastHit;
        const float Skin = 0.015f;

        public MotionCast(CapsuleCollider2D shape, LayerMask ground)
        {
            _shape = shape;
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = ground, useTriggers = false };
        }

        public bool Grounded(Vector2 position, bool dropping) => Distance(position, Vector2.down, 0.08f, dropping) < 0.08f;

        public bool Platform(Vector2 position)
        {
            Distance(position, Vector2.down, 0.08f, false);
            return _lastHit != null && _lastHit.TryGetComponent<PlatformEffector2D>(out var platform) && platform.useOneWay;
        }

        public void Move(ref Vector2 position, ref Vector2 velocity, float delta, bool dropping)
        {
            Axis(ref position, ref velocity, Vector2.right, velocity.x * delta, dropping);
            Axis(ref position, ref velocity, Vector2.up, velocity.y * delta, dropping);
        }

        void Axis(ref Vector2 position, ref Vector2 velocity, Vector2 axis, float distance, bool dropping)
        {
            if (Mathf.Abs(distance) < 0.00001f) return;
            Vector2 direction = axis * Mathf.Sign(distance);
            float travel = Mathf.Abs(distance);
            float allowed = Distance(position, direction, travel + Skin, dropping);
            if (allowed < travel + Skin)
            {
                travel = Mathf.Max(0f, allowed - Skin);
                if (axis.x != 0f) velocity.x = 0f;
                else velocity.y = 0f;
            }
            position += direction * travel;
        }

        float Distance(Vector2 position, Vector2 direction, float distance, bool dropping)
        {
            Vector2 scale = _shape.transform.lossyScale;
            scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            Vector2 size = Vector2.Scale(_shape.size, scale);
            size = Vector2.Max(Vector2.one * 0.01f, size - Vector2.one * Skin * 2f);
            Vector2 center = position + Vector2.Scale(_shape.offset, scale);
            int count = Physics2D.CapsuleCast(center, size, _shape.direction, 0f, direction, _filter, _hits, distance);
            float result = distance;
            _lastHit = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];
                if (hit.collider == _shape || hit.distance <= 0f) continue;
                if (hit.collider.TryGetComponent<PlatformEffector2D>(out var platform) && platform.useOneWay
                    && (dropping || direction.y >= 0f || center.y - size.y * 0.5f < hit.collider.bounds.max.y - 0.08f)) continue;
                if (Vector2.Dot(hit.normal, direction) < -0.1f && hit.distance < result)
                {
                    result = hit.distance;
                    _lastHit = hit.collider;
                }
            }
            return result;
        }
    }
}
