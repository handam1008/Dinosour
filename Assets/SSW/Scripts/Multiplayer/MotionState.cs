using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public struct MotionState : INetworkSerializable
    {
        public uint Epoch;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Surface;
        public float External;
        public float DropTime;
        public float DropTop;
        public uint Jump;
        public bool Grounded;
        public bool Pad;
        public float DashTime;
        public float DashSpeed;
        public uint DashAction;
        public float Scale;
        public float SmallTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Epoch);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Surface);
            serializer.SerializeValue(ref External);
            serializer.SerializeValue(ref DropTime);
            serializer.SerializeValue(ref DropTop);
            serializer.SerializeValue(ref Jump);
            serializer.SerializeValue(ref Grounded);
            serializer.SerializeValue(ref Pad);
            serializer.SerializeValue(ref DashTime);
            serializer.SerializeValue(ref DashSpeed);
            serializer.SerializeValue(ref DashAction);
            serializer.SerializeValue(ref Scale);
            serializer.SerializeValue(ref SmallTime);
        }
    }
}
