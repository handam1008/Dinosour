using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class SakuraField : NetworkBehaviour
    {
        [SerializeField, Min(0f)] float _duration = 0.5f;
        [SerializeField, Min(0f)] float _speed = 0.2f;
        [SerializeField] ParticleSystem _effect;
        [SerializeField] ParticleSystem _petals;
        readonly NetworkVariable<double> _startedAt = new NetworkVariable<double>(-1);
        readonly Dictionary<ulong, float> _shownAt = new Dictionary<ulong, float>();

        public override void OnNetworkSpawn()
        {
            _petals.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _petals.useAutoRandomSeed = false;
            _petals.randomSeed = unchecked((uint)NetworkObjectId * 747796405u + 2891336453u);
            _startedAt.OnValueChanged += Started;
            if (IsServer) _startedAt.Value = NetworkManager.ServerTime.Time;
            else if (_startedAt.Value >= 0) Started(-1, _startedAt.Value);
        }

        void Started(double previous, double current)
        {
            float age = (float)System.Math.Max(0, NetworkManager.ServerTime.Time - current);
            _petals.Simulate(age, true, true, true);
            _petals.Play(true);
        }

        void OnParticleCollision(GameObject other)
        {
            if (!IsServer || !NetGame.Current.CanFight || !other.TryGetComponent<NetPlayer>(out var player) || !player.CanAct) return;
            player.Motion.ApplySpeed(_speed, _duration);
            if (_shownAt.TryGetValue(player.NetworkObjectId, out float last) && Time.time < last + 0.3f) return;
            _shownAt[player.NetworkObjectId] = Time.time;
            EffectRpc(player.NetworkObjectId);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        void EffectRpc(ulong playerId)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerId, out var obj)) return;
            var player = obj.GetComponent<NetPlayer>();
            ParticleSystem effect = Instantiate(_effect, player.View.position, Quaternion.identity, transform);
            effect.Play(true);
        }

        public override void OnNetworkDespawn()
        {
            _startedAt.OnValueChanged -= Started;
            _shownAt.Clear();
        }
    }
}