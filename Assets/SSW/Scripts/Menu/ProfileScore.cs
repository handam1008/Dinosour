namespace SSW
{
    public sealed class ProfileScore
    {
        int _revision;
        bool _reported;

        public bool HasValue { get; private set; }
        public double Value { get; private set; }

        public int BeginRead() => ++_revision;

        public bool CompleteRead(int revision, double value)
        {
            if (revision != _revision || _reported && value != Value) return false;
            Value = value;
            HasValue = true;
            _reported = false;
            return true;
        }

        public void Report(double value)
        {
            ++_revision;
            Value = value;
            HasValue = true;
            _reported = true;
        }
    }
}
