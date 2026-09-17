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

    public sealed class NetCard : NetworkBehaviour, IShotLife
    {
        [SerializeField] FlyingCard _card;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] Collider2D _collider;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] MagicianCardFeedback _feedback;
        readonly NetworkVariable<CardState> _state = new NetworkVariable<CardState>();

        CardState _startState;
        Vector2 _velocity;
        float _spin;

        public CardState State => _state.Value;

        public void Init(NetPlayer owner, Suit suit, int rank, Vector2 direction, float effect, bool joker, bool mirror, uint action = 0)
        {
            _startState = new CardState
            {
                Caster = owner.NetworkObjectId, Action = action, Suit = suit, Rank = rank,
                Effect = effect, Joker = joker, Mirror = mirror
            };
            _velocity = direction * 12f;
            _spin = direction.x < 0f ? -720f : 720f;
            transform.localScale = Vector3.one * (mirror ? 0.18f : 0.3f);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) _state.Value = _startState;
            CardState state = _state.Value;
            NetPlayer owner = NetworkManager.SpawnManager.SpawnedObjects[state.Caster].GetComponent<NetPlayer>();
            bool anticipated = owner.IsOwner && !IsServer && state.Action != 0;
            owner.Cast.Cards.ConfigureShot(_card, _sprite, _feedback, state, !anticipated);
            if (anticipated && !owner.Cast.MatchShot(state.Action, 0, _sprite, _feedback)) _feedback.PlayLaunch();
            _card.SetLife(this);
            _card.enabled = IsServer;
            _collider.enabled = IsServer;
            if (!IsServer) return;
            Physics2D.IgnoreCollision(_collider, owner.Collider);
            _body.linearVelocity = _velocity;
            _body.angularVelocity = _spin;
        }

        void Update()
        {
            if (IsServer && IsSpawned && !NetGame.Current.CanFight) Finish();
        }

        public void Impact(Vector2 point, float radius)
        {
            if (IsServer) ImpactRpc(point, radius);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        void ImpactRpc(Vector2 point, float radius)
        {
            _feedback.PlayImpact(point, radius);
        }

        public void Finish()
        {
            if (IsServer && IsSpawned) NetworkObject.Despawn();
        }

        public override void OnNetworkDespawn()
        {
            _feedback.ReleaseTrail();
        }
    }
}