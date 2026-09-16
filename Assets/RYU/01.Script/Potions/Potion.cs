using System.Collections.Generic;
using RYU._01.Script.FeedBack;
using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class Potion : MonoBehaviour
    {
        [SerializeField] private LayerMask _explodeOn;
        [SerializeField] private float splashRadious = 1.5f;
        [SerializeField] private GameObject _zonePrefab;

        private FeedBackPlayer _feedBackPlayer;
        private AbstractPotion _data;
        private PotionModifiers _mods = PotionModifiers.None;
        private Component _owner;

        private Rigidbody2D _rb;
        private Collider2D _collider;

        private bool _exploded;
        private int _bounceLeft;

        private readonly HashSet<Transform> _appliedRoots = new HashSet<Transform>();

        private Collider2D _ownerCollider;
        private bool _thrown;

        public AbstractPotion Data => _data;

        private float Radius => splashRadious * _mods.Splash;

        private void Awake()
        {
            _feedBackPlayer = GetComponent<FeedBackPlayer>();
            _collider = GetComponent<Collider2D>();
            _rb = GetComponent<Rigidbody2D>();

            if (_collider != null) _collider.enabled = false;
        }

        public void Init(AbstractPotion data, PotionModifiers mods, Component owner)
        {
            _data = data;
            _mods = mods;
            _owner = owner;
            _bounceLeft = mods.Bounce;

            if (transform.parent != null)
                _ownerCollider = transform.parent.GetComponentInParent<Collider2D>();

            if (_ownerCollider != null && _collider != null)
                Physics2D.IgnoreCollision(_collider, _ownerCollider, true);
        }

        public void Release()
        {
            _thrown = true;
            if (_collider != null) _collider.enabled = true;
        }

        private void FixedUpdate()
        {
            if (!_thrown || _ownerCollider == null) return;
            if (_collider.Distance(_ownerCollider).isOverlapped) return;

            Physics2D.IgnoreCollision(_collider, _ownerCollider, false);
            _ownerCollider = null;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_exploded) return;
            if ((_explodeOn.value & (1 << collision.gameObject.layer)) == 0) return;
            _exploded = true;

            bool hitTarget = Explode();

            if (_bounceLeft > 0 && !hitTarget)
            {
                _bounceLeft--;
                Bounce(collision);

                _exploded = false;
                _appliedRoots.Clear();
                return;
            }

            Destroy(gameObject);
        }

        private bool Explode()
        {
            if (_feedBackPlayer != null) _feedBackPlayer.PlayAllFeedBacks(_mods.Splash);
            if (_data == null) return false;

            bool hitTarget = false;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Radius);
            foreach (Collider2D hit in hits)
            {
                if (!_appliedRoots.Add(hit.transform.root)) continue;

                if (hit.GetComponentInParent<IDamageable>() != null) hitTarget = true;

                _data.Use(hit.gameObject, _owner, _mods);
            }

            if (_mods.LeaveZone) SpawnZone();
            return hitTarget;
        }

        private void SpawnZone()
        {
            if (_zonePrefab == null) return;

            GameObject go = Instantiate(_zonePrefab, transform.position, Quaternion.identity);
            PotionZone zone = go.GetComponent<PotionZone>();
            if (zone != null) zone.Init(_data, _mods, _owner, Radius);
        }

        private void Bounce(Collider2D wall)
        {
            if (_rb == null || _collider == null) return;

            Vector2 normal = _collider.Distance(wall).normal;
            _rb.linearVelocity = Vector2.Reflect(_rb.linearVelocity, -normal);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
