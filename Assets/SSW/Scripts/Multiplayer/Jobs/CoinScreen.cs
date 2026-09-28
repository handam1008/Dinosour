using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    public sealed class CoinScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup _death;
        [SerializeField] float _deathScale = 1.3f;
        [SerializeField] float _deathAlpha = 0.65f;
        [SerializeField] float _deathIn = 0.08f;
        [SerializeField] float _deathHold = 0.3f;
        [SerializeField] float _deathOut = 0.4f;
        [SerializeField] Image _overlay;
        [SerializeField] float _overlayLow = 0.15f;
        [SerializeField] float _overlayHigh = 0.75f;
        [SerializeField] float _overlayPeriod = 0.3f;
        [SerializeField] JackpotUI _digits;
        [SerializeField] Transform _rig;
        [SerializeField] ParticleSystem _money;
        [SerializeField] GameObject[] _objects;
        Camera _camera;
        float _deathAt = float.NegativeInfinity;
        float _jackpotAt;
        bool _jackpot;
        bool _closing;

        public float DeathAlpha => _death.alpha;
        public float OverlayAlpha => _overlay.color.a;

        public void Open(Camera camera) => _camera = camera;

        public void Death(float age)
        {
            _deathAt = Time.unscaledTime - age;
            DrawDeath();
        }

        public void Jackpot(bool active)
        {
            if (_jackpot == active) return;
            _jackpot = active;
            _overlay.gameObject.SetActive(active);
            foreach (GameObject item in _objects) item.SetActive(active);
            if (active)
            {
                _jackpotAt = Time.unscaledTime;
                PlaceRig();
                _money.Play(true);
                _digits.gameObject.SetActive(true);
                _digits.Play();
            }
            else
            {
                _money.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _digits.gameObject.SetActive(false);
                Color color = _overlay.color;
                color.a = 0f;
                _overlay.color = color;
            }
        }

        void LateUpdate()
        {
            DrawDeath();
            if (_closing && Time.unscaledTime >= _deathAt + _deathIn + _deathHold + _deathOut) { Destroy(gameObject); return; }
            if (!_jackpot) return;
            PlaceRig();
            float phase = Mathf.PingPong((Time.unscaledTime - _jackpotAt) / _overlayPeriod, 1f);
            Color tint = _overlay.color;
            tint.a = Mathf.Lerp(_overlayLow, _overlayHigh, phase);
            _overlay.color = tint;
        }

        void PlaceRig()
        {
            if (_camera != null)
            {
                _rig.SetPositionAndRotation(_camera.transform.position, _camera.transform.rotation);
                _rig.localScale = Vector3.one * (_camera.orthographicSize / 9f);
            }
        }

        void DrawDeath()
        {
            float age = Time.unscaledTime - _deathAt;
            float alpha;
            float scale;
            if (age < _deathIn)
            {
                float t = Mathf.Clamp01(age / _deathIn);
                alpha = t * _deathAlpha;
                scale = Mathf.Lerp(1.55f, 1f, t);
            }
            else
            {
                float t = Mathf.Clamp01((age - _deathIn - _deathHold) / _deathOut);
                alpha = _deathAlpha * (1f - t);
                scale = Mathf.Lerp(1f, 1.12f, t);
            }
            _death.alpha = alpha;
            _death.transform.localScale = Vector3.one * (_deathScale * scale);
        }

        public void Close()
        {
            Jackpot(false);
            _closing = true;
        }
    }
}
