using UnityEngine;

namespace SSW
{
    public sealed class DraftPointer : System.IDisposable
    {
        static DraftPointer _owner;
        static bool _visible;

        public void Hide()
        {
            if (_owner == this) return;
            if (_owner == null) _visible = Cursor.visible;
            _owner = this;
            Cursor.visible = false;
        }

        public void Dispose()
        {
            if (_owner != this) return;
            Cursor.visible = _visible;
            _owner = null;
        }
    }
}
