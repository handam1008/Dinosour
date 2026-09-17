using UnityEngine;

namespace SSW
{
    public sealed class MotionZones
    {
        public struct Sample
        {
            public bool Pad;
            public float Launch;
            public float Rise;
            public float Acceleration;
        }

        readonly CapsuleCollider2D _shape;
        readonly ContactFilter2D _filter;
        readonly Collider2D[] _hits = new Collider2D[32];

        public MotionZones(CapsuleCollider2D shape)
        {
            _shape = shape;
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = Physics2D.GetLayerCollisionMask(shape.gameObject.layer),
                useTriggers = true
            };
        }

        public Sample Read(Vector2 position)
        {
            Vector2 scale = _shape.transform.lossyScale;
            scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            Vector2 size = Vector2.Scale(_shape.size, scale) + Vector2.one * 0.02f;
            Vector2 center = position + (Vector2)_shape.transform.TransformVector(_shape.offset);
            int count = Physics2D.OverlapCapsule(center, size, _shape.direction,
                _shape.transform.eulerAngles.z, _filter, _hits);
            Sample sample = default;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hits[i];
                if (!hit.isTrigger || hit == _shape || hit.attachedRigidbody == _shape.attachedRigidbody) continue;
                if (hit.TryGetComponent<JumpPad>(out var pad) && pad.isActiveAndEnabled)
                {
                    sample.Launch = sample.Pad ? Mathf.Max(sample.Launch, pad.LaunchVelocity) : pad.LaunchVelocity;
                    sample.Pad = true;
                }
                if (hit.TryGetComponent<WindZone>(out var wind) && wind.isActiveAndEnabled && wind.IsOn)
                {
                    sample.Rise = sample.Acceleration > 0f ? Mathf.Max(sample.Rise, wind.RiseSpeed) : wind.RiseSpeed;
                    sample.Acceleration = Mathf.Max(sample.Acceleration, wind.Acceleration);
                }
            }
            return sample;
        }
    }
}
