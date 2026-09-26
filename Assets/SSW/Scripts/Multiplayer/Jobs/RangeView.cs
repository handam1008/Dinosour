using UnityEngine;

namespace SSW
{
    internal sealed class RangeView
    {
        GameObject _instance;
        public bool Visible => _instance != null && _instance.activeInHierarchy;

        public void Show(bool active, GameObject prefab, Transform owner, float radius, bool follow)
        {
            if (!active)
            {
                Clear();
                return;
            }
            if (_instance != null || prefab == null) return;
            _instance = Object.Instantiate(prefab, owner.position, Quaternion.identity);
            _instance.transform.localScale = Vector3.one * (radius * 2f);
            if (follow) _instance.transform.SetParent(owner, true);
        }

        public void Clear()
        {
            if (_instance == null) return;
            Object.Destroy(_instance);
            _instance = null;
        }
    }
}
