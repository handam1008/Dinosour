using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class DraftTrail : UnityEngine.UI.MaskableGraphic
    {
        struct Mark
        {
            public Vector2 From;
            public Vector2 To;
            public Vector2 Drift;
            public float Age;
            public float Life;
        }

        static readonly Vector2[] Arrow =
        {
            new Vector2(0f, 0f), new Vector2(1f, -32f), new Vector2(9f, -25f),
            new Vector2(16f, -38f), new Vector2(22f, -35f), new Vector2(15f, -22f),
            new Vector2(28f, -22f)
        };
        readonly List<Mark> _marks = new List<Mark>(160);
        Vector2 _target;
        Vector2 _point;
        Vector2 _stamp;
        bool _active;
        bool _started;
        bool _smooth;
        float _head;

        public int Particles => _marks.Count;
        public Vector2 Point => _point;

        public void Move(Vector2 point, bool smooth)
        {
            _target = point;
            _smooth = smooth;
            if (!_started) { _point = point; _stamp = point; _started = true; }
            _active = true;
        }

        public void Stop()
        {
            _active = false;
            _started = false;
        }

        void Update()
        {
            bool visible = _head > 0f || _marks.Count > 0 || _active;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _head = Mathf.MoveTowards(_head, _active ? 1f : 0f, dt * 9f);
            if (_active)
            {
                _point = _smooth ? Vector2.Lerp(_point, _target, 1f - Mathf.Exp(-24f * dt)) : _target;
                float distance = Vector2.Distance(_stamp, _point);
                int count = Mathf.Min(24, Mathf.FloorToInt(distance / 4f));
                Vector2 normal = new Vector2(-(_point - _stamp).y, (_point - _stamp).x).normalized;
                for (int i = 0; i < count; i++)
                {
                    if (_marks.Count >= 160) _marks.RemoveAt(0);
                    Vector2 from = Vector2.Lerp(_stamp, _point, (float)i / count);
                    _marks.Add(new Mark
                    {
                        From = from, To = Vector2.Lerp(_stamp, _point, (i + 1f) / count),
                        Drift = normal * Mathf.Sin(from.x * 0.04f + from.y * 0.03f) * 10f + Vector2.up * 8f,
                        Life = 0.55f
                    });
                }
                if (count > 0) _stamp = _point;
            }
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                Mark mark = _marks[i];
                mark.Age += dt;
                if (mark.Age >= mark.Life) _marks.RemoveAt(i);
                else _marks[i] = mark;
            }
            if (visible) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            foreach (Mark mark in _marks)
            {
                float age = mark.Age / mark.Life;
                float fade = (1f - age) * (1f - age);
                Vector2 drift = mark.Drift * age * age;
                Vector2 from = mark.From + drift;
                Vector2 to = mark.To + drift;
                Vector2 normal = new Vector2(-(to - from).y, (to - from).x).normalized;
                Color tint = new Color(color.r, color.g, color.b, color.a * fade);
                Line(mesh, from, to, 1.25f * (1f - age * 0.7f), tint);
                tint.a *= 0.35f;
                Vector2 offset = normal * (3f + age * 3f);
                Line(mesh, from + offset, to + offset, 0.55f, tint);
            }
            if (_head <= 0f) return;
            Pointer(mesh, _point + new Vector2(2f, -2f), 1.14f, new Color(0f, 0f, 0f, _head * 0.22f));
            Pointer(mesh, _point, 1.12f, new Color(0.12f, 0.15f, 0.2f, _head * 0.95f));
            Pointer(mesh, _point, 1f, new Color(0.98f, 0.99f, 1f, _head));
        }

        static void Line(UnityEngine.UI.VertexHelper mesh, Vector2 from, Vector2 to, float width, Color tint)
        {
            Vector2 side = new Vector2(-(to - from).y, (to - from).x).normalized * width * 0.5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(from + side, tint, Vector2.zero);
            mesh.AddVert(to + side, tint, Vector2.zero);
            mesh.AddVert(to - side, tint, Vector2.zero);
            mesh.AddVert(from - side, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        static void Pointer(UnityEngine.UI.VertexHelper mesh, Vector2 point, float scale, Color tint)
        {
            int start = mesh.currentVertCount;
            Vector2 center = new Vector2(10f, -20f);
            foreach (Vector2 vertex in Arrow)
                mesh.AddVert(point + center + (vertex - center) * scale, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 5);
            mesh.AddTriangle(start, start + 5, start + 6);
            mesh.AddTriangle(start + 2, start + 3, start + 4);
            mesh.AddTriangle(start + 2, start + 4, start + 5);
        }
    }
}
