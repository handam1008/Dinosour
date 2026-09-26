using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(10001)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class SandboxCameraFollow : MonoBehaviour, ICameraShakeReceiver
    {
        [SerializeField] Vector2 _minimum = new(-27f, -5f);
        [SerializeField] Vector2 _maximum = new(27f, 10f);
        [SerializeField, Min(0.1f)] float _size = 9f;

        Camera _camera;
        float _depth;
        int _side = 1;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _depth = Mathf.Max(0.1f, Mathf.Abs(transform.position.z));
        }

        void OnEnable() => Frame();

        public void SetBounds(Bounds bounds)
        {
            _minimum = bounds.min;
            _maximum = bounds.max;
            Frame();
        }

        public void SetSide(int side)
        {
            _side = side < 0 ? -1 : 1;
            Frame();
        }

        public void Shake(float strength, float duration) { }

        void LateUpdate() => Frame();

        void Frame()
        {
            Vector2 center = (_minimum + _maximum) * 0.5f;
            transform.SetPositionAndRotation(
                new Vector3(center.x, center.y, -_depth * _side),
                Quaternion.Euler(0f, _side < 0 ? 180f : 0f, 0f));
            _camera.orthographic = true;
            _camera.orthographicSize = _size;
        }
    }
}
