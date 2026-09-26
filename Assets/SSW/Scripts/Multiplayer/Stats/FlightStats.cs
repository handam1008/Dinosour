using System;
using Unity.Netcode;

namespace SSW
{
    [Serializable]
    public struct FlightStats : INetworkSerializable, IEquatable<FlightStats>
    {
        public float Speed;
        public float Gravity;
        public float Life;
        public float Spin;
        public float Scale;
        public float Aspect;
        public static FlightStats Default => new FlightStats { Aspect = 1f };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Speed);
            serializer.SerializeValue(ref Gravity);
            serializer.SerializeValue(ref Life);
            serializer.SerializeValue(ref Spin);
            serializer.SerializeValue(ref Scale);
            serializer.SerializeValue(ref Aspect);
        }

        public bool Equals(FlightStats other) => Speed.Equals(other.Speed) && Gravity.Equals(other.Gravity)
            && Life.Equals(other.Life) && Spin.Equals(other.Spin) && Scale.Equals(other.Scale) && Aspect.Equals(other.Aspect);
    }
}
