using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    internal static class MagicianVfxMaterials
    {
        const string ShaderResourcePath = "Shaders/MagicianGlow";
        static readonly int ShapeId = Shader.PropertyToID("_Shape");
        static readonly int GlowId = Shader.PropertyToID("_Glow");
        static readonly Dictionary<MaterialKey, Material> Cache = new();

        internal static Material Get(MagicianGlowShape shape, float glow)
        {
            var key = new MaterialKey(shape, glow);
            if (Cache.TryGetValue(key, out Material material) && material != null)
                return material;

            Shader shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
            {
                Debug.LogError($"Magician glow shader was not found at Resources/{ShaderResourcePath}.");
                return null;
            }

            material = new Material(shader)
            {
                name = $"Magician Glow ({shape}, {glow:0.##})",
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true
            };

            material.SetFloat(ShapeId, (float)shape);
            material.SetFloat(GlowId, Mathf.Max(0f, glow));

            Cache[key] = material;
            return material;
        }

        readonly struct MaterialKey : System.IEquatable<MaterialKey>
        {
            readonly MagicianGlowShape _shape;
            readonly float _glow;

            internal MaterialKey(MagicianGlowShape shape, float glow)
            {
                _shape = shape;
                _glow = glow;
            }

            public bool Equals(MaterialKey other)
            {
                return _shape == other._shape && _glow.Equals(other._glow);
            }

            public override bool Equals(object obj)
            {
                return obj is MaterialKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)_shape * 397) ^ _glow.GetHashCode();
                }
            }
        }
    }
}
