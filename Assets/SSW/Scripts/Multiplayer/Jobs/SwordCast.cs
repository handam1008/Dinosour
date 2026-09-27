using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class SwordCast : JobCast, IIncomingDamageModifier, IDamageReceivedListener
    {
        [SerializeField] SwordTuning _tuning = new SwordTuning();
        readonly NetworkVariable<float> _growth = new NetworkVariable<float>(1f);
        readonly HashSet<SwordPerk> _augments = new HashSet<SwordPerk>();
        readonly HashSet<ulong> _dashHits = new HashSet<ulong>();
        float _dashUntil;
        double _parryUntil;
        Vector2 _dashDirection;
        Coroutine _heal;
        bool _boosted;
        float _dashDamage;
        SwordEffects _effects;
        public override PlayerJob Job => PlayerJob.Swordsman;
        public bool Has(SwordPerk type) => _augments.Contains(type);
        public float ReachScale => _growth.Value;
        public bool Parrying => Active && NetGame.Current.ServerTime < _parryUntil;
        public float ReflectSpeed => Stats.ReflectSpeed;
        int IIncomingDamageModifier.Priority => -100;

        protected override void Awake()
        {
            base.Awake();
            _effects = new SwordEffects(this, Player, _tuning);
        }

        protected override void Grant(Augment item)
        {
            if (item is SwordAugment augment) _augments.Add(augment.type);
            if (item is SwordArgument legacy) _augments.Add(legacy.type == SwordAugmentType.ParingHeal ? SwordPerk.ParryHeal : SwordPerk.DashSpeed);
        }

        protected override bool Plan(ref WeaponState state, CastInput input, out BoltSpec bolt)
        {
            bolt = default;
            switch (input.Kind)
            {
                case CastKind.Press:
                    if (input.Tick < state.Ready) return false;
                    state.Ready = input.Tick + Cooldown(Stats.AttackInterval);
                    return true;
                case CastKind.Cycle:
                    if (input.Tick < state.Skill) return false;
                    state.Skill = input.Tick + Cooldown(Stats.DashCooldown);
                    return true;
                case CastKind.Parry:
                    if (input.Tick < state.Parry) return false;
                    state.Parry = input.Tick + Cooldown(Stats.ParryCooldown);
                    return true;
                default:
                    return true;
            }
        }

        protected override void PredictAction(CastInput input)
        {
            FighterStats stats = Stats;
            if (input.Kind == CastKind.Cycle)
                Drive.PredictDash(input.Action, input.Tick, Mathf.Sign(input.Direction.x) * stats.DashSpeed,
                    stats.DashTime * (Has(SwordPerk.DashRange) ? 1.5f : 1f));
        }

        protected override void Execute(CastInput input, Vector2 origin, double lag, BoltSpec bolt)
        {
            FighterStats stats = Stats;
            if (input.Kind == CastKind.Press)
                StartCoroutine(Attack(origin, input.Direction, lag, stats));
            else if (input.Kind == CastKind.Cycle)
            {
                float duration = stats.DashTime * (Has(SwordPerk.DashRange) ? 1.5f : 1f);
                _dashDirection = new Vector2(Mathf.Sign(input.Direction.x), 0f);
                _dashHits.Clear();
                _dashDamage = stats.DashDamage * (_boosted ? 2f : 1f) * _effects.DashScale;
                _boosted = false;
                _dashUntil = Time.time + duration;
                Drive.Dash(input.Action, _dashDirection.x * stats.DashSpeed, duration);
                if (Has(SwordPerk.DashSpeed)) Motion.ApplySpeed(0.5f, duration + 3f);
            }
            else if (input.Kind == CastKind.Parry) _parryUntil = NetGame.Current.ServerTime + stats.ParryTime;
        }

        IEnumerator Attack(Vector2 origin, Vector2 direction, double lag, FighterStats stats)
        {
            var hits = new HashSet<ulong>();
            System.Action<NetPlayer> strike = target =>
            {
                if (!hits.Add(target.NetworkObjectId)) return;
                CombatDamage.Deal(this, target.Health, stats.Damage, DamageTag.BasicAttack);
                Player.Cast.ImpactSound();
            };
            Melee(origin, direction, lag, stats.HitSize * ReachScale, stats.HitOffset * ReachScale, strike, stats.Damage);
            float until = Time.time + stats.AttackTime;
            while (Time.time < until)
            {
                yield return null;
                if (!Active || !Player.CanAct || Time.time >= until) yield break;
                Melee(Player.Body.position, direction, 0d, stats.HitSize * ReachScale, stats.HitOffset * ReachScale, strike, stats.Damage);
            }
        }

        protected override void ServerTick()
        {
            _effects.Tick(Time.deltaTime);
            _growth.Value = _effects.Growth;
            if (Time.time >= _dashUntil) return;
            FighterStats stats = Stats;
            Melee(Player.Body.position, _dashDirection, 0d, stats.DashSpeed * Time.deltaTime + stats.DashRadius, stats.DashRadius, target =>
            {
                if (!_dashHits.Add(target.NetworkObjectId)) return;
                DamageResult result = CombatDamage.Deal(this, target.Health, _dashDamage, DamageTag.JobSkill);
                if (result.WasAccepted) _effects.DashHit(target, _dashUntil, result.AppliedAmount);
                Player.Cast.ImpactSound();
            });
        }

        public float ModifyIncomingDamage(DamageRequest request, float amount) =>
            Parrying && !request.HasTag(DamageTag.IgnoreDefense) && !request.HasTag(DamageTag.DamageOverTime) ? 0f : amount;

        public void OnDamageReceived(DamageRequest request, DamageResult result)
        {
            if (!IsServer || !Parrying || !result.WasBlocked) return;
            ParrySuccess();
        }

        public bool Deflect()
        {
            if (!IsServer || !Parrying) return false;
            ParrySuccess();
            return true;
        }

        void ParrySuccess()
        {
            if (Has(SwordPerk.ParryHeal)) Recover();
            if (Has(SwordPerk.DashPower)) _boosted = true;
            if (Has(SwordPerk.ParryCooldown))
            {
                WeaponState state = State;
                uint remaining = state.Skill > Tick ? state.Skill - Tick : 0;
                uint reduction = Ticks(_tuning.ParryReduction);
                state.Skill = Tick + (remaining > reduction ? remaining - reduction : 0);
                State = state;
            }
        }

        protected override void Update()
        {
            if (IsServer && !Player.CanAct) _effects.Rest();
            base.Update();
        }

        void Recover()
        {
            if (_heal != null) StopCoroutine(_heal);
            _heal = StartCoroutine(Heal());
        }

        IEnumerator Heal()
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.2f);
                if (!Player.CanAct) yield break;
                Player.Health.Heal(2f);
            }
            _heal = null;
        }
    }
}
