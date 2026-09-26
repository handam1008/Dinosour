using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-300)]
    public sealed class MapOrbit : NetworkBehaviour
    {
        [SerializeField] Transform _pivot;
        [SerializeField] Rigidbody2D[] _parts;
        [SerializeField] float _speed;
        [SerializeField] bool _level = true;
        Vector2[] _offsets;
        float _angle;

        public override void OnNetworkSpawn()
        {
            _angle = 0f;
            _offsets = new Vector2[_parts.Length];
            for (int i = 0; i < _parts.Length; i++)
                _offsets[i] = _parts[i].position - (Vector2)_pivot.position;
        }

        void FixedUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            float turn = _speed * Time.fixedDeltaTime;
            _angle = Mathf.Repeat(_angle + turn, 360f);
            Quaternion rotation = Quaternion.Euler(0f, 0f, _angle);
            for (int i = 0; i < _parts.Length; i++)
            {
                Rigidbody2D part = _parts[i];
                Vector2 target = _pivot.position + rotation * _offsets[i];
                part.linearVelocity = (target - part.position) / Time.fixedDeltaTime;
                part.MovePosition(target);
                if (!_level)
                {
                    part.angularVelocity = _speed;
                    part.MoveRotation(part.rotation + turn);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (Rigidbody2D part in _parts)
            {
                part.linearVelocity = Vector2.zero;
                part.angularVelocity = 0f;
            }
        }
    }
}
