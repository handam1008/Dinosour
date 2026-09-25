Shader "UI/VisionMaskOpaque"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        // 중심 위치
        _Center ("Center", Vector) = (0.5, 0.5, 1.777, 0)

        // 시야 크기
        _Radius ("Radius", Range(0.01, 1.0)) = 0.23

        // 경계 부드러움
        _Softness ("Softness", Range(0.001, 0.2)) = 0.02

        // 바깥쪽 색상
        _Color ("Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Output
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Center;
            float _Radius;
            float _Softness;
            fixed4 _Color;

            Output vert(Input v)
            {
                Output o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(Output i) : SV_Target
            {
                float2 delta = i.uv - _Center.xy;

                // 화면 비율 보정
                delta.x *= _Center.z;

                float distance = length(delta);

                // 중심 = 0
                // 바깥 = 1
                float alpha = smoothstep(
                    _Radius - _Softness,
                    _Radius,
                    distance
                );

                return fixed4(
                    _Color.rgb,
                    alpha * _Color.a
                );
            }

            ENDCG
        }
    }
}