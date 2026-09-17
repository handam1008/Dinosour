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
            public float Radius;
            public float Spin;
            public float Delay;
            public float Age;
            public float Life;
            public bool Started;
        }

        readonly List<Shot> _shots = new List<Shot>(8);
        readonly NumberRoller _cards;
        readonly int _ground;
        readonly float _radius;
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

        public CastView(NumberRoller cards, int ground, float radius)
        {
            _cards = cards;
            _ground = ground;
            _radius = radius;
        }

        public void Card(uint action, Vector2 position, Vector2 direction, Suit suit, int rank, float delay, float gravity, float life)
        {
            Shot shot = Create(action, 0, position, life);
            shot.Feedback = shot.View.gameObject.AddComponent<MagicianCardFeedback>();
            _cards.StylePreview(shot.View, shot.Feedback, suit, rank);
            shot.Feedback.SetTrailVisible(false);
            shot.View.transform.localScale = Vector3.one * 0.3f;
            shot.Velocity = direction * 12f;
            shot.Gravity = Physics2D.gravity * gravity;
            shot.Radius = _radius;
            shot.Spin = direction.x < 0f ? -720f : 720f;
            shot.Delay = delay;
            Start(shot);
        }

        public void Potion(uint action, int part, Vector2 position, Vector2 velocity, SpriteRenderer style, Sprite sprite, float gravity, float life)
        {
            Shot shot = Create(action, part, position, life);
            shot.View.sprite = sprite;
            shot.View.sharedMaterial = style.sharedMaterial;
            shot.View.color = style.color;
            shot.View.sortingLayerID = style.sortingLayerID;
            shot.View.sortingOrder = style.sortingOrder;
            shot.View.transform.localScale = style.transform.lossyScale;
            shot.Velocity = velocity;
            shot.Gravity = Physics2D.gravity * gravity;
            shot.Spin = -360f;
            Start(shot);
        }

        Shot Create(uint action, int part, Vector2 position, float life)
        {
            if (_shots.Count == 16) Remove(0);
            var obj = new GameObject("Shot Preview");
            obj.transform.position = position;
            var shot = new Shot { Action = action, Part = part, Life = life, View = obj.AddComponent<SpriteRenderer>() };
            shot.View.enabled = false;
            _shots.Add(shot);
            return shot;
        }

        static void Start(Shot shot)
        {
            if (shot.Started || shot.Delay > 0f) return;
            shot.Started = true;
            shot.View.enabled = true;
            if (shot.Feedback != null)
            {
                shot.Feedback.SetTrailVisible(true);
                shot.Feedback.PlayLaunch();
            }
        }

        public bool Match(uint action, int part, ShotSync target)
        {
            for (int i = 0; i < _shots.Count; i++)
            {
                Shot shot = _shots[i];
                if (shot.Action != action || shot.Part != part) continue;
                target.Adopt(shot.View.transform, shot.Normal, shot.Contact);
                Remove(i);
                return true;
            }
            return false;
        }

        public void Read(System.Action<uint, int, Vector2, bool> read)
        {
            foreach (Shot shot in _shots)
                if (shot.Started && shot.View.enabled) read(shot.Action, shot.Part, shot.View.transform.position, shot.Normal.sqrMagnitude > 0f);
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
                Vector2 before = shot.View.transform.position;
                Vector2 next = before + shot.Velocity * delta + shot.Gravity * (0.5f * delta * (delta + Time.fixedDeltaTime));
                shot.Velocity += shot.Gravity * delta;
                if (shot.Normal.sqrMagnitude == 0f && ShotQuery.Ground(before, next, shot.Radius, _ground, out RaycastHit2D hit))
                {
                    next = hit.centroid;
                    shot.Contact = hit.collider;
                    shot.Normal = hit.normal;
                    shot.Velocity = shot.Gravity = Vector2.zero;
                }
                shot.View.transform.position = next;
                shot.View.transform.Rotate(0f, 0f, shot.Spin * delta);
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
