Shader "Hidden/NHNHackathon/InteractionOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.78, 0.2, 1)
        _OutlinePixels ("Outline Pixels", Range(0.5, 12)) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "StencilMask"
            Cull Off
            ZWrite Off
            ZTest LEqual
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            float4 vert(appdata input) : SV_POSITION
            {
                return UnityObjectToClipPos(input.vertex);
            }

            fixed4 frag() : SV_Target
            {
                return 0;
            }
            ENDCG
        }

        Pass
        {
            Name "Outline"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            fixed4 _OutlineColor;
            float _OutlinePixels;

            float4 vert(appdata input) : SV_POSITION
            {
                float4 clipPosition = UnityObjectToClipPos(input.vertex);
                float4 pivotPosition = UnityObjectToClipPos(float4(0, 0, 0, 1));

                float3 viewNormal = mul(
                    (float3x3)UNITY_MATRIX_IT_MV, input.normal);
                float2 projectedNormal = mul(
                    (float2x2)UNITY_MATRIX_P, viewNormal.xy);
                float2 normalInPixels = projectedNormal * _ScreenParams.xy;

                float2 positionNdc = clipPosition.xy
                    / max(abs(clipPosition.w), 0.0001);
                float2 pivotNdc = pivotPosition.xy
                    / max(abs(pivotPosition.w), 0.0001);
                float2 radialInPixels =
                    (positionNdc - pivotNdc) * _ScreenParams.xy;

                float normalLength = length(normalInPixels);
                float radialLength = length(radialInPixels);
                float2 direction = normalLength > 0.001
                    ? normalInPixels / normalLength
                    : radialInPixels / max(radialLength, 0.001);

                float2 pixelToNdc = 2.0 / _ScreenParams.xy;
                clipPosition.xy += direction * _OutlinePixels
                    * pixelToNdc * abs(clipPosition.w);
                return clipPosition;
            }

            fixed4 frag() : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
