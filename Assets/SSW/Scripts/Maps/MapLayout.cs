using UnityEngine;

namespace SSW
{
    public sealed class MapLayout : MonoBehaviour
    {
        [SerializeField] string _title;
        [SerializeField, TextArea] string _description;
        [SerializeField] string _tip;
        [SerializeField] Color _accent;
        [SerializeField] Color _background;
        [SerializeField] Vector2 _viewSize = new Vector2(29f, 17f);
        [SerializeField] Vector2 _viewCenter;
        [SerializeField] Transform _spawn;
        [SerializeField] Transform _target;
        [SerializeField] bool _resetOnFall;
        [SerializeField] MonoBehaviour[] _resets;

        public string Title => _title;
        public string Description => _description;
        public string Tip => _tip;
        public Color Accent => _accent;
        public Color Background => _background;
        public Vector2 ViewSize => _viewSize;
        public Vector2 ViewCenter => _viewCenter;
        public Vector3 Spawn => _spawn.position;
        public Vector3 Target => _target.position;
        public bool ResetOnFall => _resetOnFall;

        public void ResetMap()
        {
            foreach (MonoBehaviour behaviour in _resets)
                ((IMapReset)behaviour).ResetMap();
        }
    }
}
