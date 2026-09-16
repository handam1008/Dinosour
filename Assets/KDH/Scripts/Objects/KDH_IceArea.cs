using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_IceArea : MonoBehaviour
    {
        [SerializeField] private float damage;
        [SerializeField] private float slowAmount;

        private const float Tick = 1f;

        private class PlayerState
        {
            public bool applyingEffect;
            public float timer;
        }

        private readonly Dictionary<Collider2D, PlayerState> _players = new();
        private readonly List<Collider2D> _keyBuffer = new();
        private readonly List<Collider2D> _toRemove = new();

        void Update()
        {
            if (_players.Count == 0) return;

            _keyBuffer.Clear();
            _keyBuffer.AddRange(_players.Keys);

            foreach (var col in _keyBuffer)
            {
                if (col == null)
                {
                    _toRemove.Add(col);
                    continue;
                }

                var state = _players[col];
                if (!state.applyingEffect) continue;

                state.timer += Time.deltaTime;

                if (state.timer >= Tick)
                {
                    ApplyDamage(col, damage);
                    ApplySlow(col, slowAmount);
                    state.timer = 0f;
                }
            }

            if (_toRemove.Count > 0)
            {
                foreach (var col in _toRemove)
                    _players.Remove(col);
                _toRemove.Clear();
            }
        }

        private void ApplyDamage(Collider2D player, float dmg)
        {
            if (player.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(dmg);
        }

        private void ApplySlow(Collider2D player, float amount)
        {
            if (player.TryGetComponent(out ISlowable slowable))
                slowable.ApplySlow(amount, 1f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerController _))
            {
                if (!_players.TryGetValue(other, out var state))
                {
                    state = new PlayerState();
                    _players[other] = state;
                }

                state.applyingEffect = false;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerController _))
            {
                if (!_players.TryGetValue(other, out var state))
                {
                    state = new PlayerState();
                    _players[other] = state;
                }

                state.applyingEffect = true;
                state.timer = 0f;
            }
        }
    }
}