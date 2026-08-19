Shader "Hidden/NHNHackathon/ScreenBrightness"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(0.6, 1.4)) = 1
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _Brightness;

            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                half inverseGamma = rcp(max(_Brightness, 0.001h));
                color.rgb = pow(max(color.rgb, 0.0h), inverseGamma);
                return color;
            }
            ENDCG
        }
    }
}
