using System;
using UnityEngine;

namespace SSW
{
    public struct SnapshotClock
    {
        ViewClock _clock;
        double _received;
        float _delay;

        public double Time => _clock.Time;
        public float Delay => _delay;

        public void Observe(double sent, double now, float interval)
        {
            float elapsed = (float)Math.Max(0d, sent - _received);
            float needed = Mathf.Clamp((float)(now - sent) + interval * 2f, interval * 2f, 0.2f);
            _delay = Mathf.Max(needed, _delay - elapsed * 0.02f);
            _received = sent;
        }

        public double Step(double target, double oldest, double newest, float delta)
        {
            double time = _clock.Step(target - _delay, delta);
            _clock.Reset(Math.Max(oldest, Math.Min(newest, time)));
            return _clock.Time;
        }
    }
}
