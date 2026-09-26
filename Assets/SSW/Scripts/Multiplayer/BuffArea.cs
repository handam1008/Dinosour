using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public enum BuffAreaKind : byte
    {
        Mine,
        Bomb
    }

    public struct BuffAreaState
    {
        public int Id;
        public BuffAreaKind Kind;
        public Vector2 Position;
        public Vector2 Velocity;
        public double Time;
        public double ArmAt;
        public double EndAt;
        public bool Settled;
    }

    [DefaultExecutionOrder(200)]
    public sealed class BuffArea : NetworkBehaviour
    {
        sealed class Body
        {
            public BuffAreaState State;
            public Collider2D Ground;
            public Vector2 Anchor;
            public double WarmUntil;
            public readonly HashSet<ulong> Ignored = new HashSet<ulong>();
        }

        struct HealPoint
        {
            public Vector2 Position;
            public double At;
        }

        [SerializeField] NetPlayer _player;
        [SerializeField] BuffFx _fx;
        [SerializeField] float _healRadius = 1.3f;
        [SerializeField] float _healChargeTime = 1f;
        [SerializeField] float _healRatio = 0.2f;
        [SerializeField] Vector2 _mineSize = new Vector2(0.585f, 0.45f);
        [SerializeField] float _mineGravity = 1f;
        [SerializeField] float _mineRadius = 2f;
        [SerializeField] float _mineDamage = 10f;
        [SerializeField] float _mineKnockback = 6f;
        [SerializeField] Vector2 _bombSize = new Vector2(0.585f, 0.45f);
        [SerializeField] float _bombGravity = 1f;
        [SerializeField] float _bombFuse = 0.45f;
        [SerializeField] float _bombArm = 0.3f;
        [SerializeField] float _bombRadius = 1.5f;
        [SerializeField] float _bombDamage = 10f;
        [SerializeField] float _bombKnockback = 6f;
        readonly List<Body> _bodies = new List<Body>();
        readonly List<HealPoint> _heals = new List<HealPoint>();
        readonly RaycastHit2D[] _hits = new RaycastHit2D[32];
        double _publishAt;
        double _previousTime;
        int _nextId;
        const int Limit = 64;

        public int Count => _bodies.Count;
        double Now => NetGame.Current.ServerTime;

        public BuffAreaState Get(int index) => _bodies[index].State;

        public Vector2 Position(int index) => _bodies[index].State.Position;

        public override void OnNetworkSpawn() => _previousTime = NetGame.Current.PhysicsTime;

        public void Heal(Vector2 position)
        {
            if (!IsServer || !IsSpawned || !_player.CanAct || !NetMath.Finite(position)) return;
            _heals.Add(new HealPoint { Position = position, At = Now + _healChargeTime });
            _fx.Play(BuffEffect.HealCharge, position, _healRadius, _healChargeTime);
        }

        public void Mine(Vector2 position) => Add(BuffAreaKind.Mine, position, Vector2.zero);
        public void Bomb(Vector2 position, Vector2 velocity) => Add(BuffAreaKind.Bomb, position, velocity);

        void Add(BuffAreaKind kind, Vector2 position, Vector2 velocity)
        {
            if (!IsServer || !IsSpawned || !_player.CanAct || !NetMath.Finite(position) || !NetMath.Finite(velocity)) return;
            if (_bodies.Count >= Limit) Remove(0);
            double now = Now;
            var body = new Body
            {
                WarmUntil = now + 0.25d,
                State = new BuffAreaState
                {
                    Id = ++_nextId,
                    Kind = kind,
                    Position = position,
                    Velocity = velocity,
                    Time = now,
                    ArmAt = kind == BuffAreaKind.Bomb ? now + _bombArm : now,
                    EndAt = kind == BuffAreaKind.Bomb ? now + _bombFuse : 0d
                }
            };
            if (kind == BuffAreaKind.Mine)
            {
                body.Ignored.Add(_player.NetworkObjectId);
                foreach (NetPlayer target in NetGame.Current.Players)
                    if (Touches(target, position, _mineSize)) body.Ignored.Add(target.NetworkObjectId);
            }
            _bodies.Add(body);
            Publish(body.State);
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned) return;
            double time = NetGame.Current.PhysicsTime;
            if (!_player.CanAct)
            {
                Clear();
                _previousTime = time;
                return;
            }
            double now = Now;
            for (int i = _heals.Count - 1; i >= 0; i--)
            {
                HealPoint field = _heals[i];
                if (now < field.At) continue;
                _heals.RemoveAt(i);
                foreach (NetPlayer target in NetGame.Current.Players)
                    if (target.CanAct && InRange(target, field.Position, _healRadius)) target.Health.Heal(target.Health.Max * _healRatio);
                _fx.Play(BuffEffect.HealBurst, field.Position, _healRadius);
            }
            bool publish = now >= _publishAt;
            if (publish) _publishAt = now + 0.05d;
            for (int i = _bodies.Count - 1; i >= 0; i--)
            {
                Body body = _bodies[i];
                Vector2 previous = body.State.Position;
                bool settled = body.State.Settled;
                Advance(body, Time.fixedDeltaTime);
                BuffAreaState state = body.State;
                if (state.Kind == BuffAreaKind.Bomb && now >= state.EndAt
                    || state.Kind == BuffAreaKind.Mine && Triggered(body, previous, _previousTime, time))
                {
                    Explode(state);
                    Remove(i);
                    continue;
                }
                if (state.Position.y < NetGame.Current.Arena.FallY - 5f)
                {
                    Remove(i);
                    continue;
                }
                bool moving = !state.Settled || previous != state.Position;
                bool armed = state.Kind == BuffAreaKind.Bomb && state.Time < state.ArmAt && now >= state.ArmAt;
                if (settled != state.Settled || armed || publish && (moving || now < body.WarmUntil || now - state.Time >= 1d))
                {
                    state.Time = now;
                    body.State = state;
                    Publish(state);
                }
            }
            _previousTime = time;
        }

        void Advance(Body body, float delta)
        {
            BuffAreaState state = body.State;
            if (state.Settled)
            {
                if (body.Ground != null && body.Ground.enabled && body.Ground.gameObject.activeInHierarchy)
                {
                    state.Position = body.Ground.transform.TransformPoint(body.Anchor);
                    body.State = state;
                    return;
                }
                state.Settled = false;
                body.Ground = null;
            }
            state.Velocity += Physics2D.gravity * (Gravity(state.Kind) * delta);
            Vector2 size = state.Kind == BuffAreaKind.Mine ? _mineSize : _bombSize;
            Vector2 travel = state.Velocity * delta;
            if (Cast(state.Position, size, travel, out RaycastHit2D hit))
            {
                state.Position = hit.centroid + hit.normal * 0.002f;
                if (hit.normal.y >= GroundProbe.MinNormal && state.Velocity.y <= 0f)
                {
                    state.Velocity = Vector2.zero;
                    state.Settled = true;
                    body.Ground = hit.collider;
                    body.Anchor = hit.collider.transform.InverseTransformPoint(state.Position);
                }
                else
                {
                    float inward = Vector2.Dot(state.Velocity, hit.normal);
                    if (inward < 0f) state.Velocity -= hit.normal * inward;
                }
            }
            else state.Position += travel;
            body.State = state;
        }

        bool Cast(Vector2 from, Vector2 size, Vector2 travel, out RaycastHit2D nearest)
        {
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = _player.GroundMask };
            int count = Physics2D.BoxCast(from, size, 0f, travel.normalized, filter, _hits, travel.magnitude);
            nearest = default;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];
                if (GroundProbe.IsPlatform(hit.collider) && (travel.y >= 0f || hit.normal.y < GroundProbe.MinNormal)) continue;
                if (nearest.collider == null || hit.distance < nearest.distance) nearest = hit;
            }
            return nearest.collider != null;
        }

        bool Triggered(Body body, Vector2 previous, double first, double last)
        {
            Vector2 position = body.State.Position;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (!target.CanAct) continue;
                bool touching = Touches(target, position, _mineSize);
                if (body.Ignored.Contains(target.NetworkObjectId))
                {
                    if (!touching) body.Ignored.Remove(target.NetworkObjectId);
                    continue;
                }
                if (touching || target.SweepHit(previous, position, first, last, Mathf.Min(_mineSize.x, _mineSize.y) * 0.5f, out _)) return true;
            }
            return false;
        }

        void Explode(BuffAreaState state)
        {
            bool mine = state.Kind == BuffAreaKind.Mine;
            float radius = mine ? _mineRadius : _bombRadius;
            float damage = mine ? _mineDamage : _bombDamage;
            float force = mine ? _mineKnockback : _bombKnockback;
            _fx.Play(BuffEffect.Explosion, state.Position, radius);
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (!target.CanAct || !mine && target == _player || !InRange(target, state.Position, radius)) continue;
                CombatDamage.Deal(_player.Health, target.Health, damage, DamageTag.JobSkill);
                float sign = Mathf.Sign(target.Body.position.x - state.Position.x);
                target.Drive.ApplyForce(new Vector2(sign, 0.5f) * force, ForceMode2D.Impulse);
            }
        }

        void Publish(BuffAreaState state) => _fx.Area(state.Id, state.Kind == BuffAreaKind.Mine ? BuffEffect.Mine : BuffEffect.Bomb, state.Position, armed: state.Kind == BuffAreaKind.Bomb && Now >= state.ArmAt);

        float Gravity(BuffAreaKind kind) => kind == BuffAreaKind.Mine ? _mineGravity : _bombGravity;

        public static bool InRange(NetPlayer target, Vector2 position, float radius) =>
            (target.Collider.ClosestPoint(position) - position).sqrMagnitude <= radius * radius;

        static bool Touches(NetPlayer target, Vector2 position, Vector2 size)
        {
            Vector2 offset = target.Collider.ClosestPoint(position) - position;
            return Mathf.Abs(offset.x) <= size.x * 0.5f && Mathf.Abs(offset.y) <= size.y * 0.5f;
        }

        void Remove(int index)
        {
            BuffAreaState state = _bodies[index].State;
            _fx.Area(state.Id, state.Kind == BuffAreaKind.Mine ? BuffEffect.Mine : BuffEffect.Bomb, state.Position, 1f, true);
            _bodies.RemoveAt(index);
        }

        void Clear()
        {
            if (_bodies.Count > 0)
            {
                for (int i = _bodies.Count - 1; i >= 0; i--) Remove(i);
            }
            _heals.Clear();
        }

        public override void OnNetworkDespawn()
        {
            _bodies.Clear();
            _heals.Clear();
        }
    }
}
