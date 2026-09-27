using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class JobAugmentHUD : MonoBehaviour
    {
        readonly Dictionary<Augment, JobAugmentIconUI> _icons = new Dictionary<Augment, JobAugmentIconUI>();
        IAugmentSource _source;
        IAugmentCooldownProvider _cooldownProvider;
        Canvas _canvas;
        RectTransform _canvasRect;
        AugmentGrid _grid;
        AugmentTip _tip;
        Vector2 _size;
        bool _jobVisible = true;
        JobAugmentHUD _above;
        Text _heading;

        public int IconCount => _icons.Count;
        public float Height => _icons.Count > 0 && _grid != null ? _grid.Height : 0f;
        public event System.Action LayoutChanged;
        float Top => _above != null ? 24f + _above.Height + 34f : 24f;
        IAugmentCooldownProvider Cooldowns => _above == null ? _cooldownProvider : null;

        public void PlaceBelow(JobAugmentHUD above)
        {
            if (_above == above) return;
            if (_above != null) _above.LayoutChanged -= Resize;
            _above = above;
            if (_above != null) _above.LayoutChanged += Resize;
            foreach (JobAugmentIconUI icon in _icons.Values) icon.SetCooldownProvider(Cooldowns);
            Resize();
        }

        public void Bind(IAugmentSource source)
        {
            Unbind();
            _source = source;
            if (isActiveAndEnabled) Subscribe();
        }

        public void Unbind()
        {
            if (_source != null) _source.AugmentGranted -= AddAugment;
            _source = null;
            PlaceBelow(null);
            Clear();
        }

        void OnEnable()
        {
            if (_source != null) Subscribe();
            else RefreshVisibility();
        }

        void OnDisable()
        {
            if (_source != null) _source.AugmentGranted -= AddAugment;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            _tip?.Hide();
        }

        void Subscribe()
        {
            _source.AugmentGranted += AddAugment;
            Clear();
            foreach (Augment augment in _source.Owned) AddAugment(augment);
        }

        void Clear()
        {
            _tip?.Hide();
            foreach (JobAugmentIconUI icon in _icons.Values)
            {
                icon.gameObject.SetActive(false);
                icon.transform.SetParent(null, false);
                Destroy(icon.gameObject);
            }
            _icons.Clear();
            RefreshVisibility();
            Resize();
        }

        public void Configure(IAugmentCooldownProvider cooldownProvider)
        {
            _cooldownProvider = cooldownProvider;
            foreach (JobAugmentIconUI icon in _icons.Values)
                icon.SetCooldownProvider(Cooldowns);
        }

        public void AddAugment(Augment augment)
        {
            if (augment == null || _icons.ContainsKey(augment)) return;
            BuildIfNeeded();
            _icons.Add(augment, CreateIcon(augment));
            RefreshVisibility();
            Resize();
        }

        public void SetJobVisible(bool visible)
        {
            _jobVisible = visible;
            RefreshVisibility();
        }

        void RefreshVisibility()
        {
            if (_canvas == null) return;
            bool visible = isActiveAndEnabled && _icons.Count > 0 && (_source != null || _jobVisible);
            _canvas.gameObject.SetActive(visible);
            if (!visible) _tip.Hide();
        }

        internal void ShowTooltip(Augment augment)
        {
            if (_icons.ContainsKey(augment)) _tip.Show(augment, _canvasRect.rect.size, Top - 24f + _grid.Height);
        }

        internal void HideTooltip(Augment augment) => _tip?.Hide(augment);

        void LateUpdate()
        {
            if (_canvas != null && _canvas.gameObject.activeSelf && _canvasRect.rect.size != _size) Resize();
        }

        void Resize()
        {
            if (_canvas == null) return;
            _size = _canvasRect.rect.size;
            _grid.Resize(_icons.Count, _size, Top);
            _heading.gameObject.SetActive(_above != null);
            _heading.rectTransform.anchoredPosition = new Vector2(-30f, -Top + 26f);
            _tip.Hide();
            LayoutChanged?.Invoke();
        }

        void BuildIfNeeded()
        {
            if (_canvas != null) return;
            EnsureEventSystem();
            _canvasRect = AugmentUI.Create("JobAugmentHUDCanvas", transform);
            _canvas = _canvasRect.gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 50;
            CanvasScaler scaler = _canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            AugmentUI.Stretch(_canvasRect);
            _tip = new AugmentTip(_canvasRect);
            _grid = new AugmentGrid(_canvasRect, _tip.Hide);
            RectTransform heading = AugmentUI.Create("OpponentAugments", _canvasRect);
            AugmentUI.TopRight(heading);
            heading.sizeDelta = new Vector2(200f, 24f);
            _heading = heading.gameObject.AddComponent<Text>();
            _heading.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _heading.fontSize = 20;
            _heading.alignment = TextAnchor.MiddleRight;
            _heading.color = new Color(0.75f, 0.78f, 0.86f);
            _heading.raycastTarget = false;
            _heading.text = "상대 증강";
        }

        JobAugmentIconUI CreateIcon(Augment augment)
        {
            RectTransform iconRect = AugmentUI.Create("JobAugment_" + augment.name, _grid.Content);
            iconRect.sizeDelta = Vector2.one * AugmentGrid.IconSize;
            Image background = iconRect.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.038f, 0.045f, 0.96f);
            Outline outline = iconRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.008f, 0.009f, 0.012f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            RectTransform artRect = AugmentUI.Create("Icon", iconRect);
            AugmentUI.Stretch(artRect);
            artRect.offsetMin = new Vector2(6f, 6f);
            artRect.offsetMax = new Vector2(-6f, -6f);
            Image art = artRect.gameObject.AddComponent<Image>();
            art.sprite = augment.icon;
            art.enabled = augment.icon != null;
            art.preserveAspect = true;
            art.raycastTarget = false;

            RectTransform cooldownRect = AugmentUI.Create("CooldownOverlay", artRect);
            AugmentUI.Stretch(cooldownRect);
            Image cooldown = cooldownRect.gameObject.AddComponent<Image>();
            cooldown.sprite = augment.icon;
            cooldown.color = new Color(0f, 0f, 0f, 0.78f);
            cooldown.preserveAspect = true;
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = true;
            cooldown.fillAmount = 0f;
            cooldown.raycastTarget = false;
            cooldown.enabled = false;
            RadialCooldownUI cooldownView = cooldownRect.gameObject.AddComponent<RadialCooldownUI>();
            cooldownView.Configure(cooldown);
            JobAugmentIconUI icon = iconRect.gameObject.AddComponent<JobAugmentIconUI>();
            icon.Initialize(augment, Cooldowns, this, background, cooldownView);
            return icon;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
    }
}
