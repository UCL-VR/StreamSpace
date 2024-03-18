Shader "Custom/BlurShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        sampler2D _MainTex;
        float _BlurSize;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            // Apply more pronounced Gaussian Blur
            float2 blurUV = float2(_BlurSize / _ScreenParams.x, _BlurSize / _ScreenParams.y);
            fixed4 blurColor = fixed4(0,0,0,0);
            int blurRange = 10; // Increase this number for a larger blur area
            int sampleCount = (2 * blurRange + 1) * (2 * blurRange + 1);

            for (int x = -blurRange; x <= blurRange; x++)
            {
                for (int y = -blurRange; y <= blurRange; y++)
                {
                    blurColor += tex2D(_MainTex, IN.uv_MainTex + float2(blurUV.x * x, blurUV.y * y)) * _Color;
                }
            }
            blurColor /= sampleCount;

            o.Albedo = blurColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = blurColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
