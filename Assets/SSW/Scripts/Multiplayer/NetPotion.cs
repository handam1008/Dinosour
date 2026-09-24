using System.Collections.Generic;
using RYU._01.Script.FeedBack;
using RYU._01.Script.Potions;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(150)]
    public sealed class NetPotion : NetworkBehaviour
    {
        [SerializeField] ShotSync _flight;
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
        Vector2 _previous;
        double _previousTime;
        double _lag;
        int _startKind;
        int _bounces;
        float _age;
        bool _hit;
        AbstractFeedBack[] _effects;

        void Awake() => _effects = _feedback.GetComponents<AbstractFeedBack>();

        public int Kind => _kind.Value;
        public SpriteRenderer Style => _sprite;
        public float Gravity => _body.gravityScale;
        public ulong Caster => _caster.Value;
        public uint Action => _action.Value;
        public Vector2 Size => Vector2.Scale(((CapsuleCollider2D)_collider).size,
            new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));

        public void Init(NetPlayer owner, int kind, Vector2 velocity, PotionModifiers mods, uint action = 0, int part = 0, double lag = 0d)
        {
            _owner = owner;
            _startAction = action;
            _startPart = part;
            _ownerCollider = owner.Collider;
            _startKind = kind;
            _mods = mods;
            _bounces = mods.Bounce;
            _velocity = velocity;
            _lag = lag;
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
            NetPlayer caster = NetworkManager.SpawnManager.SpawnedObjects[_caster.Value].GetComponent<NetPlayer>();
            _flight.Bind(caster, _action.Value, _part.Value, Size.x * 0.5f, Size);
            if (!IsServer) return;
            Physics2D.IgnoreCollision(_collider, _ownerCollider);
            _previous = _body.position;
            _previousTime = NetGame.Current.PhysicsTime;
            _body.linearVelocity = _velocity;
            _body.angularVelocity = -360f;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            _age += Time.deltaTime;
            if (_age >= 6f || !NetGame.Current.CanFight) Finish(false);
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || _hit || !NetGame.Current.CanFight) return;
            bool ignoreOwner = _ownerCollider != null;
            if (ignoreOwner && !_collider.Distance(_ownerCollider).isOverlapped)
            {
                Physics2D.IgnoreCollision(_collider, _ownerCollider, false);
                _ownerCollider = null;
            }
            double time = NetGame.Current.PhysicsTime;
            Vector2 next = _body.position;
            Vector2 size = Size;
            float nearest = float.PositiveInfinity;
            Vector2 point = next;
            Vector2 normal = Vector2.zero;
            Health direct = null;
            if (ShotQuery.Ground(_previous, next, size.x * 0.5f, size, _owner.GroundMask, out RaycastHit2D ground))
            {
                float distance = Vector2.Distance(_previous, next);
                nearest = distance > 0.000001f ? ground.distance / distance : 0f;
                point = ground.centroid;
                normal = ground.normal;
            }
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (!target.CanAct || target == _owner && ignoreOwner) continue;
                double lag = target == _owner ? 0d : _lag;
                if (!target.SweepHit(_previous, next, _previousTime - lag, time - lag,
                    size.x * 0.5f, Mathf.Max(0f, (size.y - size.x) * 0.5f), out float fraction) || fraction >= nearest) continue;
                nearest = fraction;
                point = Vector2.Lerp(_previous, next, fraction);
                direct = target.Health;
                normal = Vector2.zero;
            }
            if (nearest <= 1f) Contact(point, normal, direct);
            _previous = _body.position;
            _previousTime = time;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !IsSpawned || _hit || !NetGame.Current.CanFight) return;
            if (other.GetComponentInParent<NetPlayer>() != null) return;
            Health direct = other.GetComponentInParent<Health>();
            if (other.isTrigger && direct == null) return;
            Contact(_body.position, -_collider.Distance(other).normal, direct);
        }

        void Contact(Vector2 point, Vector2 normal, Health direct)
        {
            _hit = true;
            _body.position = point;
            float radius = _radius * _mods.Splash;
            bool target = Splash(point, radius, direct);
            ImpactRpc(point, _mods.Splash);
            if (_mods.LeaveZone)
            {
                NetZone zone = Instantiate(_zonePrefab, point, Quaternion.identity);
                zone.Init(_owner, _kind.Value, radius, _mods.ForZone(0.35f));
                zone.NetworkObject.Spawn(true);
            }
            if (_bounces > 0 && !target)
            {
                _bounces--;
                _flight.Redirect();
                _body.linearVelocity = Vector2.Reflect(_body.linearVelocity, normal);
                _body.position += normal * 0.08f;
                _previous = _body.position;
                _previousTime = NetGame.Current.PhysicsTime;
                _hit = false;
                return;
            }
            Finish(true);
        }

        bool Splash(Vector2 point, float radius, Health direct)
        {
            HashSet<Health> applied = new HashSet<Health>();
            if (direct != null)
            {
                applied.Add(direct);
                _stock.At(_kind.Value).Use(direct.gameObject, _owner, _mods);
            }
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(point, radius))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health == null || !applied.Add(health)) continue;
                _stock.At(_kind.Value).Use(health.gameObject, _owner, _mods);
            }
            return applied.Count > 0;
        }

        void Finish(bool hit)
        {
            FinishRpc(_body.position, hit);
            NetworkObject.Despawn();
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        void FinishRpc(Vector2 point, bool hit) => _flight.Complete(point, 0f, hit);

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void ImpactRpc(Vector2 point, float scale)
        {
            if (!IsClient) return;
            foreach (AbstractFeedBack effect in _effects)
            {
                try { effect.CreateFeedBack(point, scale); }
                catch (System.Exception error) { Debug.LogException(error, effect); }
            }
        }
    }
}
