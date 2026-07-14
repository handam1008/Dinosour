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

        MapSelectUI _open;

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
            if (_open != null) return;

            GameObject prefab = Resources.Load<GameObject>(_uiResourceName);
            if (prefab == null) return;

            _open = Instantiate(prefab).GetComponent<MapSelectUI>();
            _open.Show(_maps);
        }
    }
}
