using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    [System.Serializable]
    sealed class HatLayout
    {
        [SerializeField] Matrix4x4 _headToBody = Matrix4x4.identity;
        [SerializeField] Quaternion _rotation = Quaternion.identity;
        [SerializeField] Vector2 _flip = Vector2.one;
        [SerializeField, Min(0.01f)] float _widthRatio = 560f / 340f;

        public void Configure(SpriteRenderer body, SpriteRenderer hat)
        {
            _headToBody = body.transform.worldToLocalMatrix * hat.transform.parent.localToWorldMatrix;
            _rotation = Quaternion.Inverse(body.transform.rotation) * hat.transform.rotation;
            _flip = new Vector2(hat.flipX ? -1f : 1f, hat.flipY ? -1f : 1f);
        }

        public void Apply(Image body, Image hat, JobDefinition definition)
        {
            RectTransform bodyRect = body.rectTransform;
            Sprite bodySprite = body.sprite;
            Vector2 drawn = bodyRect.rect.size;
            if (body.preserveAspect)
            {
                float ratio = bodySprite.rect.width / bodySprite.rect.height;
                if (drawn.x / drawn.y > ratio) drawn.x = drawn.y * ratio;
                else drawn.y = drawn.x / ratio;
            }
            Vector2 origin = bodyRect.rect.min + Vector2.Scale(bodyRect.rect.size - drawn, bodyRect.pivot)
                + Vector2.Scale(drawn, bodySprite.pivot / bodySprite.rect.size);
            Vector2 units = drawn / (bodySprite.rect.size / bodySprite.pixelsPerUnit);
            Vector2 point = _headToBody.MultiplyPoint3x4(definition.WorldOffset);
            Sprite sprite = definition.HatSprite;
            RectTransform rect = hat.rectTransform;
            rect.anchorMin = bodyRect.pivot;
            rect.anchorMax = bodyRect.pivot;
            rect.pivot = sprite.pivot / sprite.rect.size;
            rect.anchoredPosition = origin + Vector2.Scale(point, units);
            float width = drawn.x * _widthRatio;
            rect.sizeDelta = new Vector2(width, width * sprite.rect.height / sprite.rect.width);
            rect.localRotation = _rotation;
            rect.localScale = new Vector3(_flip.x, _flip.y, 1f) * definition.PreviewScale;
        }
    }
}
