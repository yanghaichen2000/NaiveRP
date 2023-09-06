Shader "NaiveRP/BloomUpSample"
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

            static float gaussianWeightSumInv5x5 = 0.010185220671599025;
            static float gaussianWeight5x5[25] = {
                0.2915024465033185,
                1.3064233284714173,
                2.1539279301900645,
                1.3064233284714173,
                0.2915024465033185,
                1.3064233284714173,
                5.854983152448111,
                9.653235263033789,
                5.854983152448111,
                1.3064233284714173,
                2.1539279301900645,
                9.653235263033789,
                15.915494309239147,
                9.653235263033789,
                2.1539279301900645,
                1.3064233284714173,
                5.854983152448111,
                9.653235263033789,
                5.854983152448111,
                1.3064233284714173,
                0.2915024465033185,
                1.3064233284714173,
                2.1539279301900645,
                1.3064233284714173,
                0.2915024465033185
            };
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _downSampleTexture;

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
                int du, dv;

                float3 colorUpSample = float3(0, 0, 0);
                for (du = -2; du <= 2; du++) {
                    for (dv = -2; dv <= 2; dv++) {
                        float weight = gaussianWeight5x5[(du + 2) * 5 + (dv + 2)];
                        colorUpSample += weight * tex2D(_MainTex, i.uv + float2(du, dv) * _MainTex_TexelSize.xy).xyz;
                    }
                }
                colorUpSample *= gaussianWeightSumInv5x5;

                float3 colorBlurAndAdd = float3(0, 0, 0);
                for (du = -2; du <= 2; du++) {
                    for (dv = -2; dv <= 2; dv++) {
                        float weight = gaussianWeight5x5[(du + 2) * 5 + (dv + 2)];
                        colorBlurAndAdd += weight * tex2D(_downSampleTexture, i.uv + 0.5 * float2(du, dv) * _MainTex_TexelSize.xy).xyz;
                    }
                }
                colorBlurAndAdd *= gaussianWeightSumInv5x5;

                return float4(colorUpSample + colorBlurAndAdd, 1);
            }
            ENDCG
        }
    }
}
