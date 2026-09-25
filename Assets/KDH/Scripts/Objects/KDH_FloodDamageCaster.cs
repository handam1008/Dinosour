using System.Collections.Generic;
using SSW;
using UnityEngine;

public class KDH_FloodDamageCaster : MonoBehaviour
{
    [SerializeField] private float requiredStayTime;
    [SerializeField] private float tick;
    [SerializeField] private float damage;

    private class TargetInfo
    {
        public float elapsed;
        public bool isDamaging;
    }

    private readonly Dictionary<Collider2D, TargetInfo> _targets = new();
    private readonly List<Collider2D> _keyBuffer = new();
    private readonly List<Collider2D> _toRemove = new();

    public bool canDamage;

    private void Update()
    {   
        if (!canDamage) return;
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

            var info = _targets[col];
            info.elapsed += Time.deltaTime;

            if (!info.isDamaging)
            {
                if (info.elapsed >= requiredStayTime)
                {
                    info.isDamaging = true;
                    info.elapsed = 0f;
                    ApplyDamage(col, damage);
                }
            }
            else
            {
                if (info.elapsed >= tick)
                {
                    info.elapsed = 0f;
                    ApplyDamage(col, damage);
                }
            }
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
                _targets[collision] = new TargetInfo();
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