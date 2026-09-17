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
            public SpriteRenderer Target;
            public MagicianCardFeedback Feedback;
            public MagicianCardFeedback TargetFeedback;
            public Vector2 Velocity;
            public Vector2 Gravity;
            public float Spin;
            public float Delay;
            public float Age;
            public float Blend;
            public bool Matched;
            public bool Started;
        }

        readonly List<Shot> _shots = new List<Shot>(8);
        readonly NumberRoller _cards;
        readonly int _ground;
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

        public CastView(NumberRoller cards, int ground)
        {
            _cards = cards;
            _ground = ground;
        }

        public void Card(uint action, Vector2 position, Vector2 direction, Suit suit, int rank, float delay)
        {
            Shot shot = Create(action, 0, position);
            shot.Feedback = shot.View.gameObject.AddComponent<MagicianCardFeedback>();
            _cards.StylePreview(shot.View, shot.Feedback, suit, rank);
            shot.Feedback.SetTrailVisible(false);
            shot.View.transform.localScale = Vector3.one * 0.3f;
            shot.Velocity = direction * 12f;
            shot.Spin = direction.x < 0f ? -720f : 720f;
            shot.Delay = delay;
            Start(shot);
        }

        public void Potion(uint action, int part, Vector2 position, Vector2 velocity, SpriteRenderer style, Sprite sprite, float gravity)
        {
            Shot shot = Create(action, part, position);
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

        Shot Create(uint action, int part, Vector2 position)
        {
            if (_shots.Count == 16) Remove(0);
            var obj = new GameObject("Shot Preview");
            obj.transform.position = position;
            var shot = new Shot { Action = action, Part = part, View = obj.AddComponent<SpriteRenderer>() };
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

        public bool Match(uint action, int part, SpriteRenderer target, MagicianCardFeedback feedback)
        {
            foreach (Shot shot in _shots)
            {
                if (shot.Action != action || shot.Part != part || shot.Matched) continue;
                shot.Target = target;
                shot.TargetFeedback = feedback;
                if (feedback != null) feedback.SetTrailVisible(false);
                shot.Matched = true;
                shot.Delay = 0f;
                Start(shot);
                shot.View.sprite = target.sprite;
                shot.View.color = target.color;
                shot.View.sharedMaterial = target.sharedMaterial;
                target.enabled = false;
                return true;
            }
            return false;
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
                if (shot.Matched)
                {
                    if (shot.Target == null) { Remove(i); continue; }
                    shot.Blend += delta;
                    float weight = Mathf.Clamp01(delta / Mathf.Max(delta, 0.12f - shot.Blend));
                    shot.View.transform.position = Vector3.Lerp(shot.View.transform.position, shot.Target.transform.position, weight);
                    shot.View.transform.rotation = Quaternion.Slerp(shot.View.transform.rotation, shot.Target.transform.rotation, weight);
                    shot.View.transform.localScale = Vector3.Lerp(shot.View.transform.localScale, shot.Target.transform.lossyScale, weight);
                    shot.View.sprite = shot.Target.sprite;
                    if (shot.Blend >= 0.12f) Remove(i);
                    continue;
                }
                if (shot.Delay > 0f)
                {
                    shot.Delay -= delta;
                    Start(shot);
                    continue;
                }
                shot.Age += delta;
                if (shot.Age >= 0.8f) { Remove(i); continue; }
                Vector2 before = shot.View.transform.position;
                shot.Velocity += shot.Gravity * delta;
                Vector2 next = before + shot.Velocity * delta;
                RaycastHit2D hit = Physics2D.Linecast(before, next, _ground);
                if (hit.collider != null)
                {
                    next = hit.point;
                    shot.Velocity = shot.Gravity = Vector2.zero;
                }
                shot.View.transform.position = next;
                shot.View.transform.Rotate(0f, 0f, shot.Spin * delta);
                if (shot.Age > 0.5f)
                {
                    Color color = shot.View.color;
                    color.a = 1f - (shot.Age - 0.5f) / 0.3f;
                    shot.View.color = color;
                }
            }
        }

        void Remove(int index)
        {
            Shot shot = _shots[index];
            if (shot.Target != null) shot.Target.enabled = true;
            if (shot.TargetFeedback != null) shot.TargetFeedback.SetTrailVisible(true);
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
