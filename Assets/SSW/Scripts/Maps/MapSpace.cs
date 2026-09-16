using UnityEngine;

namespace SSW
{
    public static class MapSpace
    {
        static readonly Collider2D[] Hits = new Collider2D[32];

        public static bool Occupied(BoxCollider2D shape)
        {
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            Vector2 size = Vector2.Scale(shape.size, shape.transform.lossyScale);
            int count = Physics2D.OverlapBox(shape.transform.TransformPoint(shape.offset),
                size, shape.transform.eulerAngles.z, filter, Hits);

            for (int i = 0; i < count; i++)
            {
                Rigidbody2D body = Hits[i].attachedRigidbody;
                if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
                    return true;
            }

            return false;
        }
    }
}
