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
        float _flightAge;
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
            transform.localScale = new Vector3(Spec.Scale, Spec.Scale * Spec.Aspect, 1f);
            _flight.AlignVelocity = Spec.Gravity != 0f && Spec.Spin == 0f;
            _flight.AngleOffset = Spec.Style == 3 ? -90f : 0f;
            _flight.ExtraGravity = Spec.ExtraGravity;
            _flight.GravityDelay = Spec.GravityDelay;
            _flight.Bind(_owner, _action.Value, 0, Spec.Radius);
            if (_owner.Cast.Weapon is GunCast gun) gun.Effects.Attach(_sprite, Spec.Charged);
            if (!IsServer && _owner.IsOwner && Spec.Stick && _owner.Cast.Weapon is KnifeCast knife) knife.Track(this);
            if (!IsServer) return;
            _previous = _body.position;
            _previousTime = NetGame.Current.PhysicsTime;
            _body.gravityScale = Spec.Gravity;
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
            _flightAge += Time.fixedDeltaTime;
            _flight.GravityDelay = Mathf.Max(0f, Spec.GravityDelay - _flightAge);
            float falling = Mathf.Clamp(_flightAge - Spec.GravityDelay, 0f, Time.fixedDeltaTime);
            _body.linearVelocity += Vector2.down * (Spec.ExtraGravity * falling);
            if (_flight.AlignVelocity && _body.linearVelocity.sqrMagnitude > 0.000001f)
                _body.rotation = Mathf.Atan2(_body.linearVelocity.y, _body.linearVelocity.x) * Mathf.Rad2Deg + (Spec.Style == 3 ? -90f : 0f);
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
                        _body.linearVelocity = -_body.linearVelocity.normalized * sword.ReflectSpeed;
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
                    _body.gravityScale = 0f;
                    _flight.ExtraGravity = 0f;
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
            float window = Mathf.Clamp(seconds, 0f, 0.35f);
            Vector2 gravity = Physics2D.gravity * _body.gravityScale;
            Vector2 velocity = Velocity + gravity * (Time.fixedDeltaTime * 0.5f);
            float limit = velocity.magnitude * window + gravity.magnitude * (0.5f * window * window);
            if (shift.sqrMagnitude > limit * limit) return;
            float time = RecallTime(shift, velocity, gravity, window);
            if (time < -_age || Mathf.Abs(time) > window) return;
            Vector2 origin = point;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(time) / Time.fixedDeltaTime));
            for (int i = 1; i <= steps; i++)
            {
                Vector2 end = origin + Flight(velocity, gravity, time * i / steps);
                if (ShotQuery.Ground(point, end, Spec.Radius, _owner.GroundMask, out RaycastHit2D hit))
                {
                    point = hit.centroid;
                    normal = hit.normal;
                    return;
                }
                point = end;
            }
        }

        static Vector2 Flight(Vector2 velocity, Vector2 gravity, float time)
            => velocity * time + gravity * (0.5f * time * time);

        static float RecallTime(Vector2 shift, Vector2 velocity, Vector2 gravity, float window)
        {
            if (gravity.sqrMagnitude < 0.000001f)
                return velocity.sqrMagnitude > 0.000001f ? Vector2.Dot(shift, velocity) / velocity.sqrMagnitude : 0f;
            float best = 0f;
            float distance = shift.sqrMagnitude;
            float step = window / 16f;
            for (int i = -16; i <= 16; i++)
            {
                float time = step * i;
                float next = (Flight(velocity, gravity, time) - shift).sqrMagnitude;
                if (next >= distance) continue;
                best = time;
                distance = next;
            }
            float left = Mathf.Max(-window, best - step);
            float right = Mathf.Min(window, best + step);
            for (int i = 0; i < 20; i++)
            {
                float a = Mathf.Lerp(left, right, 1f / 3f);
                float b = Mathf.Lerp(left, right, 2f / 3f);
                if ((Flight(velocity, gravity, a) - shift).sqrMagnitude < (Flight(velocity, gravity, b) - shift).sqrMagnitude) right = b;
                else left = a;
            }
            return (left + right) * 0.5f;
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
