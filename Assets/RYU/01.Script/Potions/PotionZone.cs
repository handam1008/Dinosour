using System.Collections.Generic;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class PotionZone : MonoBehaviour
    {
        [SerializeField] private float duration = 3f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float powerPerTick = 0.35f;

        private AbstractPotion _data;
        private PotionModifiers _mods;
        private Component _owner;
        private float _radius;

        private float _life;
        private float _tick;

        public void Init(AbstractPotion data, PotionModifiers mods, Component owner, float radius)
        {
            _data = data;
            _owner = owner;
            _radius = radius;

            _mods = mods.ForZone(powerPerTick);

            ApplyVisual(data.potionColor);
        }

        private void ApplyVisual(Color color)
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in systems)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = color;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.radius = _radius;
            }
        }

        private void Update()
        {
            _life += Time.deltaTime;
            if (_life >= duration)
            {
                Destroy(gameObject);
                return;
            }

            _tick += Time.deltaTime;
            if (_tick < tickInterval) return;
            _tick = 0f;
            ApplyOnce();
        }

        private void ApplyOnce()
        {
            if (_data == null) return;

            HashSet<Transform> applied = new HashSet<Transform>();
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);

            foreach (Collider2D hit in hits)
            {
                if (!applied.Add(hit.transform.root)) continue;
                _data.Use(hit.gameObject, _owner, _mods);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
