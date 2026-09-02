using UnityEngine;

namespace SSW
{
    public interface ICameraShakeReceiver
    {
        void Shake(float strength, float duration);
    }

    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class CameraShakeFeedback : MonoBehaviour, ICameraShakeReceiver
    {
        const float NoiseFrequency = 42f;

        float _remaining;
        float _duration;
        float _strength;
        float _seed;
        Vector3 _lastOffset;

        public static void Play(float strength, float duration)
        {
            if (strength <= 0f || duration <= 0f) return;

            Camera camera = Camera.main;
            if (camera == null) return;

            ICameraShakeReceiver receiver = camera.GetComponent<ICameraShakeReceiver>();
            if (receiver == null)
                receiver = camera.gameObject.AddComponent<CameraShakeFeedback>();

            receiver.Shake(strength, duration);
        }

        public void Shake(float strength, float duration)
        {
            strength = Mathf.Max(0f, strength);
            duration = Mathf.Max(0f, duration);
            if (strength <= 0f || duration <= 0f) return;

            if (_remaining <= 0f)
                _seed = Random.Range(0f, 1000f);

            _strength = Mathf.Max(_strength, strength);
            _duration = Mathf.Max(_duration, duration);
            _remaining = Mathf.Max(_remaining, duration);
        }

        void LateUpdate()
        {
            RemoveLastOffset();
            if (_remaining <= 0f) return;

            _lastOffset = SampleOffset(_seed, _remaining, _duration, _strength);
            transform.position += _lastOffset;

            _remaining = Mathf.Max(0f, _remaining - Time.unscaledDeltaTime);
            if (_remaining <= 0f)
            {
                _duration = 0f;
                _strength = 0f;
            }
        }

        void OnDisable()
        {
            RemoveLastOffset();
            _remaining = 0f;
            _duration = 0f;
            _strength = 0f;
        }

        void RemoveLastOffset()
        {
            if (_lastOffset == Vector3.zero) return;
            transform.position -= _lastOffset;
            _lastOffset = Vector3.zero;
        }

        internal static Vector3 SampleOffset(float seed, float remaining, float duration, float strength)
        {
            float progress = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
            float envelope = progress * progress;
            float time = Time.unscaledTime * NoiseFrequency;
            float x = Mathf.PerlinNoise(seed, time) * 2f - 1f;
            float y = Mathf.PerlinNoise(seed + 31.7f, time) * 2f - 1f;
            return new Vector3(x, y, 0f) * strength * envelope;
        }
    }
}
