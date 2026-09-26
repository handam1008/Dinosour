using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public abstract class JobCast : NetworkBehaviour, IBoltReceiver, IOutgoingDamageModifier
    {
        [SerializeField] protected NetPlayer Player;
        [SerializeField] AugmentDrafter _source;
        [SerializeField] WeaponView _view;
        readonly NetworkVariable<WeaponState> _state = new NetworkVariable<WeaponState>();
        readonly List<CastInput> _pending = new List<CastInput>();
        protected PlayerController Motion => Player.Motion;
        protected MotionView Drive => Player.Drive;
        protected FighterStats Stats => Player.Stats;
        protected bool Active => IsSpawned && Player.Job == Job;
        protected uint Tick => IsOwner && !IsServer ? Player.CastTick : Player.InputSequence;
        protected WeaponState State { get => _state.Value; set => _state.Value = value; }
        public abstract PlayerJob Job { get; }
        public int Progress
        {
            get => State.Progress;
            set
            {
                if (!IsServer) return;
                WeaponState state = State;
                state.Progress = value;
                ProgressChanged(ref state);
                State = state;
            }
        }
        public WeaponState Status => Active && IsOwner && !IsServer ? Forecast() : State;
        public virtual float DamageScale => 1f;
        public int Priority => 50;
        public float ModifyOutgoingDamage(float amount) => Active ? amount * DamageScale : amount;

        protected virtual void Awake() => _source.AugmentGranted += Grant;

        public override void OnNetworkSpawn()
        {
            if (IsServer && Player.Job == Job) State = Initial();
            foreach (Augment item in _source.Owned) Grant(item);
        }

        protected virtual WeaponState Initial() => default;
        protected virtual void ProgressChanged(ref WeaponState state) { }
        protected abstract void Grant(Augment item);
        protected abstract bool Plan(ref WeaponState state, CastInput input, out BoltSpec bolt);
        protected abstract void Execute(CastInput input, Vector2 origin, double lag, BoltSpec bolt);
        protected virtual void Advance(ref WeaponState state, uint tick) { }
        protected virtual void ServerTick() { }
        protected virtual Vector2 BoltOrigin(Vector2 center, Vector2 direction, float radius) => center;

        protected static uint Ticks(float seconds) => (uint)Mathf.CeilToInt(Mathf.Max(0.001f, seconds) / Time.fixedDeltaTime);
        protected uint Cooldown(float seconds) => Ticks(seconds * Player.Cast.CooldownScale);

        WeaponState Forecast()
        {
            WeaponState state = State;
            _pending.RemoveAll(x => x.Action <= state.Action || x.Epoch != Player.Epoch);
            foreach (CastInput input in _pending)
            {
                Advance(ref state, input.Tick);
                Plan(ref state, input, out _);
            }
            Advance(ref state, Tick);
            return state;
        }

        public void Predict(CastInput input)
        {
            if (!Active) return;
            WeaponState state = Forecast();
            if (!Plan(ref state, input, out BoltSpec bolt)) return;
            if (_pending.Count >= 64) return;
            _pending.Add(input);
            PredictAction(input);
            if (bolt.Speed > 0f) Player.Cast.PreviewBolt(input.Action, BoltOrigin(Player.ViewPosition, input.Direction.normalized, bolt.Radius), input.Direction.normalized, bolt);
            Present(input.Kind, input.Direction);
            Player.Cast.PredictSound(input.Action, input.Kind);
            Player.Cast.Presented();
        }

        public virtual void Reject(uint action) => _pending.RemoveAll(input => input.Action == action);

        public virtual void Prepare(ref CastInput input) { }

        protected virtual void PredictAction(CastInput input) { }

        public bool Apply(CastInput input, Vector2 origin, double lag)
        {
            if (!IsServer || !Active) return false;
            WeaponState state = State;
            Advance(ref state, input.Tick);
            bool accepted = Plan(ref state, input, out BoltSpec bolt);
            state.Action = input.Action;
            State = state;
            if (!accepted) return false;
            Execute(input, origin, lag, bolt);
            Player.Cast.ShareSound(input.Action, input.Kind);
            if (input.Kind != CastKind.Release && input.Kind != CastKind.StopCycle && input.Kind != CastKind.Cancel)
                PresentRpc(input.Kind, input.Direction);
            return true;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void PresentRpc(CastKind kind, Vector2 direction)
        {
            if (IsOwner && !IsServer) return;
            Present(kind, direction);
        }

        protected virtual void Present(CastKind kind, Vector2 direction) => _view.Play(kind, direction);

        protected virtual void Update()
        {
            if (!Active || !Player.CanAct) return;
            if (IsServer)
            {
                WeaponState state = State;
                Advance(ref state, Tick);
                State = state;
                ServerTick();
            }
        }

        public virtual void Hit(NetPlayer target, NetBolt bolt)
        {
            CombatDamage.Deal(this, target.Health, bolt.Spec.Damage, DamageTag.Projectile | DamageTag.BasicAttack);
        }

        protected void Melee(Vector2 origin, Vector2 direction, double lag, Vector2 size, Vector2 offset, System.Action<NetPlayer> hit)
        {
            Vector2 axis = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            Vector3 scale = Player.transform.lossyScale;
            Vector2 extent = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            offset = Vector2.Scale(offset, extent);
            Vector2 center = origin + axis * offset.x + new Vector2(-axis.y, axis.x) * offset.y;
            size = Vector2.Scale(size, extent);
            double time = NetGame.Current.PhysicsTime - lag;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == Player || !target.CanAct) continue;
                if (!target.BoxHit(center, axis, size, time, out Vector2 point)) continue;
                if (ShotQuery.GroundRay(origin, point, Player.GroundMask, out _)) continue;
                hit(target);
            }
        }

        protected void Melee(Vector2 origin, Vector2 direction, double lag, float reach, float radius, System.Action<NetPlayer> hit)
        {
            /*Vector2 end = origin + direction.normalized * reach;
            if (ShotQuery.Ground(origin, end, radius, Player.GroundMask, out RaycastHit2D wall)) end = wall.centroid;
            double time = NetGame.Current.PhysicsTime - lag;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == Player || !target.CanAct) continue;
                if (target.SweepHit(origin, end, time, time, radius, out _)) hit(target);
            }*/
            
            //판정 좀더 좋아진 코드 / NKY수정 (위는 기존 코드)
            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            Vector2 end = origin + dir * reach;
            double time = NetGame.Current.PhysicsTime - lag;
            
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == Player || !target.CanAct) continue;
                if (!target.SweepHit(origin, end, time, time, radius, out float fraction)) continue;
                
                Vector2 hitPoint = Vector2.Lerp(origin, end, fraction);
                
                if (ShotQuery.GroundRay(origin, hitPoint, Player.GroundMask, out _)) continue;

                hit(target);
            }
        }

        protected IEnumerator DamageOverTime(NetPlayer target, float damage, int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(interval);
                if (!Active || !Player.CanAct || target == null || !target.CanAct) yield break;
                CombatDamage.Deal(this, target.Health, damage, DamageTag.JobSkill | DamageTag.DamageOverTime);
            }
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();
            _pending.Clear();
        }

        public override void OnDestroy()
        {
            _source.AugmentGranted -= Grant;
            base.OnDestroy();
        }
    }
}
