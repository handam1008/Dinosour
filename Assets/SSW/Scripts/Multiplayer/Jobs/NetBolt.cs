using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(150)]
    public sealed class NetBolt : NetworkBehaviour
    {
        [SerializeField] ShotSync _flight;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] Sprite[] _styles;
        readonly NetworkVariable<ulong> _caster = new NetworkVariable<ulong>();
        readonly NetworkVariable<uint> _action = new NetworkVariable<uint>();
        readonly NetworkVariable<BoltSpec> _spec = new NetworkVariable<BoltSpec>();
        readonly HashSet<ulong> _hitPlayers = new HashSet<ulong>();
        NetPlayer _owner;
        IBoltReceiver _receiver;
        BoltSpec _start;
        uint _startAction;
        Vector2 _velocity;
        Vector2 _previous;
        double _previousTime;
        double _lag;
        float _age;
        int _bounces;
        bool _stuck;
        Vector2 _normal;
        public BoltSpec Spec => _spec.Value;
        public uint Action => _action.Value;
        public ulong Caster => _caster.Value;
        public SpriteRenderer Style => _sprite;
        public Sprite Sprite(int style) => _styles[style];
        public NetPlayer Owner => _owner;
        public Vector2 Velocity => _body.linearVelocity;
        public Vector2 Position => _body.position;
        public Vector2 Normal => _normal;

        public void Init(NetPlayer owner, IBoltReceiver receiver, uint action, Vector2 direction, BoltSpec spec, double lag)
        {
            _owner = owner;
            _receiver = receiver;
            _startAction = action;
            _start = spec;
            _velocity = direction.normalized * spec.Speed;
            _bounces = spec.Bounce;
            _lag = lag;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _caster.Value = _owner.NetworkObjectId;
                _action.Value = _startAction;
                _spec.Value = _start;
            }
            _owner = NetworkManager.SpawnManager.SpawnedObjects[_caster.Value].GetComponent<NetPlayer>();
            _sprite.sprite = _styles[Spec.Style];
            _sprite.color = Spec.Charged ? new Color(1f, 0.85f, 0.3f) : Color.white;
            transform.localScale = Vector3.one * Spec.Scale;
            _flight.Bind(_owner, _action.Value, 0, Spec.Radius);
            if (!IsServer && _owner.IsOwner && Spec.Stick && _owner.Cast.Weapon is KnifeCast knife) knife.Track(this);
            if (!IsServer) return;
            _previous = _body.position;
            _previousTime = NetGame.Current.PhysicsTime;
            _body.linearVelocity = _velocity;
            _body.angularVelocity = Spec.Spin;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            _age += Time.deltaTime;
            if (_age >= Spec.Life || !NetGame.Current.CanFight) Finish();
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || _stuck || !NetGame.Current.CanFight) return;
            Vector2 next = _body.position;
            double time = NetGame.Current.PhysicsTime;
            float nearest = float.PositiveInfinity;
            Vector2 point = next;
            Vector2 normal = Vector2.zero;
            NetPlayer contact = null;
            if (ShotQuery.Ground(_previous, next, Spec.Radius, _owner.GroundMask, out RaycastHit2D wall))
            {
                float distance = Vector2.Distance(_previous, next);
                nearest = distance > 0.000001f ? wall.distance / distance : 0f;
                point = wall.centroid;
                normal = wall.normal;
            }
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == _owner || !target.CanAct) continue;
                
                if (_hitPlayers.Contains(target.NetworkObjectId)) continue;
                
                if (!target.SweepHit(_previous, next, _previousTime - _lag, time - _lag, Spec.Radius, out float fraction) || fraction >= nearest) continue;
                nearest = fraction;
                contact = target;
                point = Vector2.Lerp(_previous, next, fraction);
            }
            if (nearest <= 1f)
            {
                _owner.Cast.ImpactSound();
                _body.position = point;
                if (contact != null)
                {
                    if (contact.Cast.Weapon is SwordCast sword && sword.Deflect())
                    {
                        _caster.Value = contact.NetworkObjectId;
                        _receiver = sword;
                        _lag = 0d;
                        _body.linearVelocity = -_body.linearVelocity.normalized * 20f;
                        _body.position += _body.linearVelocity.normalized * 0.05f;
                        _previous = _body.position;
                        _previousTime = time;
                        DeflectRpc(contact.NetworkObjectId);
                        return;
                    }
                    _hitPlayers.Add(contact.NetworkObjectId);
                    _receiver.Hit(contact, this);
                    if(Spec.CanPenetrate) return;
                    Finish(true);
                    return;
                }
                if (_bounces > 0)
                {
                    _bounces--;
                    _body.linearVelocity = Vector2.Reflect(_body.linearVelocity, normal);
                    _body.position += normal * 0.04f;
                    _flight.Redirect();
                }
                else if (Spec.Stick)
                {
                    _stuck = true;
                    _normal = normal;
                    _body.linearVelocity = Vector2.zero;
                    _body.angularVelocity = 0f;
                    _flight.Redirect();
                }
                else
                {
                    Finish(true);
                    return;
                }
            }
            _previous = _body.position;
            _previousTime = time;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void DeflectRpc(ulong owner)
        {
            _owner = NetworkManager.SpawnManager.SpawnedObjects[owner].GetComponent<NetPlayer>();
            _flight.Deflect(_owner);
        }

        public void Hold(bool hold) => _flight.Hold(hold);

        public void Recall(Vector2 requested, float seconds, out Vector2 point, out Vector2 normal)
        {
            point = Position;
            normal = _normal;
            if (_stuck || _bounces != Spec.Bounce || !NetMath.Finite(requested)) return;
            Vector2 shift = requested - point;
            float limit = Spec.Speed * Mathf.Clamp(seconds, 0f, 0.35f);
            if (shift.sqrMagnitude > limit * limit) return;
            Vector2 direction = Velocity.normalized;
            float travel = Vector2.Dot(shift, direction);
            if (travel < -Spec.Speed * _age) return;
            Vector2 end = point + direction * travel;
            if (ShotQuery.Ground(point, end, Spec.Radius, _owner.GroundMask, out RaycastHit2D hit))
            {
                point = hit.centroid;
                normal = hit.normal;
            }
            else point = end;
        }

        public void FinishAt(Vector2 point)
        {
            if (!IsServer || !IsSpawned) return;
            _body.position = point;
            Finish();
        }

        public void Finish(bool hit = false)
        {
            if (!IsServer || !IsSpawned) return;
            FinishRpc(_body.position, hit);
            NetworkObject.Despawn();
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        void FinishRpc(Vector2 point, bool hit) => _flight.Complete(point, 0f, hit);
    }
}
