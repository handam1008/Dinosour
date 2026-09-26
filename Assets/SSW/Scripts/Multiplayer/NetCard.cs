using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public struct CardState : INetworkSerializable, System.IEquatable<CardState>
    {
        public ulong Caster;
        public uint Action;
        public Suit Suit;
        public int Rank;
        public float Effect;
        public bool Joker;
        public bool Mirror;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Caster);
            serializer.SerializeValue(ref Action);
            serializer.SerializeValue(ref Suit);
            serializer.SerializeValue(ref Rank);
            serializer.SerializeValue(ref Effect);
            serializer.SerializeValue(ref Joker);
            serializer.SerializeValue(ref Mirror);
        }

        public bool Equals(CardState other) => Caster == other.Caster && Action == other.Action && Suit == other.Suit
            && Rank == other.Rank && Effect == other.Effect && Joker == other.Joker && Mirror == other.Mirror;
    }

    [DefaultExecutionOrder(150)]
    public sealed class NetCard : NetworkBehaviour, IShotLife
    {
        [SerializeField] ShotSync _flight;
        [SerializeField] FlyingCard _card;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] Collider2D _collider;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] MagicianCardFeedback _feedback;
        readonly NetworkVariable<CardState> _state = new NetworkVariable<CardState>();

        CardState _startState;
        Vector2 _velocity;
        float _spin;
        NetPlayer _owner;
        Vector2 _previous;
        double _previousTime;
        double _lag;
        bool _returning;

        public CardState State => _state.Value;
        public float Gravity => _body.gravityScale;
        public float PreviewRadius(float scale) => ((CircleCollider2D)_collider).radius * scale;
        public float Radius => ((CircleCollider2D)_collider).radius * Mathf.Abs(transform.lossyScale.x);

        public void Init(NetPlayer owner, Suit suit, int rank, Vector2 direction, float effect, bool joker, bool mirror, uint action = 0, double lag = 0d)
        {
            _lag = lag;
            _startState = new CardState
            {
                Caster = owner.NetworkObjectId, Action = action, Suit = suit, Rank = rank,
                Effect = effect, Joker = joker, Mirror = mirror
            };
            _velocity = direction * owner.Stats.Flight.Speed;
            _spin = (direction.x < 0f ? -1f : 1f) * owner.Stats.Flight.Spin;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) _state.Value = _startState;
            CardState state = _state.Value;
            NetPlayer owner = NetworkManager.SpawnManager.SpawnedObjects[state.Caster].GetComponent<NetPlayer>();
            _owner = owner;
            _feedback.SoundEnabled = false;
            owner.Cast.Cards.ConfigureShot(_card, _sprite, _feedback, state, IsServer);
            FlightStats stats = owner.Stats.Flight;
            transform.localScale = new Vector3(stats.Scale, stats.Scale * stats.Aspect, 1f)
                * (state.Mirror ? owner.Stats.MirrorScale : 1f);
            _body.gravityScale = stats.Gravity;
            _card.SetLifetime(stats.Life);
            _card.SetDamage(owner.Stats.Damage);
            _flight.Bind(owner, state.Action, 0, Radius);
            _card.SetLife(this);
            _card.enabled = IsServer;
            _collider.enabled = false;
            if (!IsServer) return;
            _card.UseSweep();
            _previous = _body.position;
            _previousTime = NetGame.Current.PhysicsTime;
            _body.linearVelocity = _velocity;
            _body.angularVelocity = _spin;
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || !NetGame.Current.CanFight) return;
            if (_card.Returning && !_returning)
            {
                _returning = true;
                _lag = 0d;
                _flight.Redirect();
            }
            double time = NetGame.Current.PhysicsTime;
            Vector2 next = _body.position;
            Collider2D contact = null;
            float nearest = float.PositiveInfinity;
            Vector2 point = next;
            if (!_returning && ShotQuery.Ground(_previous, next, Radius, _owner.GroundMask, out RaycastHit2D ground))
            {
                contact = ground.collider;
                float distance = Vector2.Distance(_previous, next);
                nearest = distance > 0.000001f ? ground.distance / distance : 0f;
                point = ground.centroid;
            }
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == _owner || !target.CanAct) continue;
                if (!target.SweepHit(_previous, next, _previousTime - _lag, time - _lag, Radius, out float fraction)
                    || fraction >= nearest) continue;
                nearest = fraction;
                point = Vector2.Lerp(_previous, next, fraction);
                contact = target.Collider;
            }
            if (contact != null)
            {
                _body.position = point;
                _card.Contact(contact, point);
            }
            _previous = _body.position;
            _previousTime = time;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            _flight.Terrain = !_card.Returning;
            if (!NetGame.Current.CanFight) Finish();
        }

        public void Impact(Vector2 point, float radius, bool terminal = true)
        {
            if (!IsServer) return;
            ImpactRpc(point, radius, terminal);
            _owner.Cast.ImpactSound();
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        void ImpactRpc(Vector2 point, float radius, bool terminal)
        {
            if (terminal) _flight.Complete(point, radius, true);
            else _feedback.PlayEnvironmentImpact(point);
        }

        public void Finish()
        {
            if (!IsServer || !IsSpawned) return;
            FinishRpc(_body.position);
            NetworkObject.Despawn();
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        void FinishRpc(Vector2 point) => _flight.Complete(point, 0f, false);

        public override void OnNetworkDespawn()
        {
            _feedback.ReleaseTrail();
        }
    }
}
