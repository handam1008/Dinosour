using System;

namespace SSW
{
    public struct InputBudget
    {
        double _time;
        double _credit;

        public void Reset(double now)
        {
            _time = now;
            _credit = 0d;
        }

        public void Advance(double now, float step)
        {
            _credit = Math.Min(_credit + Math.Max(0d, now - _time), MotionHistory.Capacity * (double)step);
            _time = Math.Max(_time, now);
        }

        public void Seed(int count, float step)
        {
            _credit = Math.Max(_credit, Math.Min(MotionHistory.Capacity, count) * (double)step);
        }

        public bool Ready(float step) => _credit + 0.000001d >= step;

        public void Spend(float step) => _credit = Math.Max(0d, _credit - step);
    }
}
