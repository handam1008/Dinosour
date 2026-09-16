using UnityEngine;

namespace SSW
{
    public sealed class CardSurface : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] Color _paper = new Color(0.93f, 0.91f, 0.84f);
        [SerializeField] Color _edge = new Color(0.32f, 0.38f, 0.42f);
        [SerializeField] float _radius = 14f;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Rounded(mesh, new Rect(rect.x + 5f, rect.y - 11f, rect.width, rect.height), _radius, new Color(0f, 0f, 0f, 0.28f));
            Rounded(mesh, rect, _radius, _edge);
            Rounded(mesh, new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), _radius - 2f, _paper);
        }

        static void Rounded(UnityEngine.UI.VertexHelper mesh, Rect rect, float radius, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(rect.center, tint, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int step = 0; step <= 6; step++)
                {
                    float angle = (corner * 90f + step * 15f) * Mathf.Deg2Rad;
                    mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                }
            }
            for (int i = 0; i < 28; i++) mesh.AddTriangle(start, start + i + 1, start + (i + 1) % 28 + 1);
        }
    }
}
