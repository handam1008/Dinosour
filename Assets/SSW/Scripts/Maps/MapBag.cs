using System;

namespace SSW
{
    public sealed class MapBag
    {
        readonly int[] _order;
        int _next;

        public int Remaining => _order.Length - _next;

        public MapBag(int count, int seed)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            _order = new int[count];
            for (int i = 0; i < count; i++) _order[i] = i;
            var random = new Random(seed);
            for (int i = count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                (_order[i], _order[other]) = (_order[other], _order[i]);
            }
        }

        public int Next()
        {
            if (Remaining == 0) throw new InvalidOperationException("사용하지 않은 맵이 없습니다.");
            return _order[_next++];
        }
    }
}
