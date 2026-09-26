using UnityEngine;

namespace SSW
{
    public sealed class ShotTail : MonoBehaviour
    {
        SpriteRenderer _view;
        MagicianCardFeedback _feedback;
        NetCast _caster;
        Vector2 _start;
        Vector2 _end;
        Vector2 _impact;
        float _radius;
        float _duration;
        float _age;
        bool _hit;
        public uint Action { get; private set; }
        public int Part { get; private set; }
        internal Vector2 Point => _impact;

        public static void Create(NetCast caster, uint action, int part, SpriteRenderer source,
            MagicianCardFeedback feedback, Vector2 velocity, Vector2 point, float radius, bool hit)
        {
            var obj = new GameObject("Shot Finish");
            obj.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            obj.transform.localScale = source.transform.lossyScale;
            var tail = obj.AddComponent<ShotTail>();
            tail._view = obj.AddComponent<SpriteRenderer>();
            tail._view.sprite = source.sprite;
            tail._view.sharedMaterial = source.sharedMaterial;
            tail._view.color = source.color;
            tail._view.sortingLayerID = source.sortingLayerID;
            tail._view.sortingOrder = source.sortingOrder;
            if (feedback != null)
            {
                tail._feedback = obj.AddComponent<MagicianCardFeedback>();
                feedback.CopyTo(tail._feedback, source.sharedMaterial);
            }
            tail._caster = caster;
            tail.Action = action;
            tail.Part = part;
            tail._start = source.transform.position;
            Vector2 travel = point - tail._start;
            tail._end = hit && Vector2.Dot(travel, velocity) >= 0f && travel.sqrMagnitude < 16f ? point : tail._start;
            tail._impact = point;
            tail._radius = radius;
            tail._hit = hit;
            tail._duration = Mathf.Clamp(Vector2.Distance(tail._start, tail._end) / 24f, 0.05f, 0.12f);
            caster.AddFinish(tail);
        }

        void Update()
        {
            _age += Time.deltaTime;
            float progress = Mathf.Clamp01(_age / _duration);
            transform.position = Vector2.Lerp(_start, _end, progress);
            Color color = _view.color;
            color.a = 1f - progress;
            _view.color = color;
            if (progress < 1f) return;
            if (_hit && _feedback != null) _feedback.PlayImpact(_impact, _radius);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_feedback != null) _feedback.ReleaseTrail();
            if (_caster != null) _caster.RemoveFinish(this);
        }
    }
}
