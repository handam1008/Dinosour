using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-200)]
    public sealed class MapFlood : NetworkBehaviour
    {
        [SerializeField] Transform _water;
        [SerializeField] Transform _target;
        [SerializeField, Min(0f)] float _duration;
        readonly NetworkVariable<double> _startedAt = new NetworkVariable<double>(-1d);
        Vector3 _origin;
        float _height;

        public override void OnNetworkSpawn()
        {
            _origin = _water.position;
            _height = _target.position.y;
        }

        void FixedUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer && _startedAt.Value < 0d && NetGame.Current.CanFight)
                _startedAt.Value = NetworkManager.ServerTime.Time;
            if (_startedAt.Value < 0d) return;
            float age = (float)System.Math.Max(0d, NetworkManager.ServerTime.Time - _startedAt.Value);
            float progress = _duration > 0f ? Mathf.Clamp01(age / _duration) : 1f;
            Vector3 position = _origin;
            position.y = Mathf.Lerp(_origin.y, _height, 1f - (1f - progress) * (1f - progress));
            if (_water.position == position) return;
            _water.position = position;
            Physics2D.SyncTransforms();
        }
    }
}
