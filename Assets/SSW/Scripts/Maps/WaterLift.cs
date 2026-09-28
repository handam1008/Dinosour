using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-200)]
    public sealed class WaterLift : NetworkBehaviour, ILiftZone
    {
        [SerializeField, Min(0f)] float _delay;
        [SerializeField, Min(0f)] float _duration;
        [SerializeField, Min(0f)] float _force;
        [SerializeField] ParticleSystem[] _effects = System.Array.Empty<ParticleSystem>();
        readonly NetworkVariable<double> _startedAt = new NetworkVariable<double>(-1d);
        bool _playing;

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled || !IsSpawned || !NetGame.Current.CanFight || _startedAt.Value < 0d) return false;
                if (_duration <= 0f) return true;
                double age = System.Math.Max(0d, NetworkManager.ServerTime.Time - _startedAt.Value);
                return age % (_delay + _duration) >= _delay;
            }
        }

        public float RiseSpeed => float.PositiveInfinity;
        public float Acceleration => _force / Time.fixedDeltaTime;

        public override void OnNetworkSpawn() => StopEffects();

        void FixedUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer)
            {
                if (NetGame.Current.CanFight)
                {
                    if (_startedAt.Value < 0d) _startedAt.Value = NetworkManager.ServerTime.Time;
                }
                else if (_startedAt.Value >= 0d) _startedAt.Value = -1d;
            }
            bool active = Active;
            if (active == _playing) return;
            _playing = active;
            foreach (ParticleSystem effect in _effects)
            {
                if (active) effect.Play(true);
                else effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!IsServer || !Active) return;
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;
            body.AddForce(Vector2.up * _force, ForceMode2D.Impulse);
        }

        void StopEffects()
        {
            _playing = false;
            foreach (ParticleSystem effect in _effects)
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void OnDisable() => StopEffects();
        public override void OnNetworkDespawn() => StopEffects();
    }
}
