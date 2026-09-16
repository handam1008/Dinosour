Shader "Hidden/MenuBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float2 _Step;

            half4 frag(v2f_img input) : SV_Target
            {
                half4 color = tex2D(_MainTex, input.uv) * 0.227027f;
                color += tex2D(_MainTex, input.uv + _Step * 1.384615f) * 0.316216f;
                color += tex2D(_MainTex, input.uv - _Step * 1.384615f) * 0.316216f;
                color += tex2D(_MainTex, input.uv + _Step * 3.230769f) * 0.070270f;
                color += tex2D(_MainTex, input.uv - _Step * 3.230769f) * 0.070270f;
                color.a = 1;
                return color;
            }
            ENDHLSL
        }
    }
}