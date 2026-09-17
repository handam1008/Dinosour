using System;

namespace SSW
{
    public sealed class MotionHistory
    {
        public const int Capacity = 128;
        readonly MotionFrame[] _frames = new MotionFrame[Capacity];

        public void Store(MotionFrame frame) => _frames[frame.Tick % Capacity] = frame;

        public bool TryGet(uint tick, out MotionFrame frame)
        {
            frame = _frames[tick % Capacity];
            return tick != 0 && frame.Tick == tick;
        }

        public MotionFrame Get(uint tick) => TryGet(tick, out MotionFrame frame) ? frame : default;

        public void Clear() => Array.Clear(_frames, 0, _frames.Length);
    }
}
