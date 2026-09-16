using UnityEngine;

namespace SSW
{
    public sealed class Swing : MonoBehaviour, IMapAction, IMapReset
    {
        [SerializeField] Rigidbody2D _body;
        [SerializeField] DistanceJoint2D _joint;
        [SerializeField] LineRenderer _rope;
        [SerializeField] float _push = 2.2f;
        [SerializeField] float _surface = 0.5f;

        Vector2 _start;
        float _angle;

        public bool IsCut => !_joint.enabled;

        void Awake()
        {
            _start = _body.position;
            _angle = _body.rotation;
        }

        void LateUpdate()
        {
            if (_body.position.y < -20f) _body.simulated = false;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if (!collision.collider.TryGetComponent<IForceReceiver>(out _)) return;
            Rigidbody2D rider = collision.rigidbody;

            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 point = collision.GetContact(i).point;
                if (transform.InverseTransformPoint(point).y < _surface) continue;
                float speed = rider.linearVelocity.x - _body.GetPointVelocity(point).x;
                _body.AddForceAtPosition(Vector2.right * (Mathf.Clamp(speed, -7f, 7f) * _push), point);
                break;
            }
        }

        public void Use()
        {
            _joint.enabled = false;
            _rope.enabled = false;
            _body.WakeUp();
        }

        public void ResetMap()
        {
            _joint.enabled = false;
            _body.simulated = false;
            transform.SetPositionAndRotation(new Vector3(_start.x, _start.y, transform.position.z), Quaternion.Euler(0f, 0f, _angle));
            _body.position = _start;
            _body.rotation = _angle;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _joint.enabled = true;
            _rope.enabled = true;
            _body.simulated = true;
            _body.WakeUp();
        }
    }
}
