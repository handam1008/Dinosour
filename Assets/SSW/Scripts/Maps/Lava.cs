using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class Lava : NetworkBehaviour
    {
        public struct Ember : INetworkSerializable
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 Size;
            public Vector3 Rotation;
            public Color32 Color;
            public float Life;
            public float Duration;
            public uint Seed;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Position);
                serializer.SerializeValue(ref Velocity);
                serializer.SerializeValue(ref Size);
                serializer.SerializeValue(ref Rotation);
                serializer.SerializeValue(ref Color);
                serializer.SerializeValue(ref Life);
                serializer.SerializeValue(ref Duration);
                serializer.SerializeValue(ref Seed);
            }
        }

        [SerializeField] ParticleSystem _effect;
        [SerializeField, Min(0f)] float _damage = 20f;
        ParticleSystem.Particle[] _particles;
        Ember[] _embers = System.Array.Empty<Ember>();
        double _sentAt;
        double _receivedAt;

        public override void OnNetworkSpawn()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particles = new ParticleSystem.Particle[_effect.main.maxParticles];
            if (IsServer)
            {
                _effect.Play(true);
                return;
            }
            var emission = _effect.emission;
            emission.enabled = false;
            var collision = _effect.collision;
            collision.enabled = false;
            var size = _effect.sizeOverLifetime;
            size.enabled = false;
            var sizeBySpeed = _effect.sizeBySpeed;
            sizeBySpeed.enabled = false;
            var color = _effect.colorOverLifetime;
            color.enabled = false;
            var colorBySpeed = _effect.colorBySpeed;
            colorBySpeed.enabled = false;
            _effect.Play(true);
            _effect.Pause(true);
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            double now = NetworkManager.ServerTime.Time;
            if (IsServer)
            {
                if (now < _sentAt + 0.05d) return;
                _sentAt = now;
                int count = _effect.GetParticles(_particles);
                if (_embers.Length != count) _embers = new Ember[count];
                for (int i = 0; i < count; i++)
                {
                    ParticleSystem.Particle particle = _particles[i];
                    _embers[i] = new Ember
                    {
                        Position = particle.position, Velocity = particle.totalVelocity,
                        Size = particle.GetCurrentSize3D(_effect), Rotation = particle.rotation3D,
                        Color = particle.GetCurrentColor(_effect), Life = particle.remainingLifetime,
                        Duration = particle.startLifetime, Seed = particle.randomSeed
                    };
                }
                EmbersRpc(now, _embers);
                return;
            }

            float age = Mathf.Max(0f, (float)(now - _receivedAt)) * Time.timeScale;
            float travel = Mathf.Min(age, 0.15f);
            int active = 0;
            foreach (Ember ember in _embers)
            {
                if (ember.Life <= age) continue;
                _particles[active++] = new ParticleSystem.Particle
                {
                    position = ember.Position + ember.Velocity * travel, velocity = ember.Velocity,
                    startSize3D = ember.Size, rotation3D = ember.Rotation, startColor = ember.Color,
                    remainingLifetime = ember.Life - age, startLifetime = ember.Duration,
                    randomSeed = ember.Seed
                };
            }
            _effect.SetParticles(_particles, active);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Unreliable)]
        void EmbersRpc(double time, Ember[] embers)
        {
            if (time <= _receivedAt) return;
            _receivedAt = time;
            _embers = embers;
        }

        void OnParticleCollision(GameObject other)
        {
            if (!IsServer || !NetGame.Current.CanFight || !other.TryGetComponent<NetPlayer>(out var player) || !player.CanAct) return;
            player.Health.ReceiveDamage(new DamageRequest(null, _damage, DamageTag.Environment));
        }

        public override void OnNetworkDespawn()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _embers = System.Array.Empty<Ember>();
        }
    }
}
