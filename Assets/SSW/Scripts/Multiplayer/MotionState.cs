using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public struct MotionState : INetworkSerializable
    {
        public uint Epoch;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Aim;
        public Vector2 Surface;
        public float External;
        public float DropTime;
        public float DropTop;
        public uint Jump;
        public uint Pulse;
        public float CoyoteTime;
        public bool Grounded;
        public bool Pad;
        public float DashTime;
        public float DashSpeed;
        public uint DashAction;
        public float Scale;
        public float SmallTime;
        public float BodyScale;
        public byte AirJumps;
        public byte AirUsed;
        public float AirJumpRatio;
        public float FreezeTime;
        public Vector2 BurstVelocity;
        public float BurstTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Epoch);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Aim);
            serializer.SerializeValue(ref Surface);
            serializer.SerializeValue(ref External);
            serializer.SerializeValue(ref DropTime);
            serializer.SerializeValue(ref DropTop);
            serializer.SerializeValue(ref Jump);
            serializer.SerializeValue(ref Pulse);
            serializer.SerializeValue(ref CoyoteTime);
            serializer.SerializeValue(ref Grounded);
            serializer.SerializeValue(ref Pad);
            serializer.SerializeValue(ref DashTime);
            serializer.SerializeValue(ref DashSpeed);
            serializer.SerializeValue(ref DashAction);
            serializer.SerializeValue(ref Scale);
            serializer.SerializeValue(ref SmallTime);
            serializer.SerializeValue(ref BodyScale);
            serializer.SerializeValue(ref AirJumps);
            serializer.SerializeValue(ref AirUsed);
            serializer.SerializeValue(ref AirJumpRatio);
            serializer.SerializeValue(ref FreezeTime);
            serializer.SerializeValue(ref BurstVelocity);
            serializer.SerializeValue(ref BurstTime);
        }
    }
}
