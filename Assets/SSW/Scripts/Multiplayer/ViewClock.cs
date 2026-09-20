using UnityEngine;

namespace SSW
{
    public struct ViewClock
    {
        bool _started;
        public double Time { get; private set; }

        public void Reset(double time)
        {
            Time = time;
            _started = true;
        }

        public double Step(double target, float delta)
        {
            if (!_started) Reset(target);
            else
            {
                float rate = Mathf.Clamp(1f + (float)(target - Time - delta) * 2f, 0.9f, 1.1f);
                Time += delta * rate;
            }
            return Time;
        }
    }
}
