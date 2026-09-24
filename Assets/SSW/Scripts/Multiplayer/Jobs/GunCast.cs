using System.Collections.Generic;
using KDH.Scripts.Arguments;
using UnityEngine;

namespace SSW
{
    public sealed class GunCast : JobCast
    {
        [SerializeField] int _capacity = 9;
        [SerializeField] float _reload = 2.5f;
        [SerializeField] float _interval = 0.15f;
        [SerializeField] float _damage = 15f;
        [SerializeField] float _speed = 20f;
        [SerializeField] int _questHits = 5;
        readonly HashSet<GunnerAugmentType> _augments = new HashSet<GunnerAugmentType>();
        float _hasteAt;
        float _shrinkAt;
        float _lightningAt;
        float _markUntil;
        int _marks;
        NetPlayer _marked;
        readonly Dictionary<NetPlayer, Coroutine> _poison = new Dictionary<NetPlayer, Coroutine>();
        public override PlayerJob Job => PlayerJob.Gunner;
        public bool Has(GunnerAugmentType type) => _augments.Contains(type);

        protected override void Grant(Augment item)
        {
            if (item is GunnerArgument augment) _augments.Add(augment.type);
        }

        protected override WeaponState Initial() => new WeaponState { Reload = Tick + Cooldown(_reload) };

        protected override void Advance(ref WeaponState state, uint tick)
        {
            uint duration = Cooldown(_reload * (state.Progress >= _questHits ? 0.5f : 1f));
            if (state.Ammo == _capacity) { state.Reload = 0; return; }
            if (state.Reload == 0) state.Reload = tick + duration;
            while (state.Ammo < _capacity && tick >= state.Reload)
            {
                state.Loaded.Add(state.Reload);
                state.Filled = state.Reload;
                state.Ammo++;
                state.Reload += duration;
            }
            if (state.Ammo == _capacity) state.Reload = 0;
        }

        protected override bool Plan(ref WeaponState state, CastInput input, out BoltSpec bolt)
        {
            bolt = default;
            if (input.Kind != CastKind.Press) return input.Kind != CastKind.Parry;
            if (input.Tick < state.Ready || state.Ammo == 0) return false;
            bool charged = input.Tick >= state.Loaded[state.Loaded.Length - 1] + Ticks(3f);
            state.Loaded.RemoveAt(state.Loaded.Length - 1);
            state.Ammo--;
            if (state.Reload == 0) state.Reload = input.Tick + Cooldown(_reload * (state.Progress >= _questHits ? 0.5f : 1f));
            state.Ready = input.Tick + Cooldown(_interval);
            bolt = new BoltSpec { Style = 0, Speed = _speed, Damage = _damage, Life = 3f, Radius = 0.1f, Scale = 0.7f, Charged = charged };
            return true;
        }

        protected override void Execute(CastInput input, Vector2 origin, double lag, BoltSpec bolt)
        {
            if (bolt.Speed > 0f) Player.Cast.SpawnBolt(this, input.Action, origin, input.Direction, bolt, lag);
        }

        public override void Hit(NetPlayer target, NetBolt bolt)
        {
            DamageResult hit = CombatDamage.Deal(this, target.Health, bolt.Spec.Damage, DamageTag.BasicAttack | DamageTag.Projectile);
            if (!hit.WasApplied) return;
            float scale = bolt.Spec.Charged ? 2f : 1f;
            if (Has(GunnerAugmentType.AirBullet) && !target.Drive.Grounded) target.Drive.ApplyForce(Vector2.up * (40f * scale), ForceMode2D.Impulse);
            if (Has(GunnerAugmentType.GravityBullet)) target.Drive.ApplyForce(bolt.Velocity.normalized * (50f * scale), ForceMode2D.Impulse);
            if (Has(GunnerAugmentType.IceBullet)) target.Motion.ApplySlow(0.2f * scale, 1.5f * scale);
            if (Has(GunnerAugmentType.FireBullet)) StartCoroutine(DamageOverTime(target, 3f * scale, 6, 0.5f));
            if (Has(GunnerAugmentType.PoisonBullet))
            {
                if (_poison.TryGetValue(target, out Coroutine old)) StopCoroutine(old);
                _poison[target] = StartCoroutine(DamageOverTime(target, 7f * scale, 7, 4f / 7f));
            }
            if (Has(GunnerAugmentType.ShurikenBullet)) CombatDamage.Deal(this, target.Health, Vector2.Distance(Player.Body.position, target.Body.position) * 3f * scale, DamageTag.JobSkill);
            if (Has(GunnerAugmentType.BeautifulFootStepAbility) && Time.time >= _hasteAt)
            {
                _hasteAt = Time.time + 3f;
                Motion.ApplySpeed(0.5f, 1f);
            }
            if (Has(GunnerAugmentType.ShrinkingDeviceAbility) && Time.time >= _shrinkAt)
            {
                _shrinkAt = Time.time + 3f;
                Player.Effects.Shrink(2f);
            }
            if (Has(GunnerAugmentType.Quest_EvolutionAbility) && bolt.Spec.Charged)
            {
                WeaponState state = State;
                state.Progress = Mathf.Min(_questHits, state.Progress + 1);
                State = state;
            }
            if (Has(GunnerAugmentType.LightningBullet) && Time.time >= _lightningAt)
            {
                if (_marked != target || Time.time >= _markUntil) _marks = 0;
                _marked = target;
                _marks++;
                _markUntil = Time.time + 3f;
                if (_marks >= 3)
                {
                    _marks = 0;
                    _lightningAt = Time.time + 6f;
                    StartCoroutine(DamageOverTime(target, 50f, 1, 1f));
                }
            }
        }
    }
}
