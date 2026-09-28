using System.Collections.Generic;
using JJW.Script.Augments;
using JJW.Script.Jackpot;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class CoinCast : JobCast, IDamageDealtListener
    {
        struct RollResult
        {
            public JackpotResultType Main;
            public JackpotResultType Old;
        }

        [SerializeField] CoinFx _effects;
        readonly HashSet<GamblerAugmentType> _augments = new HashSet<GamblerAugmentType>();
        readonly List<float> _damageUntil = new List<float>();
        readonly Queue<RollResult> _rolls = new Queue<RollResult>();
        RollResult _rolling;
        uint _rollId;
        float _resolveAt;
        float _nextRollAt;
        bool _spinning;
        bool _resolved;
        bool _jackpotPending;
        float _jackpotUntil;
        float _healAt;
        float _slowUntil;
        float _stealUntil;
        float _probability;
        int _stacks;
        public override PlayerJob Job => PlayerJob.Gambler;
        public bool Has(GamblerAugmentType type) => _augments.Contains(type);
        public bool Jackpot => Time.time < _jackpotUntil;
        public int Stacks => _stacks;
        public CoinFx Effects => _effects;
        public int PendingRolls => _rolls.Count + (_spinning && !_resolved ? 1 : 0);
        public float Probability => Mathf.Min(10f, (Has(GamblerAugmentType.MoreChances) ? 3f : 5f)
            + (Has(GamblerAugmentType.Luck) ? 0.5f : 0f) + (Has(GamblerAugmentType.ProbabilityShift) ? _probability : 0f));
        public override float DamageScale => Mathf.Pow(1.3f, _damageUntil.Count)
            * (Has(GamblerAugmentType.RaiseTheStakes) ? 1f + _stacks * 0.1f : 1f)
            * (Jackpot && Has(GamblerAugmentType.OverflowingPower) ? 1.5f : 1f)
            * (Jackpot && Has(GamblerAugmentType.JackpotBoost) ? 1.3f : 1f);

        protected override void Grant(Augment item)
        {
            if (item is GamblerAugment augment) _augments.Add(augment.type);
        }

        protected override WeaponState Initial() => new WeaponState { Ammo = Stats.Capacity, Filled = (uint)Random.Range(1, int.MaxValue) };

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _effects.Open(Player);
        }

        protected override void Advance(ref WeaponState state, uint tick)
        {
            if (state.Reload == 0 || tick < state.Reload) return;
            state.Ammo = Stats.Capacity;
            state.Filled = unchecked(state.Filled * 1664525u + 1013904223u);
            state.Reload = 0;
        }

        protected override bool Plan(ref WeaponState state, CastInput input, out BoltSpec bolt)
        {
            bolt = default;
            if (input.Kind != CastKind.Press) return input.Kind != CastKind.Parry;
            if (input.Tick < state.Ready || state.Ammo <= 0) return false;
            FighterStats stats = Stats;
            bool roulette = state.Ammo == 1 + state.Filled % (uint)stats.Capacity || Has(GamblerAugmentType.MoreChances);
            state.Ammo--;
            state.Ready = input.Tick + Cooldown(stats.AttackInterval);
            state.Reload = input.Tick + Cooldown(stats.Reload);
            bolt = new BoltSpec { Style = roulette ? 2 : 1, Speed = stats.Flight.Speed, Damage = stats.Damage,
                Life = stats.Flight.Life, Radius = 0.14f, Scale = stats.Flight.Scale, Aspect = stats.Flight.Aspect,
                Spin = stats.Flight.Spin, Gravity = stats.Flight.Gravity, GravityDelay = stats.GravityDelay, ExtraGravity = stats.ExtraGravity };
            return true;
        }

        protected override void Execute(CastInput input, Vector2 origin, double lag, BoltSpec bolt)
        {
            if (bolt.Speed <= 0f) return;
            Player.Cast.SpawnBolt(this, input.Action, origin, input.Direction, bolt, lag);
            if (bolt.Style == 2) Roll();
        }

        void Roll()
        {
            if (!IsServer || !Active || !Player.CanAct) return;
            WeaponState state = State;
            bool available = !Jackpot && !_jackpotPending;
            bool guaranteed = available && Has(GamblerAugmentType.GuaranteedJackpot) && state.Progress >= 20;
            float luck = Has(GamblerAugmentType.Luck) ? 0.5f : 0f;
            float common = (Has(GamblerAugmentType.MoreChances) ? 5f : 7f) + luck;
            JackpotResultType main = guaranteed ? JackpotResultType.Jackpot777 : RollTable(common, 1f + luck, available ? Probability : 0f);
            JackpotResultType old = Has(GamblerAugmentType.OldCoin) ? RollTable(1f + luck, 0f, available ? 1f + luck : 0f) : JackpotResultType.None;
            if (guaranteed) state.Progress = 0;
            else if (Has(GamblerAugmentType.GuaranteedJackpot)) state.Progress = Mathf.Min(20, state.Progress
                + (main != JackpotResultType.None ? 1 : 0) + (old != JackpotResultType.None ? 1 : 0));
            State = state;
            if (main == JackpotResultType.Jackpot777 || old == JackpotResultType.Jackpot777)
            {
                _jackpotPending = true;
                if (Has(GamblerAugmentType.ProbabilityShift)) _probability += 1f;
            }
            _rolls.Enqueue(new RollResult { Main = main, Old = old });
            if (!_spinning) StartRoll();
        }

        void StartRoll()
        {
            if (_rolls.Count == 0) return;
            _rolling = _rolls.Dequeue();
            _spinning = true;
            _resolved = false;
            _resolveAt = Time.time + _effects.SpinDuration;
            _nextRollAt = Time.time + _effects.SpinInterval;
            SpinRpc(_rolling.Main, _rolling.Old, ++_rollId, NetGame.Current.ServerTime);
        }

        void CompleteRoll()
        {
            _resolved = true;
            JackpotResultType main = _rolling.Main;
            JackpotResultType old = _rolling.Old;
            _stacks = main != JackpotResultType.None || old != JackpotResultType.None ? Mathf.Min(3, _stacks + 1) : 0;
            ApplyResult(main);
            if (old != main || old == JackpotResultType.DamageUp || old == JackpotResultType.Heal) ApplyResult(old);
            if (main == JackpotResultType.Jackpot777 || old == JackpotResultType.Jackpot777) _jackpotPending = false;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void SpinRpc(JackpotResultType main, JackpotResultType old, uint roll, double started)
        {
            _effects.Spin(main, old, roll, started);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void ResultRpc(JackpotResultType result, double until)
        {
            _effects.Play(result, until, NetGame.Current.ServerTime);
        }

        void Show(JackpotResultType result, float duration) => ResultRpc(result, NetGame.Current.ServerTime + duration);

        static JackpotResultType RollTable(float common, float lethal, float jackpot)
        {
            float roll = Random.Range(0f, 100f);
            var choices = new[] { JackpotResultType.DamageUp, JackpotResultType.Heal, JackpotResultType.Invincible, JackpotResultType.SpeedUp };
            foreach (JackpotResultType choice in choices)
            {
                if (roll < common) return choice;
                roll -= common;
            }
            if (roll < lethal) return JackpotResultType.InstantKill;
            roll -= lethal;
            return roll < jackpot ? JackpotResultType.Jackpot777 : JackpotResultType.None;
        }

        void ApplyResult(JackpotResultType result)
        {
            switch (result)
            {
                case JackpotResultType.DamageUp:
                    _damageUntil.Add(Time.time + 10f);
                    Show(result, 10f);
                    break;
                case JackpotResultType.Heal:
                    Player.Health.Heal(50f);
                    _stealUntil = Time.time + 10f;
                    Show(result, 0.8f);
                    break;
                case JackpotResultType.Invincible:
                    Player.Effects.Immune(5f);
                    Show(result, 5f);
                    break;
                case JackpotResultType.SpeedUp:
                    Motion.ApplySpeed(0.523f, 7.4f);
                    _slowUntil = Time.time + 7.4f;
                    Show(result, 7.4f);
                    break;
                case JackpotResultType.InstantKill:
                    Show(result, 0.78f);
                    CombatDamage.Deal(this, Player.Health, 4444f, DamageTag.JobSkill | DamageTag.IgnoreDefense);
                    break;
                case JackpotResultType.Jackpot777:
                    if (Jackpot) break;
                    _jackpotUntil = Time.time + 15f;
                    _healAt = Time.time + 0.2f;
                    Player.Buffs.MaxScale = 2f;
                    float speed = (Has(GamblerAugmentType.Excited) ? 1.5f : 1f) * (Has(GamblerAugmentType.JackpotBoost) ? 1.2f : 1f);
                    if (speed > 1f) Motion.ApplySpeed(speed - 1f, 15f);
                    Show(result, 15f);
                    break;
            }
        }

        protected override void ServerTick()
        {
            if (_spinning && !_resolved && Time.time >= _resolveAt) CompleteRoll();
            if (_spinning && Time.time >= _nextRollAt)
            {
                _spinning = false;
                if (Player.CanAct) StartRoll();
            }
            _damageUntil.RemoveAll(time => time <= Time.time);
            if (Jackpot && Time.time >= _healAt)
            {
                _healAt = Time.time + 0.2f;
                Player.Health.Heal(30f);
            }
            if (!Jackpot && _jackpotUntil > 0f)
            {
                _jackpotUntil = 0f;
                Player.Buffs.MaxScale = 1f;
            }
        }

        void LateUpdate()
        {
            if (!Active) return;
            if (IsServer && !Player.CanAct) ClearRolls();
            _effects.Tick(NetGame.Current.ServerTime, Player.CanAct);
        }

        void ClearRolls()
        {
            _rolls.Clear();
            _spinning = false;
            _resolved = false;
            _jackpotPending = false;
        }

        public override void OnNetworkDespawn()
        {
            ClearRolls();
            _effects.Close();
            base.OnNetworkDespawn();
        }

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (Active && IsServer && request.Source == this && request.HasTag(DamageTag.Drain) && result.WasApplied)
                Player.Health.Heal(result.AppliedAmount * 0.2f);
        }

        public override void Hit(NetPlayer target, NetBolt bolt)
        {
            DamageTag tags = DamageTag.BasicAttack | DamageTag.Projectile;
            if (Has(GamblerAugmentType.CoinUpgrade) && Time.time < _stealUntil) tags |= DamageTag.Drain;
            DamageResult result = CombatDamage.Deal(this, target.Health, bolt.Spec.Damage, tags);
            if (!result.WasAccepted || !Has(GamblerAugmentType.CoinUpgrade)) return;
            if (Time.time < _slowUntil) target.Motion.ApplySlow(0.2f, 0.5f);
            if (_damageUntil.Count > 0) StartCoroutine(DamageOverTime(target, 2f, 4, 0.5f));

        }
    }
}
