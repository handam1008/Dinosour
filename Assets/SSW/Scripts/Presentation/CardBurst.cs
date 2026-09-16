using UnityEngine;

namespace SSW
{
    public sealed class CardBurst : UnityEngine.UI.MaskableGraphic
    {
        Vector2 _origin;
        float _age = 1f;

        public void Play(RectTransform card)
        {
            _origin = rectTransform.InverseTransformPoint(card.position);
            _age = 0f;
            SetVerticesDirty();
        }

        void Update()
        {
            if (_age >= 0.55f) return;
            _age += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            float t = Mathf.Clamp01(_age / 0.55f);
            if (t >= 1f) return;
            for (int i = 0; i < 14; i++)
            {
                float angle = (i * 360f / 14f + 8f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 normal = new Vector2(-dir.y, dir.x) * (1f-t) * 2.3f;
                Vector2 origin = _origin + Vector2.Scale(dir, new Vector2(165f, 228f));
                Vector2 a = origin + dir * t * 160f;
                Vector2 b = a + dir * (1f-t) * 48f;
                Color tint = new Color(0.86f,0.95f,1f,(1f-t)*(1f-t));
                int start = mesh.currentVertCount;
                mesh.AddVert(a+normal,tint,Vector2.zero); mesh.AddVert(b,tint,Vector2.zero);
                mesh.AddVert(a-normal,tint,Vector2.zero); mesh.AddTriangle(start,start+1,start+2);
            }
        }
    }
}
