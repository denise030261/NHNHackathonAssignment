Shader "NHNHackathon/Character/Manny2"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Base Color", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1
        _MetallicMap ("Metallic", 2D) = "black" {}
        _MetallicScale ("Metallic Strength", Range(0, 1)) = 1
        _RoughnessMap ("Roughness", 2D) = "white" {}
        _SmoothnessScale ("Smoothness Strength", Range(0, 1)) = 1
        _HeightMap ("Height", 2D) = "black" {}
        _HeightScale ("Height Strength", Range(0, 0.08)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface Surf Standard fullforwardshadows
        #pragma target 3.0

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicMap;
        sampler2D _RoughnessMap;
        sampler2D _HeightMap;

        fixed4 _Color;
        half _BumpScale;
        half _MetallicScale;
        half _SmoothnessScale;
        half _HeightScale;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void Surf(Input input, inout SurfaceOutputStandard output)
        {
            float2 uv = input.uv_MainTex;
            half height = tex2D(_HeightMap, uv).r;
            uv += ParallaxOffset(height, _HeightScale, input.viewDir);

            fixed4 baseColor = tex2D(_MainTex, uv) * _Color;
            output.Albedo = baseColor.rgb;
            output.Alpha = baseColor.a;
            output.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
            output.Metallic = saturate(tex2D(_MetallicMap, uv).r * _MetallicScale);

            half roughness = tex2D(_RoughnessMap, uv).r;
            output.Smoothness = saturate((1.0h - roughness) * _SmoothnessScale);
        }
        ENDCG
    }

    FallBack "Standard"
}
