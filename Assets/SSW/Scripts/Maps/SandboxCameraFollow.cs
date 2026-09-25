using Unity.Cinemachine;
using UnityEngine;

namespace SSW
{
    public class SandboxCameraFollow : MonoBehaviour, ICameraShakeReceiver
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
        float _shakeRemaining;
        float _shakeDuration;
        float _shakeStrength;
        float _shakeSeed;
        Vector3 _shakeOffset;

        public void SetTargets(Transform player, Transform opponent)
        {
            _player = player;
            _opponent = opponent;
        }

        public void SetBounds(Bounds bounds)
        {
            RemoveShakeOffset();
            _minimum = bounds.min;
            _maximum = bounds.max;
            CinemachineCamera camera = GetComponent<CinemachineCamera>();
            _maximumSize = Mathf.Max(_minimumSize, bounds.extents.y + 1f);
            _introSize = Mathf.Max(_maximumSize, (bounds.extents.x + 1f) / Camera.main.aspect);
            _introStartedAt = Time.unscaledTime;
            _velocity = Vector3.zero;
            _sizeVelocity = 0f;
            transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
            camera.Lens.OrthographicSize = _introSize;
        }

        void OnEnable()
        {
            _introStartedAt = Time.unscaledTime;
            Camera camera = GetComponent<Camera>();
            if (camera != null) camera.orthographicSize = _introSize;
        }

        void LateUpdate()
        {
            RemoveShakeOffset();

            if (_player != null)
                UpdateFollowPosition();

            ApplyShake();
        }

        public void Shake(float strength, float duration)
        {
            strength = Mathf.Max(0f, strength);
            duration = Mathf.Max(0f, duration);
            if (strength <= 0f || duration <= 0f) return;

            if (_shakeRemaining <= 0f)
                _shakeSeed = Random.Range(0f, 1000f);

            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _shakeRemaining = Mathf.Max(_shakeRemaining, duration);
        }

        void UpdateFollowPosition()
        {

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

        void ApplyShake()
        {
            if (_shakeRemaining <= 0f) return;

            _shakeOffset = CameraShakeFeedback.SampleOffset(
                _shakeSeed,
                _shakeRemaining,
                _shakeDuration,
                _shakeStrength);
            transform.position += _shakeOffset;

            _shakeRemaining = Mathf.Max(0f, _shakeRemaining - Time.unscaledDeltaTime);
            if (_shakeRemaining <= 0f)
            {
                _shakeDuration = 0f;
                _shakeStrength = 0f;
            }
        }

        void OnDisable()
        {
            RemoveShakeOffset();
            _shakeRemaining = 0f;
            _shakeDuration = 0f;
            _shakeStrength = 0f;
        }

        void RemoveShakeOffset()
        {
            if (_shakeOffset == Vector3.zero) return;
            transform.position -= _shakeOffset;
            _shakeOffset = Vector3.zero;
        }
    }
}
