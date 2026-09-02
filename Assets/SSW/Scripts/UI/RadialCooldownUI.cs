using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class RadialCooldownUI : MonoBehaviour
    {
        [SerializeField] Image _overlay;

        ICooldownSource _source;

        public float Progress => _overlay != null ? _overlay.fillAmount : 0f;

        void Awake()
        {
            Configure(_overlay);
        }

        public void Configure(Image overlay)
        {
            _overlay = overlay;
            if (_overlay == null) return;

            _overlay.type = Image.Type.Filled;
            _overlay.fillMethod = Image.FillMethod.Radial360;
            _overlay.fillOrigin = (int)Image.Origin360.Top;
            _overlay.fillClockwise = true;
            _overlay.raycastTarget = false;
            Refresh();
        }

        public void Bind(ICooldownSource source)
        {
            _source = source;
            Refresh();
        }

        public void Refresh()
        {
            if (_overlay == null) return;

            float fraction = 0f;
            if (_source != null
                && _source.TryGetCooldown(out float remaining, out float duration)
                && duration > 0f)
            {
                fraction = Mathf.Clamp01(remaining / duration);
            }

            _overlay.fillAmount = fraction;
            _overlay.enabled = fraction > 0.001f;
        }

        void Update()
        {
            Refresh();
        }
    }
}
