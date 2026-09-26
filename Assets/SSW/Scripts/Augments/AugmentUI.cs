using UnityEngine;

namespace SSW
{
    static class AugmentUI
    {
        public static RectTransform Create(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.layer = 5;
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void TopRight(RectTransform rect)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
        }
    }
}
