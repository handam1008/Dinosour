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
        readonly NetworkVariable<uint> _version = new NetworkVariable<uint>();
        CastView _preview;
        uint _action;
        uint _confirmed;
        uint _epoch;
        double _localReady;
        double _localShow;
        double _waitingUntil;
        int _localHeld;
        int _localNext;
        bool _rollingSuit;
        bool _rollingRank;
        double _tickAt;
        double _brewAt;
        double _throwAt;

        public NumberRoller Cards => _cards;
        public int Held => _held.Value;
        public int Rank => _rank.Value;
        public Suit Suit => (Suit)_suit.Value;
        public float CooldownScale { get; set; } = 1f;
        public int Previews => _preview?.Count ?? 0;
        public int VisiblePreviews => _preview?.Visible ?? 0;
        public int Matches { get; private set; }
        public int Rejections { get; private set; }
        public double ReadyIn => System.Math.Max(0d, (Anticipating ? _localReady : _readyAt.Value) - Now);
        public double FeedbackAt { get; private set; } = -1d;
        public uint Action => _action;
        public uint Confirmed => _confirmed;
        public bool Charging => _rollingRank;
        public int DisplayHeld => Anticipating ? _localHeld : _held.Value;
        bool Anticipating => IsOwner && !IsServer && (_action > _confirmed || _confirmed > _version.Value) && Now < _waitingUntil;
        float CardCooldown => _cardCooldown * CooldownScale;
        double Now => NetworkManager.ServerTime.Time;

        public override void OnNetworkSpawn()
        {
            _cards.enabled = false;
            _suits.enabled = false;
            _cooldown.gameObject.SetActive(false);
            _epoch = _player.Epoch;
            if (IsOwner && !IsServer) _preview = new CastView(_cards, _player.GroundMask);
        }

        uint Begin()
        {
            if (!Anticipating)
            {
                _localHeld = _held.Value;
                _localNext = _next.Value;
                _localReady = _readyAt.Value;
                _localShow = _showUntil.Value;
            }
            _waitingUntil = Now + 0.8d;
            return ++_action;
        }

        public void Attack(bool pressed, Vector2 direction)
        {
            if (!IsSpawned || !IsOwner || !_player.CanAct || !NetMath.Finite(direction) || direction.sqrMagnitude < 0.001f) return;
            direction.Normalize();
            uint action = Begin();
            if (!IsServer)
            {
                if (_player.Job == PlayerJob.Witch && pressed && _localHeld >= 0 && Now >= _throwAt)
                {
                    _throwAt = Now + 0.15d;
                    int kind = _localHeld;
                    _localHeld = _localNext;
                    _localNext = -1;
                    Vector2 velocity = direction * 15f + Vector2.up * 3f;
                    int count = _witch.ProjectileCount;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f) * 24f;
                        Vector2 spread = Quaternion.Euler(0f, 0f, angle) * velocity;
                        _preview.Potion(action, i, _hand.transform.position, spread, _potionPrefab.Style, _stock.At(kind).sprite, _potionPrefab.Gravity);
                    }
                    FeedbackAt = Time.unscaledTimeAsDouble;
                }
                else if (_player.Job == PlayerJob.Magician)
                {
                    if (pressed && !_rollingRank && Now >= System.Math.Max(_readyAt.Value, _localReady))
                    {
                        _rollingSuit = false;
                        _rollingRank = true;
                        _localShow = Now + 1.4d;
                        _cards.ShowRank(_rank.Value, true);
                        _suits.ShowSuit(Suit, true);
                        FeedbackAt = Time.unscaledTimeAsDouble;
                    }
                    else if (!pressed && _rollingRank)
                    {
                        _rollingRank = false;
                        _localReady = Now + CardCooldown;
                        _localShow = Now + 1.4d;
                        _preview.Card(action, _player.View.position, direction, Suit, Rank, _magic.FireDelay, _cardPrefab.Gravity);
                        FeedbackAt = Time.unscaledTimeAsDouble;
                    }
                }
            }
            AttackRpc(action, _player.Epoch, pressed, direction);
        }

        public void Cancel()
        {
            if (!IsSpawned || !IsOwner) return;
            _preview?.Clear();
            if (!IsServer)
            {
                _rollingRank = _rollingSuit = false;
                _localShow = 0d;
            }
            CancelRpc(Begin(), _player.Epoch);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void CancelRpc(uint action, uint epoch)
        {
            if (action <= _version.Value) return;
            if (epoch != _player.Epoch) { Complete(action, false); return; }
            _rollingRank = false;
            _rollingSuit = false;
            _showUntil.Value = 0;
            StopAllCoroutines();
            Complete(action, true);
        }

        public void Cycle(bool pressed)
        {
            if (!IsSpawned || !IsOwner || !_player.CanAct) return;
            uint action = Begin();
            if (!IsServer && _player.Job == PlayerJob.Magician && !_rollingRank)
            {
                _rollingSuit = pressed;
                _localShow = Now + 1.4d;
                _suits.ShowSuit(Suit, true);
                FeedbackAt = Time.unscaledTimeAsDouble;
            }
            CycleRpc(action, _player.Epoch, pressed);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void CycleRpc(uint action, uint epoch, bool pressed)
        {
            if (action <= _version.Value) return;
            bool accepted = epoch == _player.Epoch && _player.CanAct && _player.Job == PlayerJob.Magician && !_rollingRank;
            if (accepted)
            {
                _rollingSuit = pressed;
                _tickAt = Now;
                _showUntil.Value = Now + 1.4f;
            }
            Complete(action, accepted);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void AttackRpc(uint action, uint epoch, bool pressed, Vector2 direction)
        {
            if (action <= _version.Value) return;
            bool accepted = false;
            if (epoch == _player.Epoch && _player.CanAct && NetMath.Finite(direction) && direction.sqrMagnitude >= 0.001f)
            {
                if (_player.Job == PlayerJob.Witch)
                    accepted = pressed && Throw(action, direction.normalized);
                else if (_player.Job == PlayerJob.Magician)
                {
                    if (pressed && !_rollingRank && Now >= _readyAt.Value)
                    {
                        _rollingSuit = false;
                        _rollingRank = true;
                        _rank.Value = RollRank();
                        _tickAt = Now + 0.15f * _magic.TickIntervalMultiplier;
                        _showUntil.Value = Now + 1.4f;
                        accepted = true;
                    }
                    else if (!pressed && _rollingRank)
                    {
                        _rollingRank = false;
                        _readyAt.Value = Now + CardCooldown;
                        _showUntil.Value = Now + 1.4f;
                        StartCoroutine(Fire(action, Suit, Rank, direction.normalized));
                        accepted = true;
                    }
                }
            }
            Complete(action, accepted);
        }

        void Complete(uint action, bool accepted)
        {
            _version.Value = action;
            ResultRpc(action, _player.Epoch, accepted, _rollingRank, _rollingSuit, _readyAt.Value, _showUntil.Value, _held.Value, _next.Value);
            if (IsOwner && accepted) FeedbackAt = Time.unscaledTimeAsDouble;
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        void ResultRpc(uint action, uint epoch, bool accepted, bool rank, bool suit, double ready, double show, int held, int next)
        {
            if (!accepted || epoch != _player.Epoch)
            {
                Rejections++;
                _preview?.Reject(action);
            }
            if (action <= _confirmed) return;
            _confirmed = action;
            if (IsServer || action != _action || epoch != _player.Epoch) return;
            _rollingRank = rank;
            _rollingSuit = suit;
            _localReady = ready;
            _localShow = show;
            _localHeld = held;
            _localNext = next;
        }

        public bool MatchShot(uint action, int part, ShotSync shot)
        {
            bool matched = action != 0 && _preview != null && _preview.Match(action, part, shot);
            if (matched) Matches++;
            return matched;
        }

        public void ReadPreviews(System.Action<uint, int, Vector2, bool> read) => _preview?.Read(read);

        int RollRank()
        {
            int rank = Random.Range(1, 11);
            return _magic.Has(MagicianAugmentType.DoubleDraw) ? Mathf.Max(rank, Random.Range(1, 11)) : rank;
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (_epoch != _player.Epoch)
            {
                _epoch = _player.Epoch;
                _preview?.Clear();
                _rollingRank = _rollingSuit = false;
                _localShow = _waitingUntil = 0d;
                if (IsServer)
                {
                    StopAllCoroutines();
                    _showUntil.Value = 0d;
                }
            }
            if (!_player.CanAct)
            {
                _preview?.Clear();
                _rollingRank = _rollingSuit = false;
                _localShow = 0d;
            }
            else if (IsOwner && !IsServer && _action > _confirmed && Now >= _waitingUntil)
            {
                _rollingRank = _rollingSuit = false;
                _localShow = 0d;
            }
            if (IsServer) Tick();
            bool magician = _player.Job == PlayerJob.Magician;
            bool anticipating = Anticipating;
            double show = anticipating ? _localShow : _showUntil.Value;
            bool visible = magician && _player.CanAct && (Now < show || IsOwner && (_rollingRank || _rollingSuit));
            _cards.ShowRank(_rank.Value, visible);
            _suits.ShowSuit((Suit)_suit.Value, visible);
            ShowPotion(_hand, anticipating ? _localHeld : _held.Value);
            ShowPotion(_reserve, anticipating ? _localNext : _next.Value);
            if (IsOwner && magician && Mouse.current != null)
            {
                double ready = anticipating ? _localReady : _readyAt.Value;
                _cooldown.SetProgress(Mathf.Clamp01((float)(ready - Now) / Mathf.Max(0.01f, CardCooldown)));
            }
        }

        void LateUpdate()
        {
            _preview?.Tick(Time.deltaTime);
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
                _brewAt = Now + _witch.CycleInterval(_cycle) * CooldownScale;
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

        IEnumerator Fire(uint action, Suit suit, int rank, Vector2 direction)
        {
            uint epoch = _player.Epoch;
            if (_magic.FireDelay > 0f) yield return new WaitForSeconds(_magic.FireDelay);
            if (!_player.CanAct || epoch != _player.Epoch) yield break;
            bool joker = _magic.ConsumeJoker();
            SpawnCard(suit, joker ? 10 : rank, direction, 1f, joker, false, action);
        }

        public void Mirror(Suit suit, int rank, Vector2 direction, float effect)
        {
            if (IsServer && _player.CanAct) SpawnCard(suit, rank, direction, effect, false, true, 0);
        }

        void SpawnCard(Suit suit, int rank, Vector2 direction, float effect, bool joker, bool mirror, uint action)
        {
            NetCard card = Instantiate(_cardPrefab, _player.Body.position, Quaternion.identity);
            card.Init(_player, suit, rank, direction, effect, joker, mirror, action);
            card.NetworkObject.Spawn(true);
        }

        bool Throw(uint action, Vector2 direction)
        {
            if (_held.Value < 0 || Now < _throwAt) return false;
            _throwAt = Now + 0.15f;
            int kind = _held.Value;
            _held.Value = _next.Value;
            _next.Value = -1;
            int count = _witch.ProjectileCount;
            Vector2 velocity = direction * 15f + Vector2.up * 3f;
            Vector2 origin = _player.Body.position + (Vector2)(_hand.transform.position - _player.View.position);
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f) * 24f;
                Vector2 spread = Quaternion.Euler(0f, 0f, angle) * velocity;
                NetPotion potion = Instantiate(_potionPrefab, origin, Quaternion.identity);
                potion.Init(_player, kind, spread, _witch.BuildModifiers(), action, i);
                potion.NetworkObject.Spawn(true);
            }
            return true;
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();
            _preview?.Clear();
        }
    }
}
