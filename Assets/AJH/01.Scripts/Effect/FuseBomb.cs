using System.Collections.Generic;
using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace SSW
{
    public class FuseBomb : MonoBehaviour
    {
        [SerializeField] Rigidbody2D _body;
        [SerializeField] Collider2D _collider;
        [SerializeField] SpriteRenderer _renderer;
        [SerializeField] LayerMask _groundMask;
        [SerializeField] float _groundCheckDistance = 0.1f;
        [SerializeField] Color _armedColor = Color.red;
        [SerializeField] float _fuseTime = 0.45f;
        [SerializeField] float _armTime = 0.3f;
        [SerializeField] float _radius = 1.5f;
        [SerializeField] float _damage = 10f;
        [SerializeField] float _knockback = 6f;
        [SerializeField] GameObject _explosionPrefab;
        [SerializeField] SoundClipSO _explodeSound;

        Health _owner;
        float _timer;
        bool _armed;
        bool _landed;

        public void Launch(Health owner, Vector2 velocity)
        {
            _owner = owner;
            _body.linearVelocity = velocity;
        }

        void Update()
        {
            _timer += Time.deltaTime;

            if (!_armed && _timer >= _armTime)
            {
                _armed = true;
                _renderer.color = _armedColor;
            }

            if (_timer >= _fuseTime)
                Explode();
        }

        void FixedUpdate()
        {
            if (_landed || _body.linearVelocity.y > 0f) return;

            Bounds bounds = _collider.bounds;
            RaycastHit2D hit = Physics2D.Raycast(bounds.center, Vector2.down, bounds.extents.y + _groundCheckDistance, _groundMask);
            if (hit.collider == null) return;

            _landed = true;
            _body.linearVelocity = Vector2.zero;
            _body.bodyType = RigidbodyType2D.Kinematic;
            transform.position = new Vector3(transform.position.x, hit.point.y + bounds.extents.y, transform.position.z);
        }

        void Explode()
        {
            Vector2 center = transform.position;
            Instantiate(_explosionPrefab, center, Quaternion.identity);
            GameAudio.GetOrCreate().PlaySfx(_explodeSound);

            var targets = new HashSet<Health>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, _radius))
            {
                Health unit = hit.GetComponentInParent<Health>();
                if (unit != null && unit != _owner) targets.Add(unit);
            }

            foreach (Health target in targets)
            {
                CombatDamage.Deal(_owner, target, _damage);

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
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
