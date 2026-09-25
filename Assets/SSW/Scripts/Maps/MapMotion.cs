using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-250)]
    public sealed class MapMotion : NetworkBehaviour
    {
        public struct Pose : INetworkSerializable
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Angle;
            public float Spin;
            public bool Active;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Position);
                serializer.SerializeValue(ref Velocity);
                serializer.SerializeValue(ref Angle);
                serializer.SerializeValue(ref Spin);
                serializer.SerializeValue(ref Active);
            }
        }

        [SerializeField] Rigidbody2D[] _bodies;
        [SerializeField] Transform[] _moving;
        [SerializeField] Pin[] _pins;
        [SerializeField] Joint2D[] _joints;
        Pose[] _poses;
        Vector3[] _positions;
        bool[] _cuts;
        double _sentAt;
        double _receivedAt;
        bool _received;

        public override void OnNetworkSpawn()
        {
            _poses = new Pose[_bodies.Length];
            _positions = new Vector3[_moving.Length];
            _cuts = new bool[_pins.Length];
            if (IsServer) return;
            foreach (Joint2D joint in _joints) joint.enabled = false;
            foreach (Rigidbody2D body in _bodies) body.bodyType = RigidbodyType2D.Kinematic;
        }

        void FixedUpdate()
        {
            if (!IsSpawned) return;
            double now = NetworkManager.ServerTime.Time;
            if (IsServer)
            {
                if (_poses.Length + _positions.Length + _cuts.Length == 0 || now < _sentAt + 1.0 / 30.0) return;
                _sentAt = now;
                for (int i = 0; i < _bodies.Length; i++)
                {
                    Rigidbody2D body = _bodies[i];
                    _poses[i] = new Pose { Position = body.position, Velocity = body.linearVelocity,
                        Angle = body.rotation, Spin = body.angularVelocity, Active = body.simulated };
                }
                for (int i = 0; i < _moving.Length; i++) _positions[i] = _moving[i].localPosition;
                for (int i = 0; i < _pins.Length; i++) _cuts[i] = _pins[i].Current <= 0f;
                MotionRpc(now, _poses, _positions, _cuts);
            }
            else if (_received)
            {
                float age = Mathf.Clamp((float)(now - _receivedAt), 0f, 0.1f) * Time.timeScale;
                for (int i = 0; i < _bodies.Length; i++)
                {
                    Pose pose = _poses[i];
                    Rigidbody2D body = _bodies[i];
                    body.simulated = pose.Active;
                    body.position = pose.Position + pose.Velocity * age;
                    body.rotation = pose.Angle + pose.Spin * age;
                    body.linearVelocity = pose.Velocity;
                    body.angularVelocity = pose.Spin;
                }
                for (int i = 0; i < _moving.Length; i++) _moving[i].localPosition = _positions[i];
            }
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Unreliable)]
        void MotionRpc(double time, Pose[] poses, Vector3[] positions, bool[] cuts)
        {
            if (_received && time <= _receivedAt) return;
            _received = true;
            _receivedAt = time;
            _poses = poses;
            _positions = positions;
            for (int i = 0; i < cuts.Length; i++)
                if (cuts[i] && _pins[i].Current > 0f) _pins[i].TakeDamage(float.MaxValue);
        }
    }
}
