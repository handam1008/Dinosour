using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SSW
{
    /// <summary>
    /// Creates a small list for owned job augments in the top-right corner.
    /// The HUD only handles presentation; each job provides its own cooldown state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JobAugmentHUD : MonoBehaviour
    {
        const float IconSize = 68f;
        const float IconSpacing = 10f;

        readonly Dictionary<Augment, JobAugmentIconUI> _icons = new Dictionary<Augment, JobAugmentIconUI>();

        IAugmentCooldownProvider _cooldownProvider;
        Canvas _canvas;
        RectTransform _iconList;
        GameObject _tooltip;
        RectTransform _tooltipRect;
        Text _tooltipTitle;
        Text _tooltipDescription;
        Augment _hoveredAugment;
        bool _jobVisible = true;

        public int IconCount => _icons.Count;

        public void Configure(IAugmentCooldownProvider cooldownProvider)
        {
            _cooldownProvider = cooldownProvider;
            BuildIfNeeded();

            foreach (JobAugmentIconUI icon in _icons.Values)
                icon.SetCooldownProvider(_cooldownProvider);
        }

        public void AddAugment(Augment augment)
        {
            if (augment == null || augment is not IJobRestrictedAugment || _icons.ContainsKey(augment))
                return;

            BuildIfNeeded();
            JobAugmentIconUI icon = CreateIcon(augment);
            _icons.Add(augment, icon);
            _canvas.gameObject.SetActive(_jobVisible);
        }

        public void SetJobVisible(bool visible)
        {
            _jobVisible = visible;
            if (_canvas != null)
                _canvas.gameObject.SetActive(visible && _icons.Count > 0);

            if (!visible)
                HideTooltip(_hoveredAugment);
        }

        internal void ShowTooltip(Augment augment)
        {
            if (augment == null || _tooltip == null) return;

            _hoveredAugment = augment;
            _tooltipTitle.text = string.IsNullOrWhiteSpace(augment.displayName)
                ? augment.name
                : augment.displayName;
            _tooltipDescription.text = augment.description ?? string.Empty;
            _tooltip.SetActive(true);
            float tooltipHeight = Mathf.Max(160f, _tooltipDescription.preferredHeight + 68f);
            _tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);
            _tooltip.transform.SetAsLastSibling();
        }

        internal void HideTooltip(Augment augment)
        {
            if (_hoveredAugment != augment) return;

            _hoveredAugment = null;
            if (_tooltip != null)
                _tooltip.SetActive(false);
        }

        void BuildIfNeeded()
        {
            if (_canvas != null) return;

            EnsureEventSystem();

            GameObject canvasObject = CreateUIObject("JobAugmentHUDCanvas", transform);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = (RectTransform)canvasObject.transform;
            Stretch(canvasRect);

            GameObject listObject = CreateUIObject("JobAugmentIconList", canvasRect);
            _iconList = (RectTransform)listObject.transform;
            _iconList.anchorMin = Vector2.one;
            _iconList.anchorMax = Vector2.one;
            _iconList.pivot = Vector2.one;
            _iconList.anchoredPosition = new Vector2(-24f, -24f);
            _iconList.sizeDelta = new Vector2(900f, IconSize);

            HorizontalLayoutGroup layout = listObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = IconSpacing;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateTooltip(canvasRect);
            canvasObject.SetActive(false);
        }

        JobAugmentIconUI CreateIcon(Augment augment)
        {
            GameObject iconObject = CreateUIObject("JobAugment_" + augment.name, _iconList);
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);

            Image background = iconObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.038f, 0.045f, 0.96f);

            Outline outline = iconObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.008f, 0.009f, 0.012f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            GameObject artObject = CreateUIObject("Icon", iconRect);
            RectTransform artRect = (RectTransform)artObject.transform;
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = new Vector2(6f, 6f);
            artRect.offsetMax = new Vector2(-6f, -6f);

            Image art = artObject.AddComponent<Image>();
            art.sprite = augment.icon;
            art.enabled = augment.icon != null;
            art.preserveAspect = true;
            art.raycastTarget = false;

            GameObject cooldownObject = CreateUIObject("CooldownOverlay", artRect);
            RectTransform cooldownRect = (RectTransform)cooldownObject.transform;
            Stretch(cooldownRect);

            Image cooldown = cooldownObject.AddComponent<Image>();
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

            RadialCooldownUI cooldownView = cooldownObject.AddComponent<RadialCooldownUI>();
            cooldownView.Configure(cooldown);

            JobAugmentIconUI icon = iconObject.AddComponent<JobAugmentIconUI>();
            icon.Initialize(augment, _cooldownProvider, this, background, cooldownView);
            return icon;
        }

        void CreateTooltip(RectTransform canvasRect)
        {
            _tooltip = CreateUIObject("JobAugmentTooltip", canvasRect);
            _tooltipRect = (RectTransform)_tooltip.transform;
            _tooltipRect.anchorMin = Vector2.one;
            _tooltipRect.anchorMax = Vector2.one;
            _tooltipRect.pivot = Vector2.one;
            _tooltipRect.anchoredPosition = new Vector2(-24f, -106f);
            _tooltipRect.sizeDelta = new Vector2(430f, 160f);

            Image background = _tooltip.AddComponent<Image>();
            background.color = new Color(0.025f, 0.027f, 0.032f, 0.98f);
            background.raycastTarget = false;

            Outline outline = _tooltip.AddComponent<Outline>();
            outline.effectColor = new Color(0.005f, 0.006f, 0.008f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject titleObject = CreateUIObject("Title", _tooltipRect);
            RectTransform titleRect = (RectTransform)titleObject.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(18f, -50f);
            titleRect.offsetMax = new Vector2(-18f, -12f);

            _tooltipTitle = titleObject.AddComponent<Text>();
            _tooltipTitle.font = font;
            _tooltipTitle.fontSize = 25;
            _tooltipTitle.fontStyle = FontStyle.Bold;
            _tooltipTitle.alignment = TextAnchor.MiddleLeft;
            _tooltipTitle.color = new Color(0.94f, 0.95f, 0.97f, 1f);
            _tooltipTitle.raycastTarget = false;

            GameObject descriptionObject = CreateUIObject("Description", _tooltipRect);
            RectTransform descriptionRect = (RectTransform)descriptionObject.transform;
            descriptionRect.anchorMin = Vector2.zero;
            descriptionRect.anchorMax = Vector2.one;
            descriptionRect.offsetMin = new Vector2(18f, 14f);
            descriptionRect.offsetMax = new Vector2(-18f, -54f);

            _tooltipDescription = descriptionObject.AddComponent<Text>();
            _tooltipDescription.font = font;
            _tooltipDescription.fontSize = 19;
            _tooltipDescription.alignment = TextAnchor.UpperLeft;
            _tooltipDescription.color = new Color(0.78f, 0.8f, 0.84f, 1f);
            _tooltipDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipDescription.verticalOverflow = VerticalWrapMode.Truncate;
            _tooltipDescription.raycastTarget = false;

            _tooltip.SetActive(false);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.layer = 5;
            child.transform.SetParent(parent, false);
            return child;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
