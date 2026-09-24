using UnityEngine;

namespace SSW
{
    public class Teleporter : MonoBehaviour
    {
        [SerializeField] Transform _destination;
        [SerializeField] float _cooldown = 1f;

        static float _readyTime;

        void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || _destination == null) return;
            if (Time.time < _readyTime) return;

            if (rb.TryGetComponent<NetPlayer>(out var player))
            {
                if (!player.IsServer) return;
                _readyTime = Time.time + _cooldown;
                player.Drive.Teleport(_destination.position);
                return;
            }
            _readyTime = Time.time + _cooldown;
            if (rb.TryGetComponent<PlayerController>(out var motion)) motion.Teleport(_destination.position);
            else rb.position = _destination.position;
        }
    }
}
