using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-200)]
    public sealed class NetArena : MonoBehaviour
    {
        [SerializeField] Camera _view;
        [SerializeField] SandboxCameraFollow _follow;
        [SerializeField] Transform[] _spawns;
        [SerializeField] GameObject[] _offline;
        [SerializeField] Behaviour[] _localTools;
        [SerializeField] float _fallY = -12f;

        public Camera View => _view;
        public int Side { get; private set; } = 1;
        public float FallY => _fallY;
        public Vector3 Spawn(int slot) => _spawns[slot].position;

        void Awake()
        {
            if (NetGame.Current == null || !NetGame.Current.Connected) return;
            foreach (GameObject item in _offline) item.SetActive(false);
            foreach (Behaviour item in _localTools) item.enabled = false;
            NetGame.Current.Enter(this);
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
            _follow.SetTargets(local, opponent);
        }

        void OnDestroy()
        {
            if (NetGame.Current != null) NetGame.Current.Leave(this);
        }
    }
}