Shader "SSW/Magician Glow"
{
    Properties
    {
        [NoScaleOffset] _BaseMap ("Texture", 2D) = "white" {}
        [Enum(Textured, 0, Disc, 1, Ring, 2, Diamond, 3, Stripe, 4)] _Shape ("Shape", Float) = 0
        _Glow ("HDR Glow", Range(0, 16)) = 1
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "MagicianGlow"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float _Shape;
                float _Glow;
                float _Softness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float SoftInside(float distanceToEdge, float softness)
            {
                return 1.0 - smoothstep(1.0 - softness, 1.0, distanceToEdge);
            }

            float ShapeMask(float2 uv, float textureAlpha)
            {
                float2 centered = uv * 2.0 - 1.0;
                float softness = max(_Softness, 0.001);

                if (_Shape < 0.5)
                    return textureAlpha * SoftInside(length(centered), softness);

                if (_Shape < 1.5)
                    return SoftInside(length(centered), softness);

                if (_Shape < 2.5)
                {
                    float radius = length(centered);
                    float outer = SoftInside(radius, softness);
                    // A slim bright rim reads much better once Bloom spreads it out.
                    // Keeping the mesh itself thin also stops large Diamond blasts
                    // from covering enemies with a solid coloured doughnut.
                    float inner = smoothstep(0.82 - softness, 0.82 + softness, radius);
                    return outer * inner;
                }

                if (_Shape < 3.5)
                    return SoftInside(abs(centered.x) + abs(centered.y), softness);

                return SoftInside(abs(centered.y), softness);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float mask = ShapeMask(input.uv, textureColor.a);
                float opacity = saturate(input.color.a * mask);
                half3 emission = textureColor.rgb * input.color.rgb * (_Glow * opacity);

                return half4(emission, opacity);
            }
            ENDHLSL
        }
    }
}
