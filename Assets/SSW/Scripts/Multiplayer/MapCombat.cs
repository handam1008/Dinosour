using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public static class MapCombat
    {
        static readonly List<RaycastHit2D> Hits = new List<RaycastHit2D>(16);
        static readonly List<Collider2D> Contacts = new List<Collider2D>(8);

        public static bool Sweep(Vector2 from, Vector2 to, float radius, int ground, out RaycastHit2D hit, ISet<Pin> ignored = null)
            => Sweep(from, to, radius, default, ground, out hit, ignored);

        public static bool Sweep(Vector2 from, Vector2 to, float radius, Vector2 size, int ground, out RaycastHit2D hit, ISet<Pin> ignored = null)
        {
            Vector2 travel = to - from;
            var filter = new ContactFilter2D { useTriggers = true };
            if (size.sqrMagnitude > 0f)
                Physics2D.CapsuleCast(from, size, CapsuleDirection2D.Vertical, 0f, travel.normalized, filter, Hits, travel.magnitude);
            else
                Physics2D.CircleCast(from, radius, travel.normalized, filter, Hits, travel.magnitude);
            hit = default;
            foreach (RaycastHit2D candidate in Hits)
            {
                Collider2D shape = candidate.collider;
                bool target = shape.TryGetComponent<Pin>(out var pin) && pin.Current > 0f;
                if (target && ignored != null && ignored.Contains(pin)) continue;
                bool terrain = !shape.isTrigger && (ground & 1 << shape.gameObject.layer) != 0;
                if (!terrain && !target) continue;
                if (hit.collider == null || candidate.distance < hit.distance) hit = candidate;
            }
            return hit.collider != null;
        }

        public static void Strike(Component source, Vector2 origin, Vector2 center, Vector2 axis, Vector2 size, int ground, float damage)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            Physics2D.OverlapBox(center, size, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg, filter, Contacts);
            foreach (Collider2D shape in Contacts)
            {
                if (!shape.TryGetComponent<Pin>(out var pin) || pin.Current <= 0f) continue;
                if (ShotQuery.GroundRay(origin, shape.ClosestPoint(origin), ground, out _)) continue;
                CombatDamage.Deal(source, pin, damage, DamageTag.BasicAttack);
            }
        }
    }
}
