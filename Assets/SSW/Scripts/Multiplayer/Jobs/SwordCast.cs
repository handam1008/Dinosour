using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class SwordCast : JobCast, IIncomingDamageModifier, IDamageReceivedListener
    {
        [SerializeField] float _damage = 30f;
        [SerializeField] float _reach = 2f;
        [SerializeField] float _interval = 0.3f;
        [SerializeField] float _dashSpeed = 20f;
        [SerializeField] float _dashTime = 0.2f;
        [SerializeField] float _dashCooldown = 1f;
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
                    state.Ready = input.Tick + Cooldown(_interval);
                    return true;
                case CastKind.Cycle:
                    if (input.Tick < state.Skill) return false;
                    state.Skill = input.Tick + Cooldown(_dashCooldown);
                    return true;
                case CastKind.Parry:
                    if (input.Tick < state.Parry) return false;
                    state.Parry = input.Tick + Cooldown(1.2f);
                    return true;
                default:
                    return true;
            }
        }

        protected override void PredictAction(CastInput input)
        {
            if (input.Kind == CastKind.Cycle)
                Drive.PredictDash(input.Action, input.Tick, Mathf.Sign(input.Direction.x) * _dashSpeed,
                    _dashTime * (Has(SwordPerk.DashRange) ? 1.5f : 1f));
        }

        protected override void Execute(CastInput input, Vector2 origin, double lag, BoltSpec bolt)
        {
            if (input.Kind == CastKind.Press)
                Melee(origin, input.Direction, lag, _reach, 0.45f, target =>
                {
                    CombatDamage.Deal(this, target.Health, _damage, DamageTag.BasicAttack);
                    Player.Cast.ImpactSound();
                });
            else if (input.Kind == CastKind.Cycle)
            {
                float duration = _dashTime * (Has(SwordPerk.DashRange) ? 1.5f : 1f);
                _dashDirection = new Vector2(Mathf.Sign(input.Direction.x), 0f);
                _dashHits.Clear();
                _dashDamage = _boosted ? 10f : 5f;
                _boosted = false;
                _dashUntil = Time.time + duration;
                Drive.Dash(input.Action, _dashDirection.x * _dashSpeed, duration);
                if (Has(SwordPerk.DashSpeed)) Motion.ApplySpeed(0.5f, duration + 3f);
            }
            else if (input.Kind == CastKind.Parry) _parryUntil = NetGame.Current.ServerTime + 0.3d;
        }

        protected override void ServerTick()
        {
            if (Time.time >= _dashUntil) return;
            Melee(Player.Body.position, _dashDirection, 0d, _dashSpeed * Time.deltaTime + 0.5f, 0.5f, target =>
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
