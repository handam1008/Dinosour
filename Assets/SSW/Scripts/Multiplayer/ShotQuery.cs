using UnityEngine;

namespace SSW
{
    public static class ShotQuery
    {
        static readonly RaycastHit2D[] Hits = new RaycastHit2D[16];
        static readonly Collider2D[] Contacts = new Collider2D[16];

        public static bool Ground(Vector2 from, Vector2 to, float radius, int mask, out RaycastHit2D hit)
            => Ground(from, to, radius, default, mask, out hit);

        public static bool Ground(Vector2 from, Vector2 to, float radius, Vector2 size, int mask, out RaycastHit2D hit)
        {
            Vector2 travel = to - from;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(mask);
            int count = size.sqrMagnitude > 0f
                ? Physics2D.CapsuleCast(from, size, CapsuleDirection2D.Vertical, 0f, travel.normalized, filter, Hits, travel.magnitude)
                : Physics2D.CircleCast(from, radius, travel.normalized, filter, Hits, travel.magnitude);
            hit = default;
            for (int i = 0; i < count; i++)
                if (Hits[i].collider != null && (hit.collider == null || Hits[i].distance < hit.distance)) hit = Hits[i];
            return hit.collider != null;
        }

        public static bool Touches(Collider2D contact, Vector2 point, float radius, Vector2 size)
        {
            if (contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy) return false;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(1 << contact.gameObject.layer);
            int count = size.sqrMagnitude > 0f
                ? Physics2D.OverlapCapsule(point, size + Vector2.one * 0.04f, CapsuleDirection2D.Vertical, 0f, filter, Contacts)
                : Physics2D.OverlapCircle(point, radius + 0.02f, filter, Contacts);
            for (int i = 0; i < count; i++)
                if (Contacts[i] == contact) return true;
            return false;
        }
        public static bool Capsule(Vector2 from, Vector2 to, Vector2 first, Vector2 last,
            Vector2 axis, float half, float radius, out float fraction)
        {
            Vector2 side = new Vector2(axis.y, -axis.x);
            Vector2 start = from - first;
            Vector2 end = to - last;
            Vector2 point = new Vector2(Vector2.Dot(start, side), Vector2.Dot(start, axis));
            Vector2 delta = new Vector2(Vector2.Dot(end - start, side), Vector2.Dot(end - start, axis));
            fraction = float.PositiveInfinity;
            if (Box(point, delta, new Vector2(radius, half), out float body)) fraction = body;
            if (Circle(point - Vector2.up * half, delta, radius, out float top)) fraction = Mathf.Min(fraction, top);
            if (Circle(point + Vector2.up * half, delta, radius, out float bottom)) fraction = Mathf.Min(fraction, bottom);
            return fraction <= 1f;
        }

        static bool Circle(Vector2 point, Vector2 delta, float radius, out float fraction)
        {
            fraction = 0f;
            float c = point.sqrMagnitude - radius * radius;
            if (c <= 0f) return true;
            float a = delta.sqrMagnitude;
            float b = Vector2.Dot(point, delta);
            float discriminant = b * b - a * c;
            if (a < 0.000001f || discriminant < 0f) return false;
            fraction = (-b - Mathf.Sqrt(discriminant)) / a;
            return fraction >= 0f && fraction <= 1f;
        }

        static bool Box(Vector2 point, Vector2 delta, Vector2 half, out float fraction)
        {
            fraction = 0f;
            float last = 1f;
            for (int axis = 0; axis < 2; axis++)
            {
                if (Mathf.Abs(delta[axis]) < 0.000001f)
                {
                    if (Mathf.Abs(point[axis]) > half[axis]) return false;
                    continue;
                }
                float a = (-half[axis] - point[axis]) / delta[axis];
                float b = (half[axis] - point[axis]) / delta[axis];
                fraction = Mathf.Max(fraction, Mathf.Min(a, b));
                last = Mathf.Min(last, Mathf.Max(a, b));
                if (fraction > last) return false;
            }
            return fraction <= 1f;
        }
    }
}
