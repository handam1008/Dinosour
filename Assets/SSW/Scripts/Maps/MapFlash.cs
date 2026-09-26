using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class MapFlash : NetworkBehaviour
    {
        [SerializeField] MapTint _tint;
        readonly NetworkVariable<double> _at = new NetworkVariable<double>(-1d);

        public override void OnNetworkSpawn()
        {
            _at.OnValueChanged += Changed;
            if (IsServer && _at.Value < 0d) _at.Value = NetworkManager.ServerTime.Time;
            else if (_at.Value >= 0d) Changed(-1d, _at.Value);
        }

        public void Play()
        {
            if (IsSpawned && IsServer) _at.Value = NetworkManager.ServerTime.Time;
        }

        void Changed(double previous, double current)
        {
            if (current < 0d) return;
            _tint.Play((float)System.Math.Max(0d, NetworkManager.ServerTime.Time - current));
        }

        void LateUpdate()
        {
            if (IsSpawned && _at.Value >= 0d)
                _tint.Sample((float)System.Math.Max(0d, NetworkManager.ServerTime.Time - _at.Value));
        }

        public override void OnNetworkDespawn() => _at.OnValueChanged -= Changed;
    }
}
