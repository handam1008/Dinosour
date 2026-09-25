using UnityEngine;

namespace SSW
{
    public sealed class MapHost : MonoBehaviour
    {
        [SerializeField] MapLayout _placedMap;
        [SerializeField] GameObject _baseMap;
        [SerializeField] Camera _camera;
        [SerializeField] SandboxCameraFollow _follow;

        public MapLayout Map { get; private set; }

        public void Initialize()
        {
            MapLayout selected = MapTravel.Take();
            Map = selected != null ? Instantiate(selected) : _placedMap;
            enabled = Map != null;
            if (!enabled) return;
            _baseMap.SetActive(false);
            _follow.SetBounds(new Bounds(Map.ViewCenter, Map.ViewSize));
            _camera.backgroundColor = Map.Background;
            Map.ResetMap();
        }
    }
}
