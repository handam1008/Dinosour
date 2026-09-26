using System.Collections.Generic;
using NKY.Scripts;
using NKY.Scripts.Job;
using UnityEngine;

namespace SSW
{
    public sealed class KnifeCast : JobCast, IDamageReceivedListener
    {
        [SerializeField] float _damage = 15f;
        [SerializeField] float _reach = 1.6f;
        [SerializeField] float _interval = 0.5f;
        [SerializeField] float _skillCooldown = 3f;
        [SerializeField] float _throwDamage = 15f;
        [SerializeField] float _throwSpeed = 25f;
        readonly Dictionary<AssassinAugmentType, AbstractAssassinAugmentSO> _augments = new Dictionary<AssassinAugmentType, AbstractAssassinAugmentSO>();
        AugmentContext _context;
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
            if (item is AbstractAssassinAugmentSO augment) _augments.TryAdd(augment.type, augment);
        }

        protected override bool Plan(ref WeaponState state, CastInput input, out BoltSpec bolt)
        {
            bolt = default;
            if (input.Kind == CastKind.Press)
            {
                if (input.Tick < state.Ready) return false;
                state.Ready = input.Tick + Cooldown(_interval);
                return true;
            }
            if (input.Kind == CastKind.Cycle)
            {
                bool recall = state.Ammo > 0;
                if (!recall && input.Tick < state.Skill) return false;
                if (recall) { state.Ammo = 0; return true; }
                state.Ammo = 1;
                state.Skill = input.Tick + Cooldown(_skillCooldown);
                bolt = new BoltSpec { Style = 3, Speed = _throwSpeed, Damage = _throwDamage, Life = 5f,
                    Radius = 0.12f, Scale = 0.65f, Stick = true, Bounce = Has(AssassinAugmentType.ConcealedWeapon) ? 1 : 0,
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
                Melee(origin, input.Direction, lag, _reach, 0.45f, Strike);
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
                _auraUntil = Time.time + 6f;
                _auraReady = Time.time + 12f;
                _auraStart = Time.time;
            }
        }

        void Strike(NetPlayer target)
        {
            var info = new DamageInfo { Damage = _damage, Target = target.gameObject, IsBasicAttack = true, DamageTag = DamageTag.BasicAttack };
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
            if (_augments.TryGetValue(AssassinAugmentType.Escape, out AbstractAssassinAugmentSO escape)) escape.OnHit(_context);
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
            float amount = Mathf.Clamp01((Time.time - _auraStart) / 3f) * 0.3f;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == Player || Vector2.Distance(Player.Body.position, target.Body.position) > 3f) continue;
                target.Motion.ApplySlow(amount, 0.2f);
                target.Motion.ApplyAttackWeaken(amount, 0.2f);
            }
        }
    }
}
