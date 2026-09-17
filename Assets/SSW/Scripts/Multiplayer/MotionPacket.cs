using Unity.Netcode;

namespace SSW
{
    public struct MotionPacket : INetworkSerializable
    {
        public uint Epoch;
        MotionFrame _a;
        MotionFrame _b;
        MotionFrame _c;
        MotionFrame _d;
        MotionFrame _e;
        MotionFrame _f;

        public MotionPacket(MotionHistory history, uint tick, uint epoch)
        {
            Epoch = epoch;
            _a = history.Get(tick);
            _b = tick > 1 ? history.Get(tick - 1) : default;
            _c = tick > 2 ? history.Get(tick - 2) : default;
            _d = tick > 3 ? history.Get(tick - 3) : default;
            _e = tick > 4 ? history.Get(tick - 4) : default;
            _f = tick > 5 ? history.Get(tick - 5) : default;
        }

        public MotionFrame this[int index] => index switch
        {
            0 => _a,
            1 => _b,
            2 => _c,
            3 => _d,
            4 => _e,
            _ => _f
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Epoch);
            serializer.SerializeValue(ref _a);
            serializer.SerializeValue(ref _b);
            serializer.SerializeValue(ref _c);
            serializer.SerializeValue(ref _d);
            serializer.SerializeValue(ref _e);
            serializer.SerializeValue(ref _f);
        }
    }
}
