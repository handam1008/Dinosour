using System;

namespace SSW
{
    public sealed class MapBag
    {
        readonly int[] _order;
        readonly Random _random;
        int _next;
        int _last = -1;

        public int Remaining => _order.Length - _next;

        public MapBag(int count, int seed)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            _order = new int[count];
            _random = new Random(seed);
            for (int i = 0; i < count; i++) _order[i] = i;
            Shuffle();
        }

        public int Next()
        {
            if (Remaining == 0) Shuffle();
            return _last = _order[_next++];
        }

        void Shuffle()
        {
            for (int i = _order.Length - 1; i > 0; i--)
            {
                int other = _random.Next(i + 1);
                (_order[i], _order[other]) = (_order[other], _order[i]);
            }
            if (_order.Length > 1 && _order[0] == _last)
            {
                int other = _random.Next(1, _order.Length);
                (_order[0], _order[other]) = (_order[other], _order[0]);
            }
            _next = 0;
        }
    }
}
