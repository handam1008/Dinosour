using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(1200)]
    public sealed class EffectPool : MonoBehaviour
    {
        sealed class Item
        {
            public GameObject Prefab;
            public GameObject Object;
            public ParticleSystem[] Particles;
            public TrailRenderer[] Trails;
            public Vector3 Scale;
            public Vector3 DisplayScale;
            public Vector3 Point;
            public Quaternion Rotation;
            public Transform Follow;
            public Vector3 Offset;
            public SpriteRenderer Source;
            public SpriteRenderer Fade;
            public Color Color;
            public float Start;
            public float End;
            public bool Tracked;
            public bool Seen;
            public bool Following;
        }

        readonly Dictionary<GameObject, Stack<Item>> _free = new Dictionary<GameObject, Stack<Item>>();
        readonly List<Item> _active = new List<Item>();
        public int ActiveCount => _active.Count;
        public int CreatedCount { get; private set; }
        public int PooledCount => CreatedCount - ActiveCount;

        Item Create(GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            CreatedCount++;
            return new Item
            {
                Prefab = prefab, Object = obj, Scale = prefab.transform.localScale,
                Particles = obj.GetComponentsInChildren<ParticleSystem>(true),
                Trails = obj.GetComponentsInChildren<TrailRenderer>(true)
            };
        }

        Stack<Item> Free(GameObject prefab)
        {
            if (!_free.TryGetValue(prefab, out Stack<Item> items))
            {
                items = new Stack<Item>();
                _free.Add(prefab, items);
            }
            return items;
        }

        public void Warm(GameObject prefab, int count)
        {
            Stack<Item> items = Free(prefab);
            while (items.Count < count) items.Push(Create(prefab));
        }

        Item Take(GameObject prefab, Vector3 point, Quaternion rotation, float duration, Transform follow)
        {
            Stack<Item> items = Free(prefab);
            Item item = items.Count > 0 ? items.Pop() : Create(prefab);
            item.Point = point;
            item.Rotation = rotation;
            item.Follow = follow;
            item.Following = follow != null;
            item.Offset = follow != null ? point - follow.position : Vector3.zero;
            item.Source = null;
            item.Fade = null;
            item.Tracked = false;
            item.Seen = false;
            item.Start = Time.time;
            item.End = Time.time + duration;
            item.Object.transform.SetPositionAndRotation(point, rotation);
            item.DisplayScale = item.Scale;
            Resize(item);
            item.Object.SetActive(true);
            foreach (TrailRenderer trail in item.Trails) { trail.Clear(); trail.emitting = true; }
            foreach (ParticleSystem particles in item.Particles)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play(false);
            }
            _active.Add(item);
            return item;
        }

        public GameObject Play(GameObject prefab, Vector3 point, float duration, Transform follow = null)
            => Take(prefab, point, Quaternion.identity, Mathf.Max(0.01f, duration), follow).Object;

        public void Trail(GameObject prefab, SpriteRenderer source)
        {
            Item item = Take(prefab, source.transform.position, source.transform.rotation, float.PositiveInfinity, source.transform);
            item.Source = source;
            item.Tracked = true;
            item.Seen = source.enabled && source.gameObject.activeInHierarchy;
            item.Object.SetActive(item.Seen);
        }

        public void Image(GameObject prefab, SpriteRenderer source, float duration, Color color)
        {
            Item item = Take(prefab, source.transform.position, source.transform.rotation, duration, null);
            SpriteRenderer view = item.Object.GetComponent<SpriteRenderer>();
            view.sprite = source.sprite;
            view.sharedMaterial = source.sharedMaterial;
            view.flipX = source.flipX;
            view.flipY = source.flipY;
            view.sortingLayerID = source.sortingLayerID;
            view.sortingOrder = source.sortingOrder - 1;
            view.color = color;
            item.DisplayScale = source.transform.lossyScale;
            Resize(item);
            item.Fade = view;
            item.Color = color;
        }

        public void Release(GameObject obj)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Object != obj) continue;
                Return(i);
                return;
            }
        }

        void Return(int index)
        {
            Item item = _active[index];
            _active.RemoveAt(index);
            foreach (ParticleSystem particles in item.Particles) particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (TrailRenderer trail in item.Trails) { trail.Clear(); trail.emitting = false; }
            item.Object.SetActive(false);
            item.Follow = null;
            item.Source = null;
            item.Fade = null;
            Free(item.Prefab).Push(item);
        }

        void Resize(Item item)
        {
            Vector3 parent = transform.lossyScale;
            Vector3 scale = item.DisplayScale;
            item.Object.transform.localScale = new Vector3(scale.x / parent.x, scale.y / parent.y, scale.z / parent.z);
        }

        void LateUpdate() => Tick(Time.time);

        void Tick(float now)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Item item = _active[i];
                if (now >= item.End) { Return(i); continue; }
                if (item.Tracked)
                {
                    bool visible = item.Source != null && item.Source.enabled && item.Source.gameObject.activeInHierarchy;
                    if (!visible && (item.Seen || item.Source == null))
                    {
                        item.Tracked = false;
                        item.Following = false;
                        item.Follow = null;
                        item.Source = null;
                        item.End = now + 0.6f;
                        foreach (ParticleSystem particles in item.Particles) particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                        foreach (TrailRenderer trail in item.Trails) trail.emitting = false;
                    }
                    else
                    {
                        bool start = visible && !item.Seen;
                        item.Seen |= visible;
                        item.Object.SetActive(visible);
                        if (start)
                        {
                            foreach (ParticleSystem particles in item.Particles) particles.Play(false);
                            foreach (TrailRenderer trail in item.Trails) { trail.Clear(); trail.emitting = true; }
                        }
                        if (visible) item.Rotation = item.Source.transform.rotation;
                    }
                }
                if (item.Following)
                {
                    if (item.Follow == null) { Return(i); continue; }
                    item.Point = item.Follow.position + item.Offset;
                }
                item.Object.transform.SetPositionAndRotation(item.Point, item.Rotation);
                Resize(item);
                if (item.Fade != null)
                {
                    Color color = item.Color;
                    color.a *= Mathf.Clamp01((item.End - now) / (item.End - item.Start));
                    item.Fade.color = color;
                }
            }
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Return(i);
        }

        void OnDisable() => Clear();
    }
}
