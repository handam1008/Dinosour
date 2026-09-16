using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class SpawnPoints : MonoBehaviour
    {
        [SerializeField] Transform _first;
        [SerializeField] Transform _second;

        public Vector3 At(int slot) => slot switch
        {
            0 => _first.position,
            1 => _second.position,
            _ => throw new System.ArgumentOutOfRangeException(nameof(slot))
        };

        void OnDrawGizmos()
        {
            Draw(_first, new Color(0.3f, 0.8f, 1f), "Spawn 1");
            Draw(_second, new Color(1f, 0.7f, 0.25f), "Spawn 2");
        }

        static void Draw(Transform point, Color color, string label)
        {
            if (point == null) return;
            Gizmos.color = color;
            Gizmos.DrawWireCube(point.position, new Vector3(0.8f, 1.4f, 0f));
            Gizmos.DrawLine(point.position + Vector3.left * 0.25f, point.position + Vector3.right * 0.25f);
            Gizmos.DrawLine(point.position + Vector3.down * 0.25f, point.position + Vector3.up * 0.25f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(point.position + Vector3.up * 0.9f, label);
#endif
        }
    }
}
