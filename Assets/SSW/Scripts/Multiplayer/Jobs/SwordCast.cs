using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class SwordCast : JobCast, IIncomingDamageModifier, IDamageReceivedListener
    {
        readonly HashSet<SwordPerk> _augments = new HashSet<SwordPerk>();
        readonly HashSet<ulong> _dashHits = new HashSet<ulong>();
        float _dashUntil;
        double _parryUntil;
        Vector2 _dashDirection;
        Coroutine _heal;
        bool _boosted;
        float _dashDamage;
        public override PlayerJob Job => PlayerJob.Swordsman;
        public bool Has(SwordPerk type) => _augments.Contains(type);
        public bool Parrying => Active && NetGame.Current.ServerTime < _parryUntil;
        public float ReflectSpeed => Stats.ReflectSpeed;
        int IIncomingDamageModifier.Priority => -100;

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
                _dashDamage = stats.DashDamage * (_boosted ? 2f : 1f);
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
            Melee(origin, direction, lag, stats.HitSize, stats.HitOffset, strike);
            float until = Time.time + stats.AttackTime;
            while (Time.time < until)
            {
                yield return null;
                if (!Active || !Player.CanAct || Time.time >= until) yield break;
                Melee(Player.Body.position, direction, 0d, stats.HitSize, stats.HitOffset, strike);
            }
        }

        protected override void ServerTick()
        {
            if (Time.time >= _dashUntil) return;
            FighterStats stats = Stats;
            Melee(Player.Body.position, _dashDirection, 0d, stats.DashSpeed * Time.deltaTime + stats.DashRadius, stats.DashRadius, target =>
            {
                if (!_dashHits.Add(target.NetworkObjectId)) return;
                CombatDamage.Deal(this, target.Health, _dashDamage, DamageTag.JobSkill);
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
