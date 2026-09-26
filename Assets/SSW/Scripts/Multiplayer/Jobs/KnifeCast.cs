using System;
using System.Collections.Generic;
using NKY.Scripts;
using NKY.Scripts.Job;
using UnityEngine;

namespace SSW
{
    public sealed class KnifeCast : JobCast, IDamageReceivedListener
    {
        readonly Dictionary<AssassinAugmentType, AbstractAssassinAugmentSO> _augments = new Dictionary<AssassinAugmentType, AbstractAssassinAugmentSO>();
        AugmentContext _context;
        KnifeTuning.Escape _escape;
        KnifeTuning.Aura _aura;
        NetBolt _knife;
        uint _throwAction;
        uint _recallAction;
        float _hideAt;
        float _blindAt;
        float _auraUntil;
        float _auraAt;
        float _auraStart;
        float _auraReady;
        bool _shifted;
        float _shiftUntil;
        public override PlayerJob Job => PlayerJob.Assassin;
        public bool Has(AssassinAugmentType type) => _augments.ContainsKey(type);
        public override float DamageScale => Time.time < _shiftUntil ? 1.5f : 1f;

        protected override void Awake()
        {
            base.Awake();
            _context = new AugmentContext(Player.gameObject, this);
        }

        protected override void Grant(Augment item)
        {
            if (item is not AbstractAssassinAugmentSO augment || !_augments.TryAdd(augment.type, augment)) return;
            if (augment.type == AssassinAugmentType.Escape) _escape = KnifeTuning.Read<KnifeTuning.Escape>(augment);
            if (augment.type == AssassinAugmentType.KillingIntent) _aura = KnifeTuning.Read<KnifeTuning.Aura>(augment);
        }

        protected override bool Plan(ref WeaponState state, CastInput input, out BoltSpec bolt)
        {
            FighterStats stats = Stats;
            bolt = default;
            if (input.Kind == CastKind.Press)
            {
                if (input.Tick < state.Ready) return false;
                state.Ready = input.Tick + Cooldown(stats.AttackInterval);
                return true;
            }
            if (input.Kind == CastKind.Cycle)
            {
                bool recall = state.Ammo > 0;
                if (!recall && input.Tick < state.Skill) return false;
                if (recall) { state.Ammo = 0; return true; }
                state.Ammo = 1;
                state.Skill = input.Tick + Cooldown(stats.SkillCooldown);
                bolt = new BoltSpec { Style = 3, Speed = stats.Flight.Speed, Damage = stats.SkillDamage, Life = stats.Flight.Life,
                    Gravity = stats.Flight.Gravity, Spin = stats.Flight.Spin, Radius = 0.12f, Scale = stats.Flight.Scale,
                    Aspect = stats.Flight.Aspect, Stick = true, Bounce = Has(AssassinAugmentType.ConcealedWeapon) ? 1 : 0,
                    CanPenetrate = true};
                return true;
            }
            return input.Kind == CastKind.Release || input.Kind == CastKind.StopCycle || input.Kind == CastKind.Cancel;
        }

        internal void Track(NetBolt knife) => _knife = knife;

        protected override void PredictAction(CastInput input)
        {
            if (input.Kind != CastKind.Cycle) return;
            if (Status.Ammo > 0) _throwAction = input.Action;
            else _recallAction = input.Action;
        }

        public override void Prepare(ref CastInput input)
        {
            if (IsServer || input.Action != _recallAction || input.Kind != CastKind.Cycle) return;
            bool live = _knife != null && _knife.IsSpawned && _knife.Action == _throwAction && _knife.Owner == Player && _knife.Caster == Player.NetworkObjectId;
            Vector2 point;
            if (live) point = _knife.transform.position;
            else if (!Player.Cast.PreviewPoint(_throwAction, out point)) return;
            input.Recall = _throwAction;
            input.Point = point;
            Player.Cast.HoldPreview(_throwAction, true);
            if (live) _knife.Hold(true);
        }

        public override void Reject(uint action)
        {
            base.Reject(action);
            if (action != _recallAction) return;
            Player.Cast.HoldPreview(_throwAction, false);
            if (_knife != null) _knife.Hold(false);
        }
        
        protected override void Execute(CastInput input, Vector2 origin, double lag, BoltSpec bolt)
        {
            if (input.Kind == CastKind.Press)
            {
                FighterStats stats = Stats;
                Melee(origin, input.Direction, lag, stats.HitSize, stats.HitOffset, Strike, stats.Damage);
                return;
            }
            if (input.Kind != CastKind.Cycle) return;
            if (_knife != null && _knife.IsSpawned)
            {
                if (_knife.Owner != Player || _knife.Caster != Player.NetworkObjectId)
                {
                    _knife = null;
                    return;
                }
                Vector2 velocity = _knife.Velocity.normalized;
                Vector2 point = _knife.Position;
                Vector2 normal = _knife.Normal;
                if (input.Recall == _knife.Action) _knife.Recall(input.Point, Player.Cast.RecallWindow, out point, out normal);
                if (Drive.Blink(point, normal, _knife.Spec.Radius))
                    Drive.ApplyForce(velocity * 10f, ForceMode2D.Impulse);
                _knife.FinishAt(point);
                _knife = null;
            }
            else if (bolt.Speed > 0f) _knife = Player.Cast.SpawnBolt(this, input.Action, origin, input.Direction, bolt, lag);
            if (_augments.TryGetValue(AssassinAugmentType.FinishingBlow, out AbstractAssassinAugmentSO finishing)) finishing.OnSkillUsed(_context);
            if (Has(AssassinAugmentType.Blackout) && Time.time >= _blindAt)
            {
                _blindAt = Time.time + 17.5f;
                foreach (NetPlayer target in NetGame.Current.Players)
                    if (target != Player) target.Effects.Blind(2.5f, 1);
            }
            if (Has(AssassinAugmentType.KillingIntent) && Time.time >= _auraReady)
            {
                _auraUntil = Time.time + _aura.Duration;
                _auraReady = _auraUntil + _aura.Cooldown;
                _auraStart = Time.time;
            }
        }

        void Strike(NetPlayer target)
        {
            var info = new DamageInfo { Damage = Stats.Damage, Target = target.gameObject, IsBasicAttack = true, DamageTag = DamageTag.BasicAttack };
            foreach (var pair in _augments)
            {
                if (pair.Key == AssassinAugmentType.Ambush || pair.Key == AssassinAugmentType.TacticalShift || pair.Key == AssassinAugmentType.KillingIntent) continue;
                pair.Value.OnBasicAttackHit(_context, target.Motion);
                info.Damage = pair.Value.ModifyDamage(_context, info);
            }
            DamageResult result = CombatDamage.Deal(this, target.Health, info.Damage, info.DamageTag | DamageTag.BasicAttack);
            Player.Cast.ImpactSound();
            if (result.WasAccepted && Has(AssassinAugmentType.Ambush) && Time.time >= _hideAt)
            {
                _hideAt = Time.time + 5f;
                Player.Effects.Hide(0.8f);
            }
        }

        public override void Hit(NetPlayer target, NetBolt bolt) => CombatDamage.Deal(this, target.Health, bolt.Spec.Damage, DamageTag.Projectile | DamageTag.JobSkill);

        public void OnDamageReceived(DamageRequest request, DamageResult result)
        {
            if (!Active || !IsServer || !result.WasApplied) return;
            if (Has(AssassinAugmentType.Escape) && !_context.IsOnCooldown(AssassinAugmentType.Escape))
            {
                Motion.ApplySpeed(_escape.Speed, _escape.Duration);
                _context.SetCooldown(AssassinAugmentType.Escape, _escape.Duration + _escape.Cooldown);
            }
            if (!_shifted && Player.Health.Current > 0f && Player.Health.Current <= Player.Health.Max * 0.3f && Has(AssassinAugmentType.TacticalShift))
            {
                _shifted = true;
                _shiftUntil = Time.time + 10f;
                Motion.ApplySlow(0.7f, 10f);
                foreach (NetPlayer target in NetGame.Current.Players)
                    if (target != Player && Vector2.Distance(Player.Body.position, target.Body.position) <= 4f) target.Motion.ApplySlow(0.5f, 7f);
            }
        }

        protected override void ServerTick()
        {
            if (State.Ammo > 0 && (_knife == null || !_knife.IsSpawned))
            {
                WeaponState state = State;
                state.Ammo = 0;
                State = state;
            }
            if (Time.time >= _auraUntil || Time.time < _auraAt) return;
            _auraAt = Time.time + 0.1f;
            float elapsed = Time.time - _auraStart;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == Player || Vector2.Distance(Player.Body.position, target.Body.position) > _aura.Radius) continue;
                target.Motion.ApplySlow(_aura.Slow(elapsed), 0.2f);
                target.Motion.ApplyAttackWeaken(_aura.Weaken(elapsed), 0.2f);
            }
        }
    }
}
