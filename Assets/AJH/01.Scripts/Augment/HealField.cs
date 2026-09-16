using System.Collections.Generic;
using SSW;
using UnityEngine;

public class HealField : MonoBehaviour
{
    [SerializeField] float _radius = 1.5f;
    [SerializeField] float _chargeTime = 1f;
    [SerializeField] float _healRatio = 0.20f;
    [SerializeField] float _lifeTime = 8f;

    readonly Dictionary<IHealable, float> _stayTimes = new();
    readonly HashSet<IHealable> _inside = new();
    readonly List<IHealable> _left = new();

    void Start()
    {
        Destroy(gameObject, _lifeTime);
    }

    void Update()
    {
        _inside.Clear();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, _radius))
        {
            IHealable healable = hit.GetComponentInParent<IHealable>();
            if (healable != null) _inside.Add(healable);
        }

        _left.Clear();
        foreach (IHealable key in _stayTimes.Keys)
            if (!_inside.Contains(key)) _left.Add(key);
        foreach (IHealable key in _left)
            _stayTimes.Remove(key);

        foreach (IHealable healable in _inside)
        {
            _stayTimes.TryGetValue(healable, out float time);
            time += Time.deltaTime;

            if (time >= _chargeTime)
            {
                healable.Heal(healable.Max * _healRatio);
                Destroy(gameObject);
                return;
            }

            _stayTimes[healable] = time;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}