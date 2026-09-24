using UnityEngine;

public class NuclearWave : MonoBehaviour
{
    [SerializeField] MeshFilter _filter;
    [SerializeField] int _segments = 96;
    [SerializeField] float _duration = 0.38f;
    [SerializeField, Range(0f, 1f)] float _thickness = 0.16f;
    [SerializeField] AnimationCurve _expand = new AnimationCurve(new Keyframe(0f, 0.08f), new Keyframe(0.3f, 0.72f), new Keyframe(1f, 1f));
    [SerializeField] Gradient _color = new Gradient();

    Mesh _mesh;
    Vector2[] _directions;
    float[] _reach;
    Vector3[] _vertices;
    Color[] _colors;
    float _radius;
    float _time;

    public void Play(float radius, LayerMask walls)
    {
        _radius = radius;
        _time = 0f;
        _directions = new Vector2[_segments + 1];
        _reach = new float[_segments + 1];

        for (int i = 0; i <= _segments; i++)
        {
            float angle = i * Mathf.PI * 2f / _segments;
            _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _directions[i], radius, walls);
            _reach[i] = hit.collider != null ? hit.distance : radius;
        }

        BuildMesh();
        Apply();
        enabled = true;
    }

    void Update()
    {
        _time += Time.deltaTime;
        Apply();
        if (_time >= _duration) enabled = false;
    }

    void OnDestroy()
    {
        Destroy(_mesh);
    }

    void BuildMesh()
    {
        int count = (_segments + 1) * 2;
        _vertices = new Vector3[count];
        _colors = new Color[count];
        var uv = new Vector2[count];
        var triangles = new int[_segments * 6];

        for (int i = 0; i <= _segments; i++)
        {
            float u = (float)i / _segments;
            uv[i * 2] = new Vector2(u, 0f);
            uv[i * 2 + 1] = new Vector2(u, 1f);
        }

        for (int i = 0; i < _segments; i++)
        {
            int v = i * 2;
            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 1;
            triangles[t + 5] = v + 3;
        }

        _mesh = new Mesh { name = "NuclearWave" };
        _mesh.MarkDynamic();
        _mesh.vertices = _vertices;
        _mesh.uv = uv;
        _mesh.colors = _colors;
        _mesh.triangles = triangles;
        _filter.sharedMesh = _mesh;
    }

    void Apply()
    {
        float t = Mathf.Clamp01(_time / _duration);
        float outer = _expand.Evaluate(t) * _radius;
        float inner = outer * (1f - _thickness);
        Color color = _color.Evaluate(t);

        for (int i = 0; i <= _segments; i++)
        {
            Vector2 direction = _directions[i];
            float reach = _reach[i];
            _vertices[i * 2] = transform.InverseTransformVector(direction * Mathf.Min(inner, reach));
            _vertices[i * 2 + 1] = transform.InverseTransformVector(direction * Mathf.Min(outer, reach));
            _colors[i * 2] = color;
            _colors[i * 2 + 1] = color;
        }

        _mesh.vertices = _vertices;
        _mesh.colors = _colors;
        _mesh.RecalculateBounds();
    }
}
