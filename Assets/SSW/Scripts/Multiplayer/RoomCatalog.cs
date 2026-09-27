using System.Collections.Generic;
using System.Linq;

namespace SSW
{
    public sealed class RoomCatalog
    {
        readonly List<MultiplayerRoomInfo> _items = new List<MultiplayerRoomInfo>();
        readonly Dictionary<string, double> _unavailable = new Dictionary<string, double>();

        public IReadOnlyList<MultiplayerRoomInfo> Items => _items;
        public int Version { get; private set; }

        public void Replace(IEnumerable<MultiplayerRoomInfo> rooms, double now)
        {
            foreach (string id in _unavailable.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray())
                _unavailable.Remove(id);
            var next = rooms.Where(room => room.PlayerCount == 1 && !_unavailable.ContainsKey(room.Id)).ToList();
            if (_items.SequenceEqual(next)) return;
            _items.Clear();
            _items.AddRange(next);
            Version++;
        }

        public void Reject(string id, double now)
        {
            _unavailable[id] = now + 30d;
            if (_items.RemoveAll(room => room.Id == id) > 0) Version++;
        }

        public void Clear()
        {
            if (_items.Count == 0) return;
            _items.Clear();
            Version++;
        }
    }
}
