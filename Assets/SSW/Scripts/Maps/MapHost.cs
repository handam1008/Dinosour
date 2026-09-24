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
            _follow.enabled = false;
            _camera.backgroundColor = Map.Background;
            Map.ResetMap();
        }

        void LateUpdate()
        {
            _camera.transform.position = new Vector3(Map.ViewCenter.x, Map.ViewCenter.y, -10f);
            _camera.orthographicSize = Mathf.Max(Map.ViewSize.y * 0.5f, Map.ViewSize.x / (2f * _camera.aspect));
        }
    }
}
