using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class CastView
    {
        sealed class Shot
        {
            public uint Action;
            public int Part;
            public SpriteRenderer View;
            public MagicianCardFeedback Feedback;
            public Vector2 Velocity;
            public Vector2 Gravity;
            public Vector2 Normal;
            public Collider2D Contact;
            public ShotContact Target;
            public float Radius;
            public Vector2 Size;
            public float Spin;
            public float Delay;
            public float Age;
            public float Life;
            public float GravityDelay;
            public float ExtraGravity;
            public float AngleOffset;
            public bool Started;
            public bool AlignVelocity;
            public bool Held;
        }

        readonly List<Shot> _shots = new List<Shot>(8);
        readonly NumberRoller _cards;
        readonly int _ground;
        readonly float _radius;
        readonly NetPlayer _caster;
        readonly IReadOnlyList<NetPlayer> _players;
        public int Count => _shots.Count;
        public int Visible
        {
            get
            {
                int count = 0;
                foreach (Shot shot in _shots)
                    if (shot.View.enabled && shot.View.color.a > 0f) count++;
                return count;
            }
        }

        public CastView(NumberRoller cards, int ground, float radius, NetPlayer caster, IReadOnlyList<NetPlayer> players)
        {
            _cards = cards;
            _ground = ground;
            _radius = radius;
            _caster = caster;
            _players = players;
        }

        public void Card(uint action, Vector2 position, Vector2 direction, Suit suit, int rank, float delay, float gravity, float life)
        {
            Shot shot = Create(action, 0, position, life);
            shot.Feedback = shot.View.gameObject.AddComponent<MagicianCardFeedback>();
            shot.Feedback.SoundEnabled = false;
            _cards.StylePreview(shot.View, shot.Feedback, suit, rank);
            shot.Feedback.SetTrailVisible(false);
            FlightStats stats = _caster.Stats.Flight;
            shot.View.transform.localScale = new Vector3(stats.Scale, stats.Scale * stats.Aspect, 1f);
            shot.Velocity = direction * stats.Speed;
            shot.Gravity = Physics2D.gravity * gravity;
            shot.Radius = _radius;
            shot.Spin = (direction.x < 0f ? -1f : 1f) * stats.Spin;
            shot.Delay = delay;
            Start(shot);
        }

        public void Potion(uint action, int part, Vector2 position, Vector2 velocity, SpriteRenderer style, Sprite sprite, float gravity, float life, Vector2 size)
        {
            Shot shot = Create(action, part, position, life);
            shot.View.sprite = sprite;
            shot.View.sharedMaterial = style.sharedMaterial;
            shot.View.color = style.color;
            shot.View.sortingLayerID = style.sortingLayerID;
            shot.View.sortingOrder = style.sortingOrder;
            FlightStats stats = _caster.Stats.Flight;
            shot.View.transform.localScale = new Vector3(stats.Scale, stats.Scale * stats.Aspect, 1f);
            shot.Velocity = velocity;
            shot.Gravity = Physics2D.gravity * gravity;
            shot.Size = size;
            shot.Radius = size.x * 0.5f;
            shot.Spin = stats.Spin;
            Start(shot);
        }

        public void Bolt(uint action, Vector2 position, Vector2 direction, SpriteRenderer style, Sprite sprite, BoltSpec spec, float life)
        {
            Shot shot = Create(action, 0, position, life);
            shot.View.sprite = sprite;
            shot.View.sharedMaterial = style.sharedMaterial;
            shot.View.sortingLayerID = style.sortingLayerID;
            shot.View.sortingOrder = style.sortingOrder;
            shot.View.color = spec.Charged ? new Color(1f, 0.85f, 0.3f) : Color.white;
            shot.View.transform.localScale = new Vector3(spec.Scale, spec.Scale * spec.Aspect, 1f);
            shot.View.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + (spec.Style == 3 ? -90f : 0f));
            shot.Velocity = direction * spec.Speed;
            shot.Gravity = Physics2D.gravity * spec.Gravity;
            shot.GravityDelay = spec.GravityDelay;
            shot.ExtraGravity = spec.ExtraGravity;
            shot.AngleOffset = spec.Style == 3 ? -90f : 0f;
            shot.Radius = spec.Radius;
            shot.Spin = spec.Spin;
            shot.AlignVelocity = spec.Gravity != 0f && spec.Spin == 0f;
            if (_caster.Cast.Weapon is GunCast gun) gun.Effects.Attach(shot.View, spec.Charged);
            Start(shot);
        }

        Shot Create(uint action, int part, Vector2 position, float life)
        {
            if (_shots.Count == 16) Remove(0);
            var obj = new GameObject("Shot Preview");
            obj.transform.position = position;
            var shot = new Shot
            {
                Action = action, Part = part, Life = life, View = obj.AddComponent<SpriteRenderer>(),
                Target = new ShotContact(_caster, _players)
            };
            shot.View.enabled = false;
            _shots.Add(shot);
            return shot;
        }

        void Start(Shot shot)
        {
            if (shot.Started || shot.Delay > 0f) return;
            shot.Started = true;
            shot.View.enabled = true;
            if (shot.Feedback != null)
            {
                shot.Feedback.SetTrailVisible(true);
                shot.Feedback.PlayLaunch();
                _caster.Cast.PredictSound(shot.Action, CastKind.Release);
            }
        }

        public bool Match(uint action, int part, ShotSync target)
        {
            for (int i = 0; i < _shots.Count; i++)
            {
                Shot shot = _shots[i];
                if (shot.Action != action || shot.Part != part) continue;
                target.Adopt(shot.View.transform, shot.Normal, shot.Contact, shot.Target);
                target.Hold(shot.Held);
                Remove(i);
                return true;
            }
            return false;
        }

        public void Read(System.Action<uint, int, Vector2, bool> read)
        {
            foreach (Shot shot in _shots)
                if (shot.Started && shot.View.enabled)
                    read(shot.Action, shot.Part, shot.View.transform.position, shot.Normal.sqrMagnitude > 0f || shot.Target.Blocked);
        }

        public bool Point(uint action, out Vector2 point)
        {
            foreach (Shot shot in _shots)
                if (shot.Action == action && shot.Started && shot.View.enabled)
                {
                    point = shot.View.transform.position;
                    return true;
                }
            point = default;
            return false;
        }

        public void Hold(uint action, bool hold)
        {
            foreach (Shot shot in _shots)
                if (shot.Action == action) shot.Held = hold;
        }

        public void Reject(uint action)
        {
            for (int i = _shots.Count - 1; i >= 0; i--)
                if (_shots[i].Action == action) Remove(i);
        }

        public void Tick(float delta)
        {
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                Shot shot = _shots[i];
                if (shot.Delay > 0f)
                {
                    shot.Delay -= delta;
                    Start(shot);
                    continue;
                }
                shot.Age += delta;
                if (shot.Age >= shot.Life) { Remove(i); continue; }
                if (shot.Held) continue;
                Vector2 before = shot.View.transform.position;
                Vector2 next = before;
                if (shot.Normal.sqrMagnitude > 0f && !ShotQuery.Touches(shot.Contact, before, shot.Radius, shot.Size))
                {
                    shot.Normal = Vector2.zero;
                    shot.Contact = null;
                }
                if (shot.Normal.sqrMagnitude == 0f && !shot.Target.Blocked)
                {
                    next += shot.Velocity * delta + shot.Gravity * (0.5f * delta * (delta + Time.fixedDeltaTime));
                    shot.Velocity += shot.Gravity * delta;
                    float falling = Mathf.Clamp(shot.Age - shot.GravityDelay, 0f, delta);
                    next += Vector2.down * (shot.ExtraGravity * 0.5f * falling * (falling + Time.fixedDeltaTime));
                    shot.Velocity += Vector2.down * (shot.ExtraGravity * falling);
                    if (ShotQuery.Ground(before, next, shot.Radius, shot.Size, _ground, out RaycastHit2D hit))
                    {
                        next = hit.centroid;
                        shot.Contact = hit.collider;
                        shot.Normal = hit.normal;
                    }
                    if (shot.Target.TryBlock(before, next, shot.Radius, shot.Size, shot.Age, shot.Velocity.magnitude))
                    {
                        next = shot.Target.Point;
                        shot.Normal = Vector2.zero;
                        shot.Contact = null;
                    }
                }
                shot.View.transform.position = next;
                if (shot.AlignVelocity && shot.Velocity.sqrMagnitude > 0.000001f)
                    shot.View.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(shot.Velocity.y, shot.Velocity.x) * Mathf.Rad2Deg + shot.AngleOffset);
                else shot.View.transform.Rotate(0f, 0f, shot.Spin * delta);
                if (shot.Age > shot.Life - 0.3f)
                {
                    Color color = shot.View.color;
                    color.a = Mathf.Clamp01((shot.Life - shot.Age) / 0.3f);
                    shot.View.color = color;
                }
            }
        }

        void Remove(int index)
        {
            Shot shot = _shots[index];
            if (shot.Feedback != null) shot.Feedback.ReleaseTrail();
            Object.Destroy(shot.View.gameObject);
            _shots.RemoveAt(index);
        }

        public void Clear()
        {
            for (int i = _shots.Count - 1; i >= 0; i--) Remove(i);
        }
    }
}
