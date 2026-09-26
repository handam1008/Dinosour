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

        public void Recover(ref Vector2 position, ref Vector2 velocity, bool dropping, float dropTop, Vector2 surface = default)
        {
            Vector2 scale = _shape.transform.lossyScale;
            scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            Vector2 size = Vector2.Scale(_shape.size, scale);
            Vector2 offset = _shape.transform.TransformVector(_shape.offset);
            float angle = _shape.transform.eulerAngles.z;
            for (int pass = 0; pass < 4; pass++)
            {
                int count = Physics2D.OverlapCapsule(position + offset, size, _shape.direction, angle, _filter, _overlaps);
                bool moved = false;
                for (int i = 0; i < count; i++)
                {
                    Collider2D hit = _overlaps[i];
                    if (hit == _shape || hit.attachedRigidbody == _shape.attachedRigidbody) continue;
                    ColliderDistance2D contact = Contact(position, hit);
                    if (!contact.isValid || !contact.isOverlapped) continue;
                    Vector2 normal = -contact.normal;
                    if (GroundProbe.IsPlatform(hit) && (velocity.y > 0f || normal.y < GroundProbe.MinNormal
                        || dropping && Mathf.Abs(hit.bounds.max.y - dropTop) < 0.1f)) continue;
                    position += normal * (-contact.distance + 0.001f);
                    float inward = Vector2.Dot(velocity - surface, normal);
                    if (inward < 0f)
                    {
                        if (normal.y >= GroundProbe.MinNormal && velocity.y <= surface.y) velocity.y = surface.y;
                        else velocity -= normal * inward;
                    }
                    moved = true;
                }
                if (!moved) break;
            }
        }

        ColliderDistance2D Contact(Vector2 position, Collider2D other)
        {
            Rigidbody2D body = _shape.attachedRigidbody;
            Vector2 saved = body.position;
            try
            {
                body.position = position;
                return _shape.Distance(other);
            }
            finally { body.position = saved; }
        }

        public bool TryPlace(Vector2 target, Vector2 normal, float radius, out Vector2 position)
        {
            Vector2 scale = _shape.transform.lossyScale;
            scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            Vector2 size = Vector2.Scale(_shape.size, scale);
            Vector2 offset = _shape.transform.TransformVector(_shape.offset);
            float angle = _shape.transform.eulerAngles.z;
            position = target;
            if (normal.sqrMagnitude > 0.001f)
            {
                normal.Normalize();
                Vector2 axis = _shape.direction == CapsuleDirection2D.Vertical ? _shape.transform.up : _shape.transform.right;
                float half = Mathf.Max(size.x, size.y) * 0.5f;
                float round = Mathf.Min(size.x, size.y) * 0.5f;
                float extent = round + (half - round) * Mathf.Abs(Vector2.Dot(axis, normal));
                position += normal * Mathf.Max(0f, extent - Vector2.Dot(offset, normal) - radius + Skin);
            }
            Vector2 velocity = Vector2.zero;
            Recover(ref position, ref velocity, false, 0f);
            int count = Physics2D.OverlapCapsule(position + offset, size, _shape.direction, angle, _filter, _overlaps);
            if (count == _overlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _overlaps[i];
                if (hit == _shape || hit.attachedRigidbody == _shape.attachedRigidbody) continue;
                ColliderDistance2D contact = Contact(position, hit);
                if (!contact.isValid || contact.distance >= -0.001f) continue;
                if (GroundProbe.IsPlatform(hit) && -contact.normal.y < GroundProbe.MinNormal) continue;
                return false;
            }
            return true;
        }

        public float PlatformHeight => _lastHit.bounds.max.y;

        public void Ride(Vector2 position, Vector2 velocity, float gravity, float delta, bool landing)
        {
            Distance(position, Vector2.down, GroundReach, false, groundOnly: true);
            if (_lastHit != null && _lastHit.TryGetComponent<IMapRider>(out var rider))
                rider.Ride(_lastPoint, velocity, _shape.attachedRigidbody.mass, gravity, delta, landing);
        }

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

            float margin = Margin(Vector2.down);
            Vector2 velocity = body.GetPointVelocity(_lastPoint);
            Vector2 before = position;
            Vector2 allowed = velocity;
            Axis(ref position, ref allowed, Vector2.right, velocity.x * delta, dropping, dropTop, ground);
            Axis(ref position, ref allowed, Vector2.up, lift + margin - distance, dropping, dropTop, ground);
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
                if (_lastHit != null) vertical = Mathf.Min(vertical, Margin(Vector2.down) - distance);
            }
            Axis(ref position, ref velocity, Vector2.up, vertical, dropping, dropTop);
        }

        void Axis(ref Vector2 position, ref Vector2 velocity, Vector2 axis, float distance,
            bool dropping, float dropTop = 0f, Collider2D ignored = null)
        {
            if (Mathf.Abs(distance) < 0.00001f) return;
            Vector2 direction = axis * Mathf.Sign(distance);
            float travel = Mathf.Abs(distance);
            float allowed = Distance(position, direction, travel + Skin * 10f, dropping, dropTop, ignored);
            if (_lastHit != null && allowed - Margin(direction) < travel)
            {
                float permitted = Mathf.Max(0f, allowed - Margin(direction));
                if (travel - permitted > 0.001f)
                {
                    if (axis.x != 0f) velocity.x = 0f;
                    else velocity.y = 0f;
                }
                travel = permitted;
            }
            position += direction * travel;
        }

        float Margin(Vector2 direction) => Skin / Mathf.Max(0.1f, -Vector2.Dot(_lastNormal, direction)) + 0.001f;

        float Distance(Vector2 position, Vector2 direction, float distance, bool dropping,
            float dropTop = 0f, Collider2D ignored = null, bool groundOnly = false)
        {
            Rigidbody2D body = _shape.attachedRigidbody;
            Vector2 saved = body.position;
            int count;
            try
            {
                body.position = position;
                count = _shape.Cast(direction, _filter, _hits, distance);
            }
            finally { body.position = saved; }
            float result = distance;
            _lastHit = null;
            _lastPoint = _lastNormal = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];
                if (hit.collider == _shape || hit.collider == ignored) continue;
                Vector2 normal = hit.normal;
                Vector2 point = hit.point;
                if (hit.distance <= 0f)
                {
                    ColliderDistance2D contact = Contact(position, hit.collider);
                    if (!contact.isValid) continue;
                    normal = -contact.normal;
                    point = contact.pointB;
                }
                if (groundOnly && normal.y < GroundProbe.MinNormal) continue;
                if (GroundProbe.IsPlatform(hit.collider)
                    && ((dropping && Mathf.Abs(hit.collider.bounds.max.y - dropTop) < 0.1f)
                        || direction.y >= 0f || normal.y < GroundProbe.MinNormal)) continue;
                if (Vector2.Dot(normal, direction) < -0.1f && hit.distance < result)
                {
                    result = hit.distance;
                    _lastHit = hit.collider;
                    _lastPoint = point;
                    _lastNormal = normal;
                }
            }
            return result;
        }
    }
}
