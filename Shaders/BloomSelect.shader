Shader "NaiveRP/BloomSelect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Cull Off
        ZWrite On
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _bloomVariance;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                float3 col = tex2D(_MainTex, i.uv);

                col = pow(col, clamp(_bloomVariance, 1, 10));

                return float4(col, 1);
            }
            ENDCG
        }
    }
}
