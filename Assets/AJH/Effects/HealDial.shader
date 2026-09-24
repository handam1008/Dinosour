Shader "AJH/Heal Dial"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _FillColor ("Fill Color", Color) = (0.55, 0.85, 0.3, 0.3)
        _LineColor ("Line Color", Color) = (0.78, 0.95, 0.25, 0.9)
        _RimAlpha ("Rim Alpha", Range(0, 1)) = 0.45
        _TipColor ("Tip Color", Color) = (1, 0.95, 0.3, 1)
        _LineWidth ("Line Width", Range(0.001, 0.1)) = 0.018
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
            Blend SrcAlpha OneMinusSrcAlpha
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                float4 _FillColor;
                float4 _LineColor;
                float _RimAlpha;
                float4 _TipColor;
                float _LineWidth;
            CBUFFER_END

            static const float Radius = 0.9;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Stroke(float distance, float width, float aa)
            {
                return 1.0 - smoothstep(width - aa, width + aa, distance);
            }

            float Spoke(float2 p, float angle, float width, float aa)
            {
                float2 direction = float2(sin(angle), cos(angle));
                float along = dot(p, direction);
                float across = abs(p.x * direction.y - p.y * direction.x);
                return step(0.0, along) * step(along, Radius) * Stroke(across, width, aa);
            }

            float4 Over(float4 below, float3 color, float alpha)
            {
                float outAlpha = alpha + below.a * (1.0 - alpha);
                float3 outColor = (color * alpha + below.rgb * below.a * (1.0 - alpha)) / max(outAlpha, 1e-4);
                return float4(outColor, outAlpha);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float r = length(p);
                float aa = max(fwidth(r), 1e-4);
                float width = max(_LineWidth, aa);
                float turn = frac(atan2(p.x, p.y) / TWO_PI + 1.0);
                float filled = step(turn, _Progress) * step(0.001, _Progress);
                float inside = 1.0 - smoothstep(Radius - aa, Radius + aa, r);
                float tipAngle = _Progress * TWO_PI;

                float4 color = float4(_FillColor.rgb, _FillColor.a * inside * filled);

                float rim = Stroke(abs(r - Radius), width, aa) * lerp(_RimAlpha, 1.0, filled);
                float spokes = max(Spoke(p, 0.0, width, aa), Spoke(p, tipAngle, width, aa) * step(0.001, _Progress));
                color = Over(color, _LineColor.rgb, _LineColor.a * max(rim, spokes));

                float2 q = abs(p - float2(sin(tipAngle), cos(tipAngle)) * Radius);
                float arm = 0.07;
                float thick = width * 1.4;
                float plus = max(Stroke(q.x, thick, aa) * Stroke(q.y, arm, aa), Stroke(q.y, thick, aa) * Stroke(q.x, arm, aa));
                color = Over(color, _TipColor.rgb, _TipColor.a * plus * step(0.001, _Progress));

                return half4(color.rgb, color.a);
            }
            ENDHLSL
        }
    }
}
