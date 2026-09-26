using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(200)]
    public sealed class MapArea : MonoBehaviour
    {
        struct State
        {
            public bool Inside;
            public bool Armed;
            public bool Active;
            public bool Applied;
            public float Time;
        }

        [SerializeField] BattleMap _map;
        [SerializeField] Collider2D _shape;
        [SerializeField] MonoBehaviour _phase;
        [SerializeField, Min(0f)] float _damage;
        [SerializeField, Min(0f)] float _force;
        [SerializeField, Min(0f)] float _delay;
        [SerializeField, Min(0f)] float _interval;
        [SerializeField, Range(0f, 1f)] float _slow;
        [SerializeField, Min(0f)] float _slowDuration;
        [SerializeField] bool _outside;
        [SerializeField] bool _contact;
        readonly Dictionary<ulong, State> _states = new Dictionary<ulong, State>();
        IMapPhase _clock;

        void Awake() => _clock = _phase as IMapPhase;

        void FixedUpdate()
        {
            if (!_map.IsSpawned || !_map.IsServer) return;
            NetGame game = NetGame.Current;
            if (!game.CanFight || !_shape.enabled || !_shape.gameObject.activeInHierarchy
                || _clock != null && !_clock.Active)
            {
                _states.Clear();
                return;
            }
            foreach (NetPlayer player in game.Players)
            {
                if (!player.CanAct)
                {
                    _states.Remove(player.NetworkObjectId);
                    continue;
                }
                _states.TryGetValue(player.NetworkObjectId, out State state);
                ColliderDistance2D distance = player.Collider.Distance(_shape);
                float reach = _contact ? (state.Inside ? 0.07f : 0.035f) : 0f;
                bool inside = distance.isValid && distance.distance <= reach;
                if (inside) state.Armed = true;
                bool active = _outside ? state.Armed && !inside : inside;
                state.Inside = inside;
                if (!active)
                {
                    state.Active = state.Applied = false;
                    state.Time = 0f;
                }
                else
                {
                    if (!state.Active)
                    {
                        state.Active = true;
                        state.Applied = false;
                        state.Time = 0f;
                    }
                    float due = state.Applied ? _interval : _delay;
                    if (!state.Applied || _interval > 0f)
                    {
                        state.Time += Time.fixedDeltaTime;
                        if (state.Time + 0.00001f >= due)
                        {
                            state.Time = Mathf.Max(0f, state.Time - due);
                            state.Applied = true;
                            Apply(player);
                        }
                    }
                }
                _states[player.NetworkObjectId] = state;
            }
        }

        void Apply(NetPlayer player)
        {
            if (_damage > 0f)
                player.Health.ReceiveDamage(new DamageRequest(null, _damage, DamageTag.Environment));
            if (_slowDuration > 0f) player.Motion.ApplySlow(_slow, _slowDuration);
            if (_force > 0f)
            {
                Vector2 direction = player.Drive.Position - (Vector2)_shape.transform.position;
                if (direction.sqrMagnitude < 0.0001f) direction = Vector2.up;
                player.Drive.ApplyForce(direction.normalized * _force, ForceMode2D.Impulse);
            }
        }

        void OnDisable() => _states.Clear();
    }
}
