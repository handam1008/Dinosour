using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public struct MotionRate : INetworkSerializable
    {
        public float Base;
        public float Slow;
        public float Haste;
        public float SlowTime;
        public float HasteTime;

        public float Value => Base * (SlowTime > 0f ? Slow : 1f) * (HasteTime > 0f ? Haste : 1f);

        public void Advance(float delta)
        {
            SlowTime = Mathf.Max(0f, SlowTime - delta);
            HasteTime = Mathf.Max(0f, HasteTime - delta);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Base);
            serializer.SerializeValue(ref Slow);
            serializer.SerializeValue(ref Haste);
            serializer.SerializeValue(ref SlowTime);
            serializer.SerializeValue(ref HasteTime);
        }
    }
}
