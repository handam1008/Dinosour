using UnityEngine;

namespace SSW
{
    public class SandboxCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform _player;
        [SerializeField] Transform _opponent;
        [SerializeField] Vector2 _minimum = new(-27f, -5f);
        [SerializeField] Vector2 _maximum = new(27f, 10f);
        [SerializeField] float _smoothTime = 0.18f;
        [SerializeField] float _minimumSize = 7.2f;
        [SerializeField] float _maximumSize = 12f;
        [SerializeField] float _introSize = 18f;
        [SerializeField] float _introDuration = 2.4f;

        Vector3 _velocity;
        float _sizeVelocity;
        float _introStartedAt;

        public void SetTargets(Transform player, Transform opponent)
        {
            _player = player;
            _opponent = opponent;
        }

        void OnEnable()
        {
            _introStartedAt = Time.unscaledTime;
            Camera camera = GetComponent<Camera>();
            if (camera != null) camera.orthographicSize = _introSize;
        }

        void LateUpdate()
        {
            if (_player == null) return;

            Vector3 opponentPosition = _opponent != null ? _opponent.position : _player.position;
            Vector3 focus = (_player.position + opponentPosition) * 0.5f;

            Vector3 desired = new(
                Mathf.Clamp(focus.x, _minimum.x, _maximum.x),
                Mathf.Clamp(focus.y + 1.5f, _minimum.y, _maximum.y),
                transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime);

            Camera camera = GetComponent<Camera>();
            if (camera == null) return;

            float horizontalSize = Mathf.Abs(_player.position.x - opponentPosition.x) / (2f * camera.aspect) + 4.5f;
            float verticalSize = Mathf.Abs(_player.position.y - opponentPosition.y) * 0.5f + 5.2f;
            float combatSize = Mathf.Clamp(Mathf.Max(horizontalSize, verticalSize), _minimumSize, _maximumSize);
            float introProgress = Mathf.Clamp01((Time.unscaledTime - _introStartedAt) / _introDuration);
            float easedProgress = 1f - Mathf.Pow(1f - introProgress, 3f);
            float desiredSize = Mathf.Lerp(_introSize, combatSize, easedProgress);
            camera.orthographicSize = Mathf.SmoothDamp(camera.orthographicSize, desiredSize, ref _sizeVelocity, 0.16f, Mathf.Infinity, Time.unscaledDeltaTime);
        }
    }
}
