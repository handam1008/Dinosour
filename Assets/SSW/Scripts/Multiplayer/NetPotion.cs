using System.Collections.Generic;
using RYU._01.Script.FeedBack;
using RYU._01.Script.Potions;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class NetPotion : NetworkBehaviour
    {
        [SerializeField] NetStock _stock;
        [SerializeField] NetZone _zonePrefab;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] Collider2D _collider;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] FeedBackPlayer _feedback;
        [SerializeField] float _radius = 1.5f;
        readonly NetworkVariable<int> _kind = new NetworkVariable<int>();
        readonly NetworkVariable<ulong> _caster = new NetworkVariable<ulong>();
        readonly NetworkVariable<uint> _action = new NetworkVariable<uint>();
        readonly NetworkVariable<int> _part = new NetworkVariable<int>();
        uint _startAction;
        int _startPart;
        NetPlayer _owner;
        Collider2D _ownerCollider;
        PotionModifiers _mods;
        Vector2 _velocity;
        int _startKind;
        int _bounces;
        float _age;
        bool _hit;

        public int Kind => _kind.Value;
        public SpriteRenderer Style => _sprite;
        public float Gravity => _body.gravityScale;
        public ulong Caster => _caster.Value;
        public uint Action => _action.Value;

        public void Init(NetPlayer owner, int kind, Vector2 velocity, PotionModifiers mods, uint action = 0, int part = 0)
        {
            _owner = owner;
            _startAction = action;
            _startPart = part;
            _ownerCollider = owner.Collider;
            _startKind = kind;
            _mods = mods;
            _bounces = mods.Bounce;
            _velocity = velocity;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _kind.Value = _startKind;
                _caster.Value = _owner.NetworkObjectId;
                _action.Value = _startAction;
                _part.Value = _startPart;
            }
            _sprite.sprite = _stock.At(_kind.Value).sprite;
            _collider.enabled = IsServer;
            if (!IsServer)
            {
                if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(_caster.Value, out NetworkObject caster))
                    caster.GetComponent<NetPlayer>().Cast.MatchShot(_action.Value, _part.Value, _sprite);
                return;
            }
            Physics2D.IgnoreCollision(_collider, _ownerCollider);
            _body.linearVelocity = _velocity;
            _body.angularVelocity = -360f;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            _age += Time.deltaTime;
            if (_age >= 6f || !NetGame.Current.CanFight) NetworkObject.Despawn();
        }

        void FixedUpdate()
        {
            if (!IsServer || _ownerCollider == null) return;
            if (_collider.Distance(_ownerCollider).isOverlapped) return;
            Physics2D.IgnoreCollision(_collider, _ownerCollider, false);
            _ownerCollider = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !IsSpawned || _hit || !NetGame.Current.CanFight) return;
            if (other.isTrigger && other.GetComponentInParent<Health>() == null) return;
            _hit = true;
            float radius = _radius * _mods.Splash;
            bool target = Splash(radius);
            ImpactRpc(_mods.Splash);
            if (_mods.LeaveZone)
            {
                NetZone zone = Instantiate(_zonePrefab, transform.position, Quaternion.identity);
                zone.Init(_owner, _kind.Value, radius, _mods.ForZone(0.35f));
                zone.NetworkObject.Spawn(true);
            }
            if (_bounces > 0 && !target)
            {
                _bounces--;
                Vector2 normal = _collider.Distance(other).normal;
                _body.linearVelocity = Vector2.Reflect(_body.linearVelocity, normal);
                _body.position -= normal * 0.08f;
                _hit = false;
                return;
            }
            NetworkObject.Despawn();
        }

        bool Splash(float radius)
        {
            bool found = false;
            HashSet<Health> applied = new HashSet<Health>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health == null || !applied.Add(health)) continue;
                found = true;
                _stock.At(_kind.Value).Use(health.gameObject, _owner, _mods);
            }
            return found;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void ImpactRpc(float scale)
        {
            _feedback.PlayAllFeedBacks(scale);
        }
    }
}