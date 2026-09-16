using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class DraftTrail : UnityEngine.UI.MaskableGraphic
    {
        struct Spark
        {
            public Vector2 Position;
            public Vector2 Drift;
            public float Age;
            public float Life;
            public float Size;
        }

        readonly List<Spark> _sparks = new List<Spark>(180);
        Vector2 _target;
        Vector2 _point;
        Vector2 _stamp;
        bool _active;
        bool _started;
        bool _smooth;
        float _head;
        uint _seed = 4739;

        public int Particles => _sparks.Count;
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
        }

        float Noise()
        {
            _seed ^= _seed << 13;
            _seed ^= _seed >> 17;
            _seed ^= _seed << 5;
            return (_seed & 65535) / 65535f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _head = Mathf.MoveTowards(_head, _active ? 1f : 0f, dt * 7f);
            if (_active)
            {
                _point = _smooth ? Vector2.Lerp(_point, _target, 1f - Mathf.Exp(-24f * dt)) : _target;
                float distance = Vector2.Distance(_stamp, _point);
                int count = Mathf.Min(18, Mathf.FloorToInt(distance / 7f));
                for (int i = 0; i < count; i++)
                {
                    if (_sparks.Count >= 180) _sparks.RemoveAt(0);
                    _sparks.Add(new Spark
                    {
                        Position = Vector2.Lerp(_stamp, _point, (i + 1f) / count),
                        Drift = new Vector2((Noise() - 0.5f) * 52f, 18f + Noise() * 42f),
                        Life = 0.42f + Noise() * 0.35f, Size = 2f + Noise() * 3f
                    });
                }
                if (count > 0) _stamp = _point;
            }
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = _sparks[i];
                spark.Age += dt;
                if (spark.Age >= spark.Life) _sparks.RemoveAt(i);
                else _sparks[i] = spark;
            }
            if (_head > 0f || _sparks.Count > 0 || !_active) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            foreach (Spark spark in _sparks)
            {
                float age = spark.Age / spark.Life;
                Vector2 point = spark.Position + spark.Drift * age * age;
                Color tint = color;
                tint.a *= (1f - age) * (1f - age);
                Glow(mesh, point, spark.Size * 2.8f, new Color(tint.r, tint.g, tint.b, tint.a * 0.3f));
                Diamond(mesh, point, spark.Size * (1f - age * 0.65f), tint);
            }
            if (_head <= 0f) return;
            Glow(mesh, _point, 25f, new Color(color.r, color.g, color.b, _head * 0.55f));
            Glow(mesh, _point, 11f, new Color(1f, 0.94f, 0.65f, _head * 0.9f));
            Diamond(mesh, _point, 4f, new Color(1f, 0.99f, 0.9f, _head));
        }

        static void Diamond(UnityEngine.UI.VertexHelper mesh, Vector2 point, float size, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(point + Vector2.up * size, tint, Vector2.zero);
            mesh.AddVert(point + Vector2.right * size, tint, Vector2.zero);
            mesh.AddVert(point + Vector2.down * size, tint, Vector2.zero);
            mesh.AddVert(point + Vector2.left * size, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        static void Glow(UnityEngine.UI.VertexHelper mesh, Vector2 point, float radius, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(point, tint, Vector2.zero);
            tint.a = 0f;
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                mesh.AddVert(point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
            }
            for (int i = 0; i < 12; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 12);
        }
    }
}
