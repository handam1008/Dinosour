using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SSW
{
    public sealed class JobAugmentIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        static readonly Color NormalColor = new Color(0.035f, 0.038f, 0.045f, 0.96f);
        static readonly Color HoverColor = new Color(0.095f, 0.1f, 0.115f, 0.98f);

        Augment _augment;
        IAugmentCooldownProvider _cooldownProvider;
        JobAugmentHUD _hud;
        Image _background;
        RadialCooldownUI _cooldownView;

        public Augment Augment => _augment;

        public void Initialize(
            Augment augment,
            IAugmentCooldownProvider cooldownProvider,
            JobAugmentHUD hud,
            Image background,
            RadialCooldownUI cooldownView)
        {
            _augment = augment;
            _cooldownProvider = cooldownProvider;
            _hud = hud;
            _background = background;
            _cooldownView = cooldownView;
            BindCooldownSource();
        }

        public void SetCooldownProvider(IAugmentCooldownProvider cooldownProvider)
        {
            _cooldownProvider = cooldownProvider;
            BindCooldownSource();
        }

        public void RefreshCooldown()
        {
            if (_cooldownView != null) _cooldownView.Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_background != null) _background.color = HoverColor;
            transform.localScale = Vector3.one * 1.06f;
            if (_hud != null) _hud.ShowTooltip(_augment);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_background != null) _background.color = NormalColor;
            transform.localScale = Vector3.one;
            if (_hud != null) _hud.HideTooltip(_augment);
        }

        void OnDisable()
        {
            if (_background != null) _background.color = NormalColor;
            transform.localScale = Vector3.one;
            if (_hud != null) _hud.HideTooltip(_augment);
        }

        void BindCooldownSource()
        {
            if (_cooldownView == null) return;

            ICooldownSource source = _cooldownProvider != null && _augment != null
                ? new AugmentCooldownSource(_cooldownProvider, _augment)
                : null;
            _cooldownView.Bind(source);
        }

        sealed class AugmentCooldownSource : ICooldownSource
        {
            readonly IAugmentCooldownProvider _provider;
            readonly Augment _augment;

            public AugmentCooldownSource(IAugmentCooldownProvider provider, Augment augment)
            {
                _provider = provider;
                _augment = augment;
            }

            public bool TryGetCooldown(out float remaining, out float duration)
            {
                return _provider.TryGetCooldown(_augment, out remaining, out duration);
            }
        }
    }
}
