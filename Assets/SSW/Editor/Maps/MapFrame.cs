using System;
using UnityEditor;
using UnityEngine;

namespace SSW
{
    public static class MapFrame
    {
        public static Bounds Read(MapEdges edges)
        {
            using var settings = new SerializedObject(edges);
            var entries = settings.FindProperty("_edges");
            if (entries.arraySize != 4) throw new InvalidOperationException(edges.name + ": 맵 경계가 네 개여야 합니다.");
            Bounds bounds = default;
            bool first = true;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var shape = entries.GetArrayElementAtIndex(i).FindPropertyRelative("Shape").objectReferenceValue as BoxCollider2D;
                if (shape == null) throw new InvalidOperationException(edges.name + ": 맵 경계 상자 참조가 없습니다.");
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                    {
                        Vector3 point = shape.transform.TransformPoint(shape.offset + Vector2.Scale(shape.size * 0.5f, new Vector2(x, y)));
                        if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                        else bounds.Encapsulate(point);
                    }
            }
            return bounds;
        }
    }
}
