using System;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public struct ShotPose : INetworkSerializable, IEquatable<ShotPose>
    {
        public double Time;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Gravity;
        public float Angle;
        public float Spin;
        public bool Terrain;
        public uint Turn;

        public Vector2 Point(double time)
        {
            float age = Mathf.Clamp((float)(time - Time), 0f, 1f);
            return Position + Velocity * age + Gravity * (0.5f * age * (age + UnityEngine.Time.fixedDeltaTime));
        }

        public float Rotation(double time) => Angle + Spin * Mathf.Clamp((float)(time - Time), 0f, 1f);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Time);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Gravity);
            serializer.SerializeValue(ref Angle);
            serializer.SerializeValue(ref Spin);
            serializer.SerializeValue(ref Terrain);
            serializer.SerializeValue(ref Turn);
        }

        public bool Equals(ShotPose other) => Time == other.Time && Position == other.Position
            && Velocity == other.Velocity && Gravity == other.Gravity && Angle == other.Angle && Spin == other.Spin && Terrain == other.Terrain && Turn == other.Turn;
    }
}
