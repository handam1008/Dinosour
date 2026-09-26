using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace SSW
{
    [DefaultExecutionOrder(-150)]
    public sealed class MapGust : NetworkBehaviour
    {
        [SerializeField] Vector2 _direction;
        [SerializeField] float _force;
        [SerializeField, Min(0.05f)] float _interval = 2f;
        [SerializeField] ParticleSystem _effect;
        [SerializeField] MapTint _tint;
        [SerializeField] UnityEvent _onForce;
        readonly NetworkVariable<double> _startedAt = new NetworkVariable<double>(-1);
        uint _pulse;

        public uint PulseCount => _pulse;

        void FixedUpdate()
        {
            if (!IsSpawned) return;
            NetGame game = NetGame.Current;
            if (!game.CanFight)
            {
                if (IsServer && _startedAt.Value >= 0) _startedAt.Value = -1;
                _pulse = 0;
                return;
            }
            if (IsServer && _startedAt.Value < 0) _startedAt.Value = NetworkManager.ServerTime.Time;
            if (_startedAt.Value < 0) return;
            uint pulse = (uint)(System.Math.Max(0, NetworkManager.ServerTime.Time - _startedAt.Value) / _interval);
            if (pulse <= _pulse) return;
            _pulse = pulse;
            foreach (NetPlayer player in game.Players)
                player.Drive.Pulse(pulse, _direction.normalized * _force);
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.Play(true);
            _tint.Play();
            _onForce.Invoke();
        }
    }
}