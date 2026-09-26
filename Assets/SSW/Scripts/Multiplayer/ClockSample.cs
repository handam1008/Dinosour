using System;
using Unity.Netcode;

namespace SSW
{
    internal struct ClockSample : INetworkSerializable, IEquatable<ClockSample>
    {
        public double Game;
        public double Network;
        public float Scale;

        public double At(double time) => Game + Math.Max(0d, time - Network) * Scale;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Game);
            serializer.SerializeValue(ref Network);
            serializer.SerializeValue(ref Scale);
        }

        public bool Equals(ClockSample other) => Game == other.Game && Network == other.Network && Scale == other.Scale;
    }
}
