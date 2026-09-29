using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    sealed class AugmentTip
    {
        readonly RectTransform _rect;
        readonly Text _title;
        readonly Text _description;
        Augment _augment;

        public AugmentTip(RectTransform parent)
        {
            _rect = AugmentUI.Create("JobAugmentTooltip", parent);
            AugmentUI.TopRight(_rect);
            Canvas canvas = _rect.gameObject.AddComponent<Canvas>();   
            canvas.overrideSorting = true;                            
            canvas.sortingOrder = 60;     
            Image background = _rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.025f, 0.027f, 0.032f, 0.98f);
            background.raycastTarget = false;
            Outline outline = _rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.005f, 0.006f, 0.008f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform titleRect = AugmentUI.Create("Title", _rect);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(18f, -50f);
            titleRect.offsetMax = new Vector2(-18f, -12f);
            _title = titleRect.gameObject.AddComponent<Text>();
            _title.font = font;
            _title.fontSize = 25;
            _title.fontStyle = FontStyle.Bold;
            _title.alignment = TextAnchor.MiddleLeft;
            _title.color = new Color(0.94f, 0.95f, 0.97f, 1f);
            _title.resizeTextForBestFit = true;
            _title.resizeTextMinSize = 14;
            _title.resizeTextMaxSize = 25;
            _title.raycastTarget = false;

            RectTransform descriptionRect = AugmentUI.Create("Description", _rect);
            AugmentUI.Stretch(descriptionRect);
            descriptionRect.offsetMin = new Vector2(18f, 14f);
            descriptionRect.offsetMax = new Vector2(-18f, -54f);
            _description = descriptionRect.gameObject.AddComponent<Text>();
            _description.font = font;
            _description.fontSize = 19;
            _description.alignment = TextAnchor.UpperLeft;
            _description.color = new Color(0.78f, 0.8f, 0.84f, 1f);
            _description.horizontalOverflow = HorizontalWrapMode.Wrap;
            _description.verticalOverflow = VerticalWrapMode.Truncate;
            _description.resizeTextForBestFit = true;
            _description.resizeTextMinSize = 12;
            _description.resizeTextMaxSize = 19;
            _description.raycastTarget = false;
            Hide();
        }

        public void Show(Augment augment, Vector2 size, float gridHeight)
        {
            _augment = augment;
            _title.text = string.IsNullOrWhiteSpace(augment.displayName) ? augment.name : augment.displayName;
            _description.text = augment.description ?? string.Empty;
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(430f, size.x - 48f));
            float height = Mathf.Min(Mathf.Max(160f, _description.preferredHeight + 68f), size.y - 48f);
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            float top = Mathf.Min(24f + gridHeight + 10f, size.y - height - 24f);
            _rect.anchoredPosition = new Vector2(-24f, -Mathf.Max(24f, top));
            _rect.gameObject.SetActive(true);
            _rect.SetAsLastSibling();
        }

        public void Hide(Augment augment)
        {
            if (_augment == augment) Hide();
        }

        public void Hide()
        {
            _augment = null;
            _rect.gameObject.SetActive(false);
        }
    }
}
