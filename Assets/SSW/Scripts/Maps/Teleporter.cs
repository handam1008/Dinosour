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

            _readyTime = Time.time + _cooldown;
            rb.transform.position = _destination.position;
        }
    }
}
