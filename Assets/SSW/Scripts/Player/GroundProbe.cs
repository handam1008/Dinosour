using UnityEngine;

namespace SSW
{
    public sealed class GroundProbe
    {
        public const float MinNormal = 0.55f;
        public const float Reach = 0.08f;
        readonly Collider2D _shape;
        readonly Rigidbody2D _body;
        readonly ContactFilter2D _filter;
        readonly RaycastHit2D[] _hits = new RaycastHit2D[16];

        public GroundProbe(Collider2D shape, Rigidbody2D body, LayerMask ground)
        {
            _shape = shape;
            _body = body;
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = ground, useTriggers = false };
        }

        public Collider2D Read(Collider2D ignored = null)
        {
            int count = _shape.Cast(Vector2.down, _filter, _hits, Reach);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];
                if (hit.collider == ignored) continue;
                Vector2 normal = hit.normal;
                Vector2 point = hit.point;
                if (hit.distance <= 0f)
                {
                    ColliderDistance2D contact = _shape.Distance(hit.collider);
                    if (!contact.isValid) continue;
                    normal = -contact.normal;
                    point = contact.pointB;
                }
                if (normal.y < MinNormal) continue;
                Vector2 velocity = _body.GetPointVelocity(point);
                if (hit.rigidbody != null) velocity -= hit.rigidbody.GetPointVelocity(point);
                if (Vector2.Dot(velocity, normal) <= 0.1f) return hit.collider;
            }
            return null;
        }

        public static bool IsPlatform(Collider2D shape) => shape.usedByEffector
            && shape.TryGetComponent<PlatformEffector2D>(out var platform) && platform.isActiveAndEnabled && platform.useOneWay;
    }
}
