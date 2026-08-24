using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Rigidbody2DForceReceiver : MonoBehaviour, IForceReceiver
    {
        Rigidbody2D _body;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        public void ApplyForce(Vector2 force, ForceMode2D mode)
        {
            if (_body == null)
                _body = GetComponent<Rigidbody2D>();

            _body.AddForce(force, mode);
        }
    }
}
