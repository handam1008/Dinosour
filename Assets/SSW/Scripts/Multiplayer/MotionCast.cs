using UnityEngine;

namespace SSW
{
    public sealed class MotionCast
    {
        readonly CapsuleCollider2D _shape;
        readonly ContactFilter2D _filter;
        readonly Vector3 _baseScale;
        readonly Collider2D[] _overlaps = new Collider2D[16];
        readonly RaycastHit2D[] _hits = new RaycastHit2D[32];
        Collider2D _lastHit;
        Vector2 _lastPoint;
        Vector2 _lastNormal;
        const float Skin = 0.015f;
        const float GroundReach = GroundProbe.Reach;

        public MotionCast(CapsuleCollider2D shape, LayerMask ground)
        {
            _shape = shape;
            _baseScale = shape.transform.localScale;
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = ground, useTriggers = false };
        }

        public void Scale(ref MotionState state, float factor)
        {
            float previous = state.Scale > 0f ? state.Scale : 1f;
            float foot = (_shape.offset.y - _shape.size.y * 0.5f) * _baseScale.y;
            Vector2 point = state.Position + Vector2.up * (foot * (previous - factor));
            if (factor > previous)
            {
                Vector2 scale = Vector2.Scale(_baseScale, Vector2.one * factor);
                Vector2 size = Vector2.Scale(_shape.size, scale) - Vector2.one * Skin * 2f;
                Vector2 center = point + Vector2.Scale(_shape.offset, scale);
                int count = Physics2D.OverlapCapsule(center, size, _shape.direction, 0f, _filter, _overlaps);
                for (int i = 0; i < count; i++)
                {
                    Collider2D hit = _overlaps[i];
                    if (hit == _shape || hit.bounds.max.y <= center.y - size.y * 0.5f + GroundReach
                        || GroundProbe.IsPlatform(hit)) continue;
                    factor = previous;
                    point = state.Position;
                    break;
                }
            }
            state.Scale = factor;
            state.Position = point;
            _shape.transform.localScale = _baseScale * factor;
        }

        public float PlatformHeight => _lastHit.bounds.max.y;

        public bool Grounded(Vector2 position, bool dropping, float dropTop = 0f)
        {
            Distance(position, Vector2.down, GroundReach, dropping, dropTop, groundOnly: true);
            return _lastHit != null;
        }

        public bool Platform(Vector2 position)
        {
            Distance(position, Vector2.down, GroundReach, false, groundOnly: true);
            return _lastHit != null && GroundProbe.IsPlatform(_lastHit);
        }

        public Vector2 Carry(ref Vector2 position, Vector2 previous, float delta, bool dropping, float dropTop)
        {
            float lift = Mathf.Clamp(previous.y * delta, 0f, 0.5f);
            float reach = GroundReach + lift + Mathf.Clamp(-previous.y * delta, 0f, 0.5f);
            float distance = Distance(position + Vector2.up * lift, Vector2.down, reach, dropping, dropTop, groundOnly: true);
            Collider2D ground = _lastHit;
            if (ground == null) return Vector2.zero;
            Rigidbody2D body = ground.attachedRigidbody;
            if (body == null || body.bodyType == RigidbodyType2D.Static) return Vector2.zero;

            Vector2 velocity = body.GetPointVelocity(_lastPoint);
            Vector2 before = position;
            Vector2 allowed = velocity;
            Axis(ref position, ref allowed, Vector2.right, velocity.x * delta, dropping, dropTop, ground);
            Axis(ref position, ref allowed, Vector2.up, lift + Skin - distance, dropping, dropTop, ground);
            velocity.x = (position.x - before.x) / delta;
            return velocity;
        }

        public void Move(ref Vector2 position, ref Vector2 velocity, float delta, bool dropping, float dropTop = 0f, bool followGround = false)
        {
            float slope = followGround && Grounded(position, dropping, dropTop) ? -_lastNormal.x / _lastNormal.y : 0f;
            Vector2 start = position;
            float rise = Mathf.Max(0f, velocity.x * delta * slope);
            if (rise > 0f)
            {
                Axis(ref position, ref velocity, Vector2.up, rise, dropping, dropTop);
                if (position.y - start.y < rise - 0.0001f) velocity.x = 0f;
            }
            Axis(ref position, ref velocity, Vector2.right, velocity.x * delta, dropping, dropTop);
            float vertical = velocity.y * delta;
            if (Mathf.Abs(slope) > 0.001f)
            {
                float descent = Mathf.Max(0f, position.y - start.y)
                    + Mathf.Max(0f, -slope * (position.x - start.x));
                float distance = Distance(position, Vector2.down, descent + GroundReach, dropping, dropTop, groundOnly: true);
                if (_lastHit != null) vertical = Mathf.Min(vertical, Skin - distance);
            }
            Axis(ref position, ref velocity, Vector2.up, vertical, dropping, dropTop);
        }

        void Axis(ref Vector2 position, ref Vector2 velocity, Vector2 axis, float distance,
            bool dropping, float dropTop = 0f, Collider2D ignored = null)
        {
            if (Mathf.Abs(distance) < 0.00001f) return;
            Vector2 direction = axis * Mathf.Sign(distance);
            float travel = Mathf.Abs(distance);
            float allowed = Distance(position, direction, travel + Skin, dropping, dropTop, ignored);
            if (_lastHit != null)
            {
                travel = Mathf.Max(0f, allowed - Skin);
                if (axis.x != 0f) velocity.x = 0f;
                else velocity.y = 0f;
            }
            position += direction * travel;
        }

        float Distance(Vector2 position, Vector2 direction, float distance, bool dropping,
            float dropTop = 0f, Collider2D ignored = null, bool groundOnly = false)
        {
            Vector2 scale = _shape.transform.lossyScale;
            scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            Vector2 size = Vector2.Scale(_shape.size, scale);
            size = Vector2.Max(Vector2.one * 0.01f, size - Vector2.one * Skin * 2f);
            Vector2 center = position + (Vector2)_shape.transform.TransformVector(_shape.offset);
            int count = Physics2D.CapsuleCast(center, size, _shape.direction,
                _shape.transform.eulerAngles.z, direction, _filter, _hits, distance);
            float result = distance;
            _lastHit = null;
            _lastPoint = _lastNormal = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];
                if (hit.collider == _shape || hit.collider == ignored || hit.distance <= 0f) continue;
                if (groundOnly && hit.normal.y < GroundProbe.MinNormal) continue;
                if (GroundProbe.IsPlatform(hit.collider)
                    && ((dropping && Mathf.Abs(hit.collider.bounds.max.y - dropTop) < 0.1f)
                        || direction.y >= 0f || hit.normal.y < GroundProbe.MinNormal)) continue;
                if (Vector2.Dot(hit.normal, direction) < -0.1f && hit.distance < result)
                {
                    result = hit.distance;
                    _lastHit = hit.collider;
                    _lastPoint = hit.point;
                    _lastNormal = hit.normal;
                }
            }
            return result;
        }
    }
}
