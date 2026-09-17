using System.Collections;
using System.Collections.Generic;
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
        readonly NetworkVariable<uint> _seed = new NetworkVariable<uint>();
        readonly NetworkVariable<double> _showUntil = new NetworkVariable<double>();
        readonly NetworkVariable<uint> _version = new NetworkVariable<uint>();
        struct Pending
        {
            public CastInput Input;
            public double At;
        }

        readonly Queue<Pending> _commands = new Queue<Pending>();
        readonly List<ShotTail> _finishes = new List<ShotTail>();
        CastView _preview;
        uint _queued;
        uint _readyTick;
        uint _throwTick;
        uint _battle;
        uint _rankStart;
        uint _suitStart;
        int _localRank = 1;
        int _localSuit;
        const float CastWait = 2.5f;
        uint _action;
        uint _confirmed;
        uint _epoch;
        double _cooldownUntil;
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
        public int Rank => IsOwner && !IsServer && (Anticipating || _rollingRank) ? _localRank : _rank.Value;
        public Suit Suit => (Suit)(IsOwner && !IsServer && (Anticipating || _rollingSuit) ? _localSuit : _suit.Value);
        public float CooldownScale { get; set; } = 1f;
        public IReadOnlyList<ShotTail> Finishes => _finishes;
        public void AddFinish(ShotTail tail) => _finishes.Add(tail);
        public void RemoveFinish(ShotTail tail) => _finishes.Remove(tail);
        public int Previews => _preview?.Count ?? 0;
        public int VisiblePreviews => _preview?.Visible ?? 0;
        public Vector2 Origin { get; private set; }
        public uint ShotTick { get; private set; }
        public double ShotLag { get; private set; }
        public int Shots { get; private set; }
        public int Matches { get; private set; }
        public int Rejections { get; private set; }
        public double ReadyIn => IsOwner && !IsServer
            ? System.Math.Max(System.Math.Max(0d, _cooldownUntil - Time.unscaledTimeAsDouble), Remaining(_player.CastTick))
            : Remaining(_player.InputSequence);
        double Remaining(uint tick) => _readyTick > tick ? (_readyTick - tick) * (double)Time.fixedDeltaTime : 0d;
        static uint Ticks(double seconds) => (uint)System.Math.Ceiling(System.Math.Max(0d, seconds) / Time.fixedDeltaTime);
        public double FeedbackAt { get; private set; } = -1d;
        public uint Action => _action;
        public uint Confirmed => _confirmed;
        public bool Charging => _rollingRank;
        public int DisplayHeld => Anticipating ? _localHeld : _held.Value;
        bool Anticipating => IsOwner && !IsServer && (_action > _confirmed || _confirmed > _version.Value) && Time.unscaledTimeAsDouble < _waitingUntil;
        float CardCooldown => _cardCooldown * CooldownScale;
        double Now => IsOwner && !IsServer ? NetworkManager.LocalTime.Time : NetworkManager.ServerTime.Time;
        float ResponseTime => CastWait;

        public override void OnNetworkSpawn()
        {
            if (IsServer) _seed.Value = (uint)Random.Range(1, int.MaxValue);
            _cards.enabled = false;
            _suits.enabled = false;
            _cooldown.gameObject.SetActive(false);
            _epoch = _player.Epoch;
            if (IsOwner && !IsServer) _preview = new CastView(_cards, _player.GroundMask, _cardPrefab.PreviewRadius);
        }

        uint Begin()
        {
            if (!Anticipating)
            {
                _localRank = _rank.Value;
                _localSuit = _suit.Value;
                _localHeld = _held.Value;
                _localNext = _next.Value;
                _localShow = _showUntil.Value;
            }
            _waitingUntil = Time.unscaledTimeAsDouble + ResponseTime;
            return ++_action;
        }

        public void Attack(bool pressed, Vector2 direction)
        {
            if (!IsSpawned || !IsOwner || !_player.CanAct || !NetMath.Finite(direction) || direction.sqrMagnitude < 0.001f) return;
            direction.Normalize();
            uint action = Begin();
            if (!IsServer)
            {
                if (_player.Job == PlayerJob.Witch && pressed && _localHeld >= 0 && Time.unscaledTimeAsDouble >= _throwAt && _player.CastTick >= _throwTick)
                {
                    _throwAt = Time.unscaledTimeAsDouble + 0.15d;
                    _throwTick = _player.CastTick + Ticks(0.15d);
                    int kind = _localHeld;
                    _localHeld = _localNext;
                    _localNext = -1;
                    Vector2 velocity = direction * 15f + Vector2.up * 3f;
                    int count = _witch.ProjectileCount;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f) * 24f;
                        Vector2 spread = Quaternion.Euler(0f, 0f, angle) * velocity;
                        _preview.Potion(action, i, _hand.transform.position, spread, _potionPrefab.Style, _stock.At(kind).sprite, _potionPrefab.Gravity, ResponseTime);
                    }
                    FeedbackAt = Time.unscaledTimeAsDouble;
                }
                else if (_player.Job == PlayerJob.Magician)
                {
                    if (pressed && !_rollingRank && ReadyIn <= 0d)
                    {
                        if (_rollingSuit) _localSuit = DrawSuit(_player.CastTick);
                        _rollingSuit = false;
                        _rollingRank = true;
                        _rankStart = _player.CastTick;
                        _localRank = DrawRank(_player.CastTick);
                        _localShow = Now + 1.4d;
                        _cards.ShowRank(Rank, true);
                        _suits.ShowSuit(Suit, true);
                        FeedbackAt = Time.unscaledTimeAsDouble;
                    }
                    else if (!pressed && _rollingRank)
                    {
                        _localRank = DrawRank(_player.CastTick);
                        _rollingRank = false;
                        _cooldownUntil = Time.unscaledTimeAsDouble + CardCooldown;
                        _readyTick = _player.CastTick + Ticks(CardCooldown);
                        _localShow = Now + 1.4d;
                        Origin = _player.Body.position;
                        ShotTick = _player.CastTick;
                        _preview.Card(action, Origin, direction, Suit, Rank, _magic.FireDelay, _cardPrefab.Gravity, ResponseTime);
                        FeedbackAt = Time.unscaledTimeAsDouble;
                    }
                }
            }
            Send(action, pressed ? CastKind.Press : CastKind.Release, direction);
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
            Send(Begin(), CastKind.Cancel, default);
        }

        public void Cycle(bool pressed)
        {
            if (!IsSpawned || !IsOwner || !_player.CanAct) return;
            uint action = Begin();
            if (!IsServer && _player.Job == PlayerJob.Magician && !_rollingRank)
            {
                if (pressed) _suitStart = _player.CastTick;
                else if (_rollingSuit) _localSuit = DrawSuit(_player.CastTick);
                _rollingSuit = pressed;
                if (pressed) _localSuit = DrawSuit(_player.CastTick);
                _localShow = Now + 1.4d;
                _suits.ShowSuit(Suit, true);
                FeedbackAt = Time.unscaledTimeAsDouble;
            }
            Send(action, pressed ? CastKind.Cycle : CastKind.StopCycle, default);
        }

        void Send(uint action, CastKind kind, Vector2 direction)
        {
            double viewTime = NetworkManager.ServerTime.Time;
            foreach (NetPlayer player in NetGame.Current.Players)
                if (player != _player) viewTime = player.ViewTime;
            CastRpc(new CastInput
            {
                Action = action, Epoch = _player.Epoch, Tick = _player.CastTick,
                Kind = kind, Direction = direction, ViewTime = viewTime
            }, _player.CastPacket);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void CastRpc(CastInput input, MotionPacket motion)
        {
            if (input.Action <= _queued) return;
            _queued = input.Action;
            if (_commands.Count >= 64)
            {
                while (_commands.Count > 0) Complete(_commands.Dequeue().Input.Action, false);
            }
            if (input.Epoch == motion.Epoch) _player.ReceiveCast(motion);
            _commands.Enqueue(new Pending { Input = input, At = Time.unscaledTimeAsDouble });
            Drain();
        }

        void Drain()
        {
            while (_commands.Count > 0)
            {
                Pending pending = _commands.Peek();
                CastInput input = pending.Input;
                bool valid = input.Epoch == _player.Epoch
                    && (input.Kind == CastKind.Cancel || _player.CanAct)
                    && input.Kind <= CastKind.Cancel
                    && !double.IsNaN(input.ViewTime) && !double.IsInfinity(input.ViewTime)
                    && input.Tick <= (ulong)_player.InputSequence + MotionHistory.Capacity
                    && Time.unscaledTimeAsDouble - pending.At < CastWait;
                if (valid && input.Tick > _player.InputSequence) return;
                _commands.Dequeue();
                Vector2 origin = default;
                valid &= _player.ReadCast(input.Epoch, input.Tick, out origin);
                bool accepted = valid && Apply(input, origin);
                Complete(input.Action, accepted);
            }
        }

        bool Apply(CastInput input, Vector2 origin)
        {
            if (input.Kind == CastKind.Cancel)
            {
                _rollingRank = _rollingSuit = false;
                _showUntil.Value = 0d;
                return true;
            }
            if (input.Kind == CastKind.Cycle || input.Kind == CastKind.StopCycle)
            {
                if (_player.Job != PlayerJob.Magician || _rollingRank) return false;
                bool cycling = input.Kind == CastKind.Cycle;
                if (cycling) _suitStart = input.Tick;
                if (cycling || _rollingSuit) _suit.Value = DrawSuit(input.Tick);
                _rollingSuit = cycling;
                _tickAt = Now;
                _showUntil.Value = Now + 1.4d;
                return true;
            }
            if (!NetMath.Finite(input.Direction) || input.Direction.sqrMagnitude < 0.001f) return false;
            bool pressed = input.Kind == CastKind.Press;
            if (_player.Job == PlayerJob.Witch)
                return pressed && Throw(input.Action, input.Direction.normalized, origin, input.Tick);
            if (_player.Job != PlayerJob.Magician) return false;
            if (pressed && !_rollingRank && input.Tick >= _readyTick)
            {
                if (_rollingSuit) _suit.Value = DrawSuit(input.Tick);
                _rollingSuit = false;
                _rollingRank = true;
                _rankStart = input.Tick;
                _rank.Value = DrawRank(input.Tick);
                _tickAt = Now + 0.15f * _magic.TickIntervalMultiplier;
                _showUntil.Value = Now + 1.4f;
                return true;
            }
            if (!pressed && _rollingRank)
            {
                _rank.Value = DrawRank(input.Tick);
                _rollingRank = false;
                _readyTick = input.Tick + Ticks(CardCooldown);
                _showUntil.Value = Now + 1.4f;
                ShotTick = input.Tick;
                StartCoroutine(Fire(input, origin, Suit, Rank));
                return true;
            }
            return false;
        }

        void Complete(uint action, bool accepted)
        {
            _version.Value = action;
            ResultRpc(action, _player.Epoch, accepted, _rollingRank, _rollingSuit, _rankStart, _suitStart, _readyTick, _showUntil.Value, _held.Value, _next.Value);
            if (IsOwner && accepted) FeedbackAt = Time.unscaledTimeAsDouble;
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        void ResultRpc(uint action, uint epoch, bool accepted, bool rank, bool suit, uint rankStart, uint suitStart, uint ready, double show, int held, int next)
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
            _rankStart = rankStart;
            _suitStart = suitStart;
            _readyTick = ready;
            if (!accepted) _cooldownUntil = System.Math.Max(_cooldownUntil, Time.unscaledTimeAsDouble + Remaining(_player.CastTick));
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

        uint Draw(uint tick, uint start, uint salt)
        {
            uint interval = System.Math.Max(1u, Ticks(0.15f * _magic.TickIntervalMultiplier));
            uint step = tick >= start ? (tick - start) / interval : 0u;
            uint value = _seed.Value ^ start * 0x9e3779b9u ^ step * 0x85ebca6bu ^ salt;
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            return value ^ (value >> 16);
        }

        int DrawSuit(uint tick) => (int)(Draw(tick, _suitStart, 47u) % 4u);

        int DrawRank(uint tick)
        {
            int rank = 1 + (int)(Draw(tick, _rankStart, 113u) % 10u);
            return _magic.Has(MagicianAugmentType.DoubleDraw)
                ? Mathf.Max(rank, 1 + (int)(Draw(tick, _rankStart, 211u) % 10u)) : rank;
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (_epoch != _player.Epoch)
            {
                _epoch = _player.Epoch;
                if (!_player.CanAct) _preview?.Clear();
                _rollingRank = _rollingSuit = false;
                _localShow = _waitingUntil = 0d;
                if (IsServer)
                {
                    _showUntil.Value = 0d;
                }
            }
            if (!_player.CanAct)
            {
                _battle++;
                _preview?.Clear();
                _rollingRank = _rollingSuit = false;
                _localShow = 0d;
            }
            else if (IsOwner && !IsServer && _action > _confirmed && Time.unscaledTimeAsDouble >= _waitingUntil)
            {
                _rollingRank = _rollingSuit = false;
                _localShow = 0d;
            }
            if (IsServer) { Drain(); Tick(); }
            else if (IsOwner)
            {
                if (_rollingRank) _localRank = DrawRank(_player.CastTick);
                if (_rollingSuit) _localSuit = DrawSuit(_player.CastTick);
            }
            bool magician = _player.Job == PlayerJob.Magician;
            bool anticipating = Anticipating;
            double show = anticipating ? _localShow : _showUntil.Value;
            bool visible = magician && _player.CanAct && (Now < show || IsOwner && (_rollingRank || _rollingSuit));
            _cards.ShowRank(Rank, visible);
            _suits.ShowSuit(Suit, visible);
            ShowPotion(_hand, anticipating ? _localHeld : _held.Value);
            ShowPotion(_reserve, anticipating ? _localNext : _next.Value);
            if (IsOwner && magician && Mouse.current != null)
            {
                _cooldown.SetProgress(Mathf.Clamp01((float)ReadyIn / Mathf.Max(0.01f, CardCooldown)));
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
            if (_rollingRank) _rank.Value = DrawRank(_player.InputSequence);
            else _suit.Value = DrawSuit(_player.InputSequence);
        }

        void ShowPotion(SpriteRenderer view, int kind)
        {
            bool visible = _player.Job == PlayerJob.Witch && kind >= 0;
            view.enabled = visible;
            if (visible) view.sprite = _stock.At(kind).sprite;
        }

        IEnumerator Fire(CastInput input, Vector2 origin, Suit suit, int rank)
        {
            uint battle = _battle;
            if (_magic.FireDelay > 0f) yield return new WaitForSeconds(_magic.FireDelay);
            if (!_player.CanAct || battle != _battle)
            {
                RevokeRpc(input.Action);
                yield break;
            }
            bool joker = _magic.ConsumeJoker();
            double allowance = Mathf.Min(1.25f, NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId) * 0.001f + 0.15f);
            double lag = IsOwner ? 0d : System.Math.Clamp(NetGame.Current.PhysicsTime - input.ViewTime - _magic.FireDelay, 0d, allowance);
            SpawnCard(suit, joker ? 10 : rank, input.Direction.normalized, 1f, joker, false, input.Action, origin, lag);
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        void RevokeRpc(uint action) => _preview?.Reject(action);

        public void Mirror(Suit suit, int rank, Vector2 direction, float effect)
        {
            if (IsServer && _player.CanAct) SpawnCard(suit, rank, direction, effect, false, true, 0, _player.Body.position);
        }

        void SpawnCard(Suit suit, int rank, Vector2 direction, float effect, bool joker, bool mirror, uint action, Vector2 origin, double lag = 0d)
        {
            Origin = origin;
            ShotLag = lag;
            Shots++;
            NetCard card = Instantiate(_cardPrefab, origin, Quaternion.identity);
            card.Init(_player, suit, rank, direction, effect, joker, mirror, action, lag);
            card.NetworkObject.Spawn(true);
        }

        bool Throw(uint action, Vector2 direction, Vector2 position, uint tick)
        {
            if (_held.Value < 0 || tick < _throwTick) return false;
            _throwTick = tick + Ticks(0.15d);
            int kind = _held.Value;
            _held.Value = _next.Value;
            _next.Value = -1;
            int count = _witch.ProjectileCount;
            Vector2 velocity = direction * 15f + Vector2.up * 3f;
            Vector2 origin = position + (Vector2)(_hand.transform.position - _player.View.position);
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
            foreach (ShotTail tail in _finishes) if (tail != null) Destroy(tail.gameObject);
            _finishes.Clear();
        }
    }
}
