using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public enum CastKind : byte { Press, Release, Cycle, StopCycle, Cancel }

    public struct CastInput : INetworkSerializable
    {
        public uint Action;
        public uint Epoch;
        public uint Tick;
        public CastKind Kind;
        public Vector2 Direction;
        public double ViewTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Action);
            serializer.SerializeValue(ref Epoch);
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref Direction);
            serializer.SerializeValue(ref ViewTime);
        }
    }
}
