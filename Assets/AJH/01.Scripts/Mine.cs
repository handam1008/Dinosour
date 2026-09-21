using System.Collections.Generic;
using SSW;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Mine : MonoBehaviour
{
    [SerializeField] LayerMask _groundMask;
    [SerializeField] float _groundCheckDistance = 0.1f;
    [SerializeField] float _explosionRadius = 2f;
    [SerializeField] float _damage = 20f;
    [SerializeField] float _knockback = 6f;
    [SerializeField] float _lifeTime = 15f;
    [SerializeField] GameObject _explosionPrefab;

    readonly HashSet<Health> _ignored = new HashSet<Health>();

    Rigidbody2D _body;
    Collider2D _collider;
    Component _source;
    bool _landed;
    bool _exploded;

    public void Init(Component source, Health owner)
    {
        _source = source;
        if (owner != null) _ignored.Add(owner);
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    void Start()
    {
        Bounds bounds = _collider.bounds;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(bounds.center, bounds.extents.magnitude))
        {
            Health unit = hit.GetComponentInParent<Health>();
            if (unit != null) _ignored.Add(unit);
        }

        Destroy(gameObject, _lifeTime);
    }

    void FixedUpdate()
    {
        if (_landed) return;

        Bounds bounds = _collider.bounds;
        RaycastHit2D hit = Physics2D.Raycast(
            bounds.center,
            Vector2.down,
            bounds.extents.y + _groundCheckDistance,
            _groundMask);

        if (hit.collider == null) return;

        _landed = true;
        _body.linearVelocity = Vector2.zero;
        _body.bodyType = RigidbodyType2D.Kinematic;
        transform.position = new Vector3(
            transform.position.x,
            hit.point.y + bounds.extents.y,
            transform.position.z);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Health unit = other.GetComponentInParent<Health>();
        if (unit == null) return;

        _ignored.Remove(unit);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;

        Health unit = other.GetComponentInParent<Health>();
        if (unit == null || unit.Current <= 0f) return;
        if (_ignored.Contains(unit)) return;

        Explode();
    }

    void Explode()
    {
        _exploded = true;
        Vector2 center = transform.position;

        if (_explosionPrefab != null)
            Instantiate(_explosionPrefab, center, Quaternion.identity);

        var targets = new HashSet<Health>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, _explosionRadius))
        {
            Health unit = hit.GetComponentInParent<Health>();
            if (unit != null) targets.Add(unit);
        }

        Component source = _source != null ? _source : null;

        foreach (Health target in targets)
        {
            CombatDamage.Deal(source, target, _damage, DamageTag.JobSkill);

            IForceReceiver receiver = target.GetComponentInParent<IForceReceiver>();
            if (receiver == null) continue;

            float dir = Mathf.Sign(target.transform.position.x - center.x);
            receiver.ApplyForce(new Vector2(dir, 0.5f) * _knockback, ForceMode2D.Impulse);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}