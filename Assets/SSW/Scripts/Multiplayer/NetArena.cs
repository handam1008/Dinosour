using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-200)]
    public sealed class NetArena : MonoBehaviour
    {
        [SerializeField] Camera _view;
        [SerializeField] SandboxCameraFollow _follow;
        [SerializeField] SpawnPoints _spawnPoints;
        [SerializeField] GameObject _baseMap;
        [SerializeField] GameObject[] _offline;
        [SerializeField] Behaviour[] _localTools;
        [SerializeField] float _fallY = -12f;
        [SerializeField] Practice _practice;

        public Camera View => _view;
        public int Side { get; private set; } = 1;
        public BattleMap Map { get; private set; }
        public float FallY => Map != null ? Map.FallY : _fallY;
        public Vector3 Spawn(int slot) => Map != null ? Map.Spawn(slot) : _spawnPoints.At(slot);

        public void UseMap(BattleMap map)
        {
            Map = map;
            _baseMap.SetActive(false);
            _follow.SetBounds(map.Bounds);
        }

        void Awake()
        {
            bool local = NetGame.Current == null || !NetGame.Current.Connected;
            foreach (GameObject item in _offline) item.SetActive(false);
            foreach (Behaviour item in _localTools) item.enabled = false;
            _practice.enabled = local;
            if (!local) NetGame.Current.Enter(this);
        }

        public void Follow(Transform local, Transform opponent, int side)
        {
            Side = side;
            Transform camera = _view.transform;
            Vector3 position = camera.position;
            position.z = -Mathf.Abs(position.z) * side;
            camera.SetPositionAndRotation(position, Quaternion.Euler(0f, side < 0 ? 180f : 0f, 0f));
            _view.transparencySortMode = TransparencySortMode.CustomAxis;
            _view.transparencySortAxis = Vector3.forward;
            if (_follow != null) _follow.SetTargets(local, opponent);
        }

        void OnDestroy()
        {
            if (NetGame.Current != null) NetGame.Current.Leave(this);
        }
    }
}