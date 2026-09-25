using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public struct MotionFrame : INetworkSerializable
    {
        public uint Tick;
        public uint Jump;
        public uint Pulse;
        public Vector2 Move;
        public Vector2 Aim;

        public bool Valid => Tick != 0 && NetMath.Finite(Move) && NetMath.Finite(Aim)
            && Move.sqrMagnitude <= 1.001f && Aim.sqrMagnitude <= 1.001f;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Jump);
            serializer.SerializeValue(ref Pulse);
            serializer.SerializeValue(ref Move);
            serializer.SerializeValue(ref Aim);
        }
    }
}
