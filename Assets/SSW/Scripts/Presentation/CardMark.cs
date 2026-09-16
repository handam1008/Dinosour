using UnityEngine;

namespace SSW
{
    public sealed class CardMark : UnityEngine.UI.MaskableGraphic
    {
        int _kind;
        Color _ink;

        public void Show(Augment augment)
        {
            if (augment is CommonAugment common)
                _kind = common.type == CommonAugmentType.Giant ? 0 : common.type == CommonAugmentType.Vampire ? 1 :
                    common.type == CommonAugmentType.Berserker ? 2 : common.type == CommonAugmentType.Confidence ? 3 :
                    common.type == CommonAugmentType.GlassCannon ? 4 : 5;
            else _kind = augment is IJobRestrictedAugment job && job.RequiredJob == PlayerJob.Witch ? 6 : 7;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            _ink = color;
            if (_kind == 0)
            {
                Box(mesh, -27, -32, 18, 46); Box(mesh, 9, -32, 18, 46); Box(mesh, -34, 2, 68, 28); Box(mesh, -17, 36, 34, 24);
            }
            else if (_kind == 1)
            {
                Disc(mesh, new Vector2(-18, 15), 23); Disc(mesh, new Vector2(18, 15), 23);
                Triangle(mesh, new Vector2(-39, 10), new Vector2(39, 10), new Vector2(0, -42));
            }
            else if (_kind == 2)
            {
                Triangle(mesh, new Vector2(12, 53), new Vector2(-33, -6), new Vector2(18, -6));
                Triangle(mesh, new Vector2(-12, -52), new Vector2(33, 9), new Vector2(-18, 9));
            }
            else if (_kind == 3)
            {
                Box(mesh, -34, -24, 68, 13); Triangle(mesh, new Vector2(-34, -11), new Vector2(-43, 31), new Vector2(-8, 3));
                Triangle(mesh, new Vector2(-24, -11), new Vector2(0, 42), new Vector2(24, -11));
                Triangle(mesh, new Vector2(8, 3), new Vector2(43, 31), new Vector2(34, -11));
            }
            else if (_kind == 4)
            {
                Triangle(mesh, new Vector2(0, 48), new Vector2(-32, 8), new Vector2(-4, 3));
                Triangle(mesh, new Vector2(7, 0), new Vector2(34, 8), new Vector2(0, -43));
                Triangle(mesh, new Vector2(-32, 0), new Vector2(-6, -4), new Vector2(-2, -43));
                Triangle(mesh, new Vector2(5, 46), new Vector2(34, 15), new Vector2(8, 8));
            }
            else if (_kind == 5)
            {
                for (int i = 0; i < 20; i++)
                {
                    float a = (30f + i * 14f) * Mathf.Deg2Rad;
                    float b = (30f + (i + 1) * 14f) * Mathf.Deg2Rad;
                    Line(mesh, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 33, new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 33, 9);
                }
                Triangle(mesh, new Vector2(22, 34), new Vector2(46, 32), new Vector2(32, 10));
                Line(mesh, Vector2.zero, new Vector2(0, 21), 6); Line(mesh, Vector2.zero, new Vector2(14, -7), 6);
            }
            else if (_kind == 6)
            {
                Disc(mesh, new Vector2(0, -13), 32); Box(mesh, -12, 4, 24, 32); Box(mesh, -18, 32, 36, 9);
                Triangle(mesh, new Vector2(39, 20), new Vector2(44, 31), new Vector2(49, 20));
            }
            else
            {
                Box(mesh, -30, -40, 60, 80);
                _ink = new Color(0.93f, 0.91f, 0.84f);
                Triangle(mesh, new Vector2(0, 23), new Vector2(-16, 0), new Vector2(16, 0));
                Triangle(mesh, new Vector2(-16, 0), new Vector2(16, 0), new Vector2(0, -23));
            }
        }

        void Box(UnityEngine.UI.VertexHelper mesh, float x, float y, float w, float h)
        {
            Triangle(mesh, new Vector2(x,y), new Vector2(x+w,y), new Vector2(x+w,y+h));
            Triangle(mesh, new Vector2(x,y), new Vector2(x+w,y+h), new Vector2(x,y+h));
        }

        void Line(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float width)
        {
            Vector2 side = new Vector2(-(b-a).y, (b-a).x).normalized * width * 0.5f;
            Triangle(mesh, a+side, b+side, b-side); Triangle(mesh, a+side, b-side, a-side);
        }

        void Disc(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius)
        {
            for (int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12f; float b=(i+1)*Mathf.PI/12f;
                Triangle(mesh, center, center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius, center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);
            }
        }

        void Triangle(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(a,_ink,Vector2.zero); mesh.AddVert(b,_ink,Vector2.zero); mesh.AddVert(c,_ink,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);
        }
    }
}
