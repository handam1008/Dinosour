using System.Collections;
using System.Collections.Generic;
using KDH.Scripts.Arguments;
using UnityEngine;

namespace SSW
{
    public sealed class GunCast : JobCast
    {
        sealed class Poison
        {
            public NetPlayer Target;
            public float Damage;
            public int Remaining;
            public float Next;
            public float Interval;
        }

        [SerializeField] int _questHits = 2;
        [SerializeField] int _poisonTicks = 8;
        [SerializeField] float _shrinkCooldown = 5f;
        [SerializeField] GunBalance _balance;
        [SerializeField] GunView _gunView;
        [SerializeField] GunFx _effects;
        readonly HashSet<GunnerAugmentType> _augments = new HashSet<GunnerAugmentType>();
        readonly List<Poison> _poison = new List<Poison>();
        float _hasteAt;
        float _shrinkAt;
        float _lightningAt;
        float _markUntil;
        int _marks;
        NetPlayer _marked;
        public override PlayerJob Job => PlayerJob.Gunner;
        public GunFx Effects => _effects;
        public GunView View => _gunView;
        public int Capacity => Stats.Capacity;
        public int QuestHits => _questHits;
        public uint ViewTick => Tick;
        public bool Has(GunnerAugmentType type) => _augments.Contains(type);
        public float Charge(uint loaded, uint tick) => tick >= loaded ? Mathf.Clamp01((tick - loaded) * Time.fixedDeltaTime / Stats.ChargeTime) : 0f;

        protected override void Grant(Augment item)
        {
            if (item is not GunnerArgument augment || !_augments.Add(augment.type)) return;
            if (augment.type == GunnerAugmentType.BeautifulFootStepAbility) _hasteAt = Time.time + 3f;
            if (augment.type == GunnerAugmentType.ShrinkingDeviceAbility) _shrinkAt = Time.time + _shrinkCooldown;
        }

        protected override WeaponState Initial() => new WeaponState { Filled = Tick, Reload = Tick + Cooldown(Stats.Reload) };

        protected override void ProgressChanged(ref WeaponState state)
        {
            if (state.Progress >= _questHits) state.Reload = System.Math.Min(state.Reload, state.Filled + Cooldown(Stats.Reload * 0.5f));
        }

        protected override void Advance(ref WeaponState state, uint tick)
        {
            FighterStats stats = Stats;
            if (state.Ammo == stats.Capacity) return;
            uint duration = Cooldown(stats.Reload * (state.Progress >= _questHits ? 0.5f : 1f));
            if (state.Reload == 0) state.Reload = tick + duration;
            while (state.Ammo < stats.Capacity && tick >= state.Reload)
            {
                state.Loaded.Add(state.Reload);
                state.Filled = state.Reload;
                state.Ammo++;
                state.Reload += duration;
            }
        }

        protected override bool Plan(ref WeaponState state, CastInput input, out BoltSpec bolt)
        {
            bolt = default;
            if (input.Kind != CastKind.Press) return input.Kind != CastKind.Parry;
            if (input.Tick < state.Ready || state.Ammo == 0) return false;
            FighterStats stats = Stats;
            bool charged = input.Tick >= state.Loaded[state.Loaded.Length - 1] + Ticks(stats.ChargeTime);
            if (state.Ammo == stats.Capacity && input.Tick > state.Reload) state.Reload = input.Tick;
            state.Loaded.RemoveAt(state.Loaded.Length - 1);
            state.Ammo--;
            if (state.Reload == 0) state.Reload = input.Tick + Cooldown(stats.Reload * (state.Progress >= _questHits ? 0.5f : 1f));
            state.Ready = input.Tick + Cooldown(stats.AttackInterval);
            bolt = new BoltSpec { Style = 0, Speed = stats.Flight.Speed, Gravity = stats.Flight.Gravity,
                Damage = stats.Damage * (charged ? stats.ChargeDamage : 1f), Life = stats.Flight.Life,
                Radius = 0.125f, Scale = stats.Flight.Scale, Aspect = stats.Flight.Aspect, Spin = stats.Flight.Spin, Charged = charged };
            return true;
        }

        protected override Vector2 BoltOrigin(Vector2 center, Vector2 direction, float radius)
        {
            Vector2 end = _gunView.Muzzle(center, direction, Drive.Scale);
            return ShotQuery.Ground(center, end, radius, Player.GroundMask, out RaycastHit2D hit) ? hit.centroid : end;
        }

        protected override void Execute(CastInput input, Vector2 origin, double lag, BoltSpec bolt)
        {
            if (bolt.Speed > 0f) Player.Cast.SpawnBolt(this, input.Action, BoltOrigin(origin, input.Direction, bolt.Radius), input.Direction, bolt, lag);
        }

        public override void Hit(NetPlayer target, NetBolt bolt)
        {
            DamageResult hit = CombatDamage.Deal(this, target.Health, bolt.Spec.Damage, DamageTag.BasicAttack | DamageTag.Projectile);
            if (!hit.WasAccepted) return;
            float scale = bolt.Spec.Charged ? Stats.ChargeDamage : 1f;
            int mask = 0;
            foreach (GunnerAugmentType type in _augments) mask |= 1 << (int)type;
            if (Has(GunnerAugmentType.AirBullet) && !target.Drive.Grounded) target.Drive.Launch(40f * scale);
            if (Has(GunnerAugmentType.GravityBullet)) target.Drive.ApplyForce((target.Body.position - Player.Body.position).normalized * (_balance.GravityForce * scale), ForceMode2D.Impulse);
            if (Has(GunnerAugmentType.IceBullet)) target.Motion.ApplySlow(_balance.IceSlow * scale, 1.5f * scale);
            if (Has(GunnerAugmentType.FireBullet)) StartCoroutine(DamageOverTime(target, _balance.FireDamage * scale, 6, 0.5f));
            if (Has(GunnerAugmentType.PoisonBullet)) ApplyPoison(target, _balance.PoisonDamage * scale);
            if (Has(GunnerAugmentType.ShurikenBullet)) CombatDamage.Deal(this, target.Health, Vector2.Distance(Player.Body.position, target.Body.position) * _balance.ShurikenDamage * scale, DamageTag.JobSkill);
            GunProc proc = GunProc.None;
            if (Has(GunnerAugmentType.BeautifulFootStepAbility) && Time.time >= _hasteAt)
            {
                _hasteAt = Time.time + 3f;
                Motion.ApplySpeed(0.5f, 1f);
                proc |= GunProc.Haste;
            }
            if (Has(GunnerAugmentType.ShrinkingDeviceAbility) && Time.time >= _shrinkAt)
            {
                _shrinkAt = Time.time + _shrinkCooldown;
                Player.Effects.Shrink(2f);
                proc |= GunProc.Shrink;
            }
            if (Has(GunnerAugmentType.Quest_EvolutionAbility) && bolt.Spec.Charged && State.Progress < _questHits)
            {
                WeaponState state = State;
                state.Progress++;
                if (state.Progress == _questHits)
                {
                    proc |= GunProc.Evolve;
                    ProgressChanged(ref state);
                }
                State = state;
            }
            else mask &= ~(1 << (int)GunnerAugmentType.Quest_EvolutionAbility);
            if (Has(GunnerAugmentType.LightningBullet) && Time.time >= _lightningAt)
            {
                if (_marked != target || Time.time >= _markUntil) _marks = 0;
                _marked = target;
                _marks++;
                _markUntil = Time.time + 3f;
                _effects.Mark(target, _marks, _marks >= 3 ? 1f : 3f);
                if (_marks >= 3)
                {
                    _marks = 0;
                    _lightningAt = Time.time + 6f;
                    StartCoroutine(Lightning(target));
                }
            }
            _effects.Hit(target, mask, bolt.Spec.Charged, proc);
        }

        void ApplyPoison(NetPlayer target, float damage)
        {
            foreach (Poison poison in _poison)
            {
                if (poison.Target != target) continue;
                poison.Remaining = Mathf.Max(poison.Remaining, _poisonTicks);
                poison.Damage = damage;
                return;
            }
            float interval = 4f / _poisonTicks;
            _poison.Add(new Poison { Target = target, Damage = damage, Remaining = _poisonTicks, Interval = interval, Next = Time.time + interval });
        }

        protected override void ServerTick()
        {
            for (int i = _poison.Count - 1; i >= 0; i--)
            {
                Poison poison = _poison[i];
                if (poison.Target == null || !poison.Target.CanAct) { _poison.RemoveAt(i); continue; }
                while (poison.Remaining > 0 && Time.time >= poison.Next)
                {
                    poison.Next += poison.Interval;
                    poison.Remaining--;
                    CombatDamage.Deal(this, poison.Target.Health, poison.Damage, DamageTag.JobSkill | DamageTag.DamageOverTime);
                    if (!poison.Target.CanAct) break;
                }
                if (poison.Remaining == 0 || !poison.Target.CanAct) _poison.RemoveAt(i);
            }
        }

        IEnumerator Lightning(NetPlayer target)
        {
            yield return new WaitForSeconds(1f);
            if (!Active || !Player.CanAct || target == null || !target.CanAct) yield break;
            _effects.Strike(target);
            CombatDamage.Deal(this, target.Health, 50f, DamageTag.JobSkill);
        }

        protected override void Update()
        {
            if (IsServer && !Player.CanAct && _poison.Count > 0) _poison.Clear();
            base.Update();
        }

        public override void OnNetworkDespawn()
        {
            _poison.Clear();
            base.OnNetworkDespawn();
        }
    }
}
