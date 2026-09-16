using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_BreatheDamageCaster : MonoBehaviour
    {
        [SerializeField] private float tick;
        [SerializeField] private float damage;

        private readonly Dictionary<Collider2D, float> _targets = new();
        private readonly List<Collider2D> _keyBuffer = new();
        private readonly List<Collider2D> _toRemove = new();

        private void Update()
        {
            if (_targets.Count == 0) return;

            _keyBuffer.Clear();
            _keyBuffer.AddRange(_targets.Keys);

            foreach (var col in _keyBuffer)
            {
                if (col == null)
                {
                    _toRemove.Add(col);
                    continue;
                }

                float timer = _targets[col] + Time.deltaTime;

                if (timer >= tick)
                {
                    ApplyDamage(col, damage);
                    timer = 0f;
                }

                _targets[col] = timer;
            }

            if (_toRemove.Count > 0)
            {
                foreach (var col in _toRemove)
                    _targets.Remove(col);
                _toRemove.Clear();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out IDamageable _))
            {
                if (!_targets.ContainsKey(collision))
                    _targets[collision] = 0f;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out IDamageable _))
                _targets.Remove(collision);
        }

        private void ApplyDamage(Collider2D collision, float applyDamage)
        {
            if (collision.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(applyDamage);
        }
    }
}