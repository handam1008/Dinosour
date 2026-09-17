using UnityEngine;

namespace SSW
{
    public sealed class HitTrack
    {
        struct Sample
        {
            public double Time;
            public uint Epoch;
            public Vector2 Position;
        }

        readonly Sample[] _samples = new Sample[256];
        int _next;
        int _count;

        public void Store(double time, uint epoch, Vector2 position)
        {
            if (_count > 0 && _samples[(_next + 255) % 256].Time == time)
            {
                _samples[(_next + 255) % 256] = new Sample { Time = time, Epoch = epoch, Position = position };
                return;
            }
            _samples[_next] = new Sample { Time = time, Epoch = epoch, Position = position };
            _next = (_next + 1) % 256;
            _count = Mathf.Min(_count + 1, 256);
        }

        public bool Read(double time, uint epoch, out Vector2 position)
        {
            position = default;
            if (_count == 0) return false;
            Sample last = _samples[(_next + 255) % 256];
            if (time >= last.Time)
            {
                position = last.Position;
                return last.Epoch == epoch && time - last.Time <= 0.05d;
            }
            for (int i = 1; i < _count; i++)
            {
                Sample first = _samples[(_next + 255 - i) % 256];
                if (first.Time <= time)
                {
                    if (first.Epoch != epoch || last.Epoch != epoch) return false;
                    float alpha = (float)((time - first.Time) / (last.Time - first.Time));
                    position = Vector2.Lerp(first.Position, last.Position, alpha);
                    return true;
                }
                last = first;
            }
            return false;
        }
    }
}
