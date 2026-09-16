using System.Collections;
using RYU._01.Script.Argument;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public sealed class NetCast : NetworkBehaviour
    {
        [SerializeField] NetPlayer _player;
        [SerializeField] NumberRoller _cards;
        [SerializeField] SuitSelector _suits;
        [SerializeField] MagicianAugmentController _magic;
        [SerializeField] WitchAugmentController _witch;
        [SerializeField] NetCard _cardPrefab;
        [SerializeField] NetPotion _potionPrefab;
        [SerializeField] NetStock _stock;
        [SerializeField] SpriteRenderer _hand;
        [SerializeField] SpriteRenderer _reserve;
        [SerializeField] CooldownCursorUI _cooldown;
        [SerializeField] float _cardCooldown = 3f;
        [SerializeField] float _cycle = 1.5f;
        readonly NetworkVariable<int> _suit = new NetworkVariable<int>();
        readonly NetworkVariable<int> _rank = new NetworkVariable<int>(1);
        readonly NetworkVariable<int> _held = new NetworkVariable<int>(-1);
        readonly NetworkVariable<int> _next = new NetworkVariable<int>(-1);
        readonly NetworkVariable<double> _readyAt = new NetworkVariable<double>();
        readonly NetworkVariable<double> _showUntil = new NetworkVariable<double>();
        bool _rollingSuit;
        bool _rollingRank;
        double _tickAt;
        double _brewAt;
        double _throwAt;

        public NumberRoller Cards => _cards;
        public int Held => _held.Value;
        public int Rank => _rank.Value;
        public Suit Suit => (Suit)_suit.Value;
        double Now => NetworkManager.ServerTime.Time;

        public override void OnNetworkSpawn()
        {
            _cards.enabled = false;
            _suits.enabled = false;
            _cooldown.gameObject.SetActive(false);
        }

        public void Attack(bool pressed, Vector2 direction)
        {
            if (IsSpawned && IsOwner && _player.CanAct) AttackRpc(pressed, direction);
        }

        public void Cancel()
        {
            if (IsSpawned && IsOwner) CancelRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void CancelRpc()
        {
            _rollingRank = false;
            _rollingSuit = false;
            _showUntil.Value = 0;
        }

        public void Cycle(bool pressed)
        {
            if (IsSpawned && IsOwner && _player.CanAct) CycleRpc(pressed);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void CycleRpc(bool pressed)
        {
            if (!_player.CanAct || _player.Job != PlayerJob.Magician || _rollingRank) return;
            _rollingSuit = pressed;
            _tickAt = Now;
            _showUntil.Value = Now + 1.4f;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void AttackRpc(bool pressed, Vector2 direction)
        {
            if (!_player.CanAct || !NetMath.Finite(direction) || direction.sqrMagnitude < 0.001f) return;
            if (_player.Job == PlayerJob.Witch)
            {
                if (pressed) Throw(direction.normalized);
                return;
            }
            if (_player.Job != PlayerJob.Magician) return;
            if (pressed)
            {
                if (_rollingRank || Now < _readyAt.Value) return;
                _rollingSuit = false;
                _rollingRank = true;
                _rank.Value = RollRank();
                _tickAt = Now + 0.15f * _magic.TickIntervalMultiplier;
                _showUntil.Value = Now + 1.4f;
                return;
            }
            if (!_rollingRank) return;
            _rollingRank = false;
            _readyAt.Value = Now + _cardCooldown;
            _showUntil.Value = Now + 1.4f;
            StartCoroutine(Fire((Suit)_suit.Value, _rank.Value, direction.normalized));
        }

        int RollRank()
        {
            int rank = Random.Range(1, 11);
            return _magic.Has(MagicianAugmentType.DoubleDraw) ? Mathf.Max(rank, Random.Range(1, 11)) : rank;
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (IsServer) Tick();
            bool magician = _player.Job == PlayerJob.Magician;
            _cards.ShowRank(_rank.Value, magician && Now < _showUntil.Value);
            _suits.ShowSuit((Suit)_suit.Value, magician && Now < _showUntil.Value);
            ShowPotion(_hand, _held.Value);
            ShowPotion(_reserve, _next.Value);
            if (IsOwner && magician && Mouse.current != null)
                _cooldown.SetProgress(Mathf.Clamp01((float)(_readyAt.Value - Now) / _cardCooldown));
        }

        void Tick()
        {
            if (!_player.CanAct)
            {
                _rollingRank = false;
                _rollingSuit = false;
                return;
            }
            if (_player.Job == PlayerJob.Witch)
            {
                if (Now < _brewAt) return;
                _brewAt = Now + _witch.CycleInterval(_cycle);
                int count = _stock.BaseCount + _witch.Unlocked.Count;
                int roll = Random.Range(0, count);
                int kind = roll < _stock.BaseCount
                    ? _stock.BaseAt(roll)
                    : _stock.IndexOf(_witch.Unlocked[roll - _stock.BaseCount]);
                if (_held.Value < 0) _held.Value = kind;
                else
                {
                    if (_next.Value >= 0) _held.Value = _next.Value;
                    _next.Value = kind;
                }
                return;
            }
            if (!_rollingSuit && !_rollingRank || Now < _tickAt) return;
            _tickAt = Now + 0.15f * _magic.TickIntervalMultiplier;
            _showUntil.Value = Now + 1.4f;
            if (_rollingRank) _rank.Value = RollRank();
            else _suit.Value = Random.Range(0, 4);
        }

        void ShowPotion(SpriteRenderer view, int kind)
        {
            bool visible = _player.Job == PlayerJob.Witch && kind >= 0;
            view.enabled = visible;
            if (visible) view.sprite = _stock.At(kind).sprite;
        }

        IEnumerator Fire(Suit suit, int rank, Vector2 direction)
        {
            if (_magic.FireDelay > 0f) yield return new WaitForSeconds(_magic.FireDelay);
            if (!_player.CanAct) yield break;
            bool joker = _magic.ConsumeJoker();
            SpawnCard(suit, joker ? 10 : rank, direction, 1f, joker, false);
        }

        public void Mirror(Suit suit, int rank, Vector2 direction, float effect)
        {
            if (IsServer && _player.CanAct) SpawnCard(suit, rank, direction, effect, false, true);
        }

        void SpawnCard(Suit suit, int rank, Vector2 direction, float effect, bool joker, bool mirror)
        {
            NetCard card = Instantiate(_cardPrefab, transform.position, Quaternion.identity);
            card.Init(_player, suit, rank, direction, effect, joker, mirror);
            card.NetworkObject.Spawn(true);
        }

        void Throw(Vector2 direction)
        {
            if (_held.Value < 0 || Now < _throwAt) return;
            _throwAt = Now + 0.15f;
            int kind = _held.Value;
            _held.Value = _next.Value;
            _next.Value = -1;
            int count = _witch.ProjectileCount;
            Vector2 velocity = direction * 15f + Vector2.up * 3f;
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f) * 24f;
                Vector2 spread = Quaternion.Euler(0f, 0f, angle) * velocity;
                NetPotion potion = Instantiate(_potionPrefab, _hand.transform.position, Quaternion.identity);
                potion.Init(_player, kind, spread, _witch.BuildModifiers());
                potion.NetworkObject.Spawn(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();
        }
    }
}