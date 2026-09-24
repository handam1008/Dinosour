using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    [System.Serializable]
    public class MapEntry
    {
        public string sceneName;
        public string displayName;
    }

    public class MapSelector : MonoBehaviour
    {
        [SerializeField] MapEntry[] _maps;
        [SerializeField] string _uiResourceName = "MapSelectUI";

        [SerializeField] PauseMenu _pausePrefab;
        [SerializeField] PlayerInput _input;

        MapSelectUI _open;
        PauseMenu _pause;

        public bool IsOpen => _open != null;

        void Start()
        {
            _pause = Instantiate(_pausePrefab);
            _pause.Initialize(_input, this);
        }

        public void Bind(PlayerInput input)
        {
            _input = input;
            if (_pause != null) _pause.Initialize(input, this);
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.mKey.wasPressedThisFrame) return;

            if (_open != null)
            {
                _open.Close();
                _open = null;
                return;
            }
            Open();
        }

        public void Open()
        {
            if (_open != null || Time.timeScale <= 0f) return;

            GameObject prefab = Resources.Load<GameObject>(_uiResourceName);
            if (prefab == null) return;

            _open = Instantiate(prefab).GetComponent<MapSelectUI>();
            _open.Show(_maps);
        }
    }
}
