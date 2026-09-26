using System;
using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    sealed class AugmentGrid
    {
        public const float IconSize = 68f;
        const float Spacing = 10f;
        const int Padding = 6;
        readonly RectTransform _viewport;
        readonly GridLayoutGroup _layout;
        readonly ScrollRect _scroll;

        public RectTransform Content { get; }
        public float Height => _viewport.sizeDelta.y;

        public AugmentGrid(RectTransform parent, Action scrolled)
        {
            _viewport = AugmentUI.Create("JobAugmentViewport", parent);
            AugmentUI.TopRight(_viewport);
            _viewport.anchoredPosition = new Vector2(-24f, -24f);
            _viewport.gameObject.AddComponent<RectMask2D>();
            Content = AugmentUI.Create("JobAugmentIconList", _viewport);
            AugmentUI.TopRight(Content);
            _layout = Content.gameObject.AddComponent<GridLayoutGroup>();
            _layout.cellSize = Vector2.one * IconSize;
            _layout.spacing = Vector2.one * Spacing;
            _layout.padding = new RectOffset(Padding, Padding, Padding, Padding);
            _layout.startCorner = GridLayoutGroup.Corner.UpperRight;
            _layout.childAlignment = TextAnchor.UpperRight;
            _layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = _viewport;
            _scroll.content = Content;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.inertia = false;
            _scroll.scrollSensitivity = IconSize + Spacing;
            _scroll.onValueChanged.AddListener(_ => scrolled());
        }

        public void Resize(int count, Vector2 size)
        {
            float width = Mathf.Min(900f, size.x - 48f);
            int columns = Mathf.Max(1, Mathf.Min(count, Mathf.FloorToInt((width - Padding * 2 + Spacing) / (IconSize + Spacing))));
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
            int visibleRows = Mathf.Clamp(Mathf.FloorToInt((size.y * 0.35f - Padding * 2 + Spacing) / (IconSize + Spacing)), 1, 3);
            float height = rows * (IconSize + Spacing) - Spacing + Padding * 2;
            float visibleHeight = Mathf.Min(rows, visibleRows) * (IconSize + Spacing) - Spacing + Padding * 2;
            width = columns * (IconSize + Spacing) - Spacing + Padding * 2;
            _viewport.sizeDelta = new Vector2(width, visibleHeight);
            Content.sizeDelta = new Vector2(width, height);
            _layout.constraintCount = columns;
            _scroll.vertical = height > visibleHeight;
            _scroll.StopMovement();
            Content.anchoredPosition = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(Content);
        }
    }
}
