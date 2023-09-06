Shader "NaiveRP/SSRpass4"
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

            static float2 poissonDiskL16D2[16] = {
                float2(-0.94201624, -0.39906216),
                float2(0.94558609, -0.76890725),
                float2(-0.094184101, -0.92938870),
                float2(0.34495938, 0.29387760),
                float2(-0.91588581, 0.45771432),
                float2(-0.81544232, -0.87912464),
                float2(-0.38277543, 0.27676845),
                float2(0.97484398, 0.75648379),
                float2(0.44323325, -0.97511554),
                float2(0.53742981, -0.47373420),
                float2(-0.26496911, -0.41893023),
                float2(0.79197514, 0.19090188),
                float2(-0.24188840, 0.99706507),
                float2(-0.81409955, 0.91437590),
                float2(0.19984126, 0.78641367),
                float2(0.14383161, -0.14100790)
            };

            static float2 hammersleySequenceL16D2[16] = {
                float2(0.0, 0.0),
                float2(0.0625, 0.5),
                float2(0.125, 0.25),
                float2(0.1875, 0.75),
                float2(0.25, 0.125),
                float2(0.3125, 0.625),
                float2(0.375, 0.375),
                float2(0.4375, 0.875),
                float2(0.5, 0.0625),
                float2(0.5625, 0.5625),
                float2(0.625, 0.3125),
                float2(0.6875, 0.8125),
                float2(0.75, 0.1875),
                float2(0.8125, 0.6875),
                float2(0.875, 0.4375),
                float2(0.9375, 0.9375)
            };

            static float3 hammersleySequenceL16D3[16] = {
                float3(0.3333333333333333, 0.2, 0.14285714285714285),
                float3(0.6666666666666666, 0.4, 0.2857142857142857),
                float3(0.1111111111111111, 0.6, 0.42857142857142855),
                float3(0.4444444444444444, 0.8, 0.5714285714285714),
                float3(0.7777777777777778, 0.04, 0.7142857142857143),
                float3(0.2222222222222222, 0.24, 0.8571428571428571),
                float3(0.5555555555555556, 0.44, 0.02040816326530612),
                float3(0.8888888888888888, 0.64, 0.16326530612244897),
                float3(0.037037037037037035, 0.84, 0.30612244897959184),
                float3(0.37037037037037035, 0.08, 0.4489795918367347),
                float3(0.7037037037037037, 0.28, 0.5918367346938775),
                float3(0.14814814814814814, 0.48, 0.7346938775510204),
                float3(0.48148148148148145, 0.68, 0.8775510204081632),
                float3(0.8148148148148148, 0.88, 0.04081632653061224),
                float3(0.25925925925925924, 0.12, 0.1836734693877551),
                float3(0.5925925925925926, 0.32, 0.32653061224489793)
            };

            static float3 hammersleySequenceL32D3[32] = {
                float3(0.3333333333333333, 0.2, 0.14285714285714285),
                float3(0.6666666666666666, 0.4, 0.2857142857142857),
                float3(0.1111111111111111, 0.6, 0.42857142857142855),
                float3(0.4444444444444444, 0.8, 0.5714285714285714),
                float3(0.7777777777777778, 0.04, 0.7142857142857143),
                float3(0.2222222222222222, 0.24, 0.8571428571428571),
                float3(0.5555555555555556, 0.44, 0.02040816326530612),
                float3(0.8888888888888888, 0.64, 0.16326530612244897),
                float3(0.037037037037037035, 0.84, 0.30612244897959184),
                float3(0.37037037037037035, 0.08, 0.4489795918367347),
                float3(0.7037037037037037, 0.28, 0.5918367346938775),
                float3(0.14814814814814814, 0.48, 0.7346938775510204),
                float3(0.48148148148148145, 0.68, 0.8775510204081632),
                float3(0.8148148148148148, 0.88, 0.04081632653061224),
                float3(0.25925925925925924, 0.12, 0.1836734693877551),
                float3(0.5925925925925926, 0.32, 0.32653061224489793),
                float3(0.9259259259259259, 0.52, 0.46938775510204084),
                float3(0.07407407407407407, 0.72, 0.6122448979591837),
                float3(0.4074074074074074, 0.92, 0.7551020408163265),
                float3(0.7407407407407407, 0.16, 0.8979591836734694),
                float3(0.18518518518518517, 0.36, 0.061224489795918366),
                float3(0.5185185185185185, 0.56, 0.20408163265306123),
                float3(0.8518518518518519, 0.76, 0.3469387755102041),
                float3(0.2962962962962963, 0.96, 0.4897959183673469),
                float3(0.6296296296296297, 0.008, 0.6326530612244898),
                float3(0.9629629629629629, 0.208, 0.7755102040816326),
                float3(0.012345679012345678, 0.408, 0.9183673469387755),
                float3(0.345679012345679, 0.608, 0.08163265306122448),
                float3(0.6790123456790124, 0.808, 0.22448979591836735),
                float3(0.12345679012345678, 0.048, 0.3673469387755102),
                float3(0.4567901234567901, 0.248, 0.5102040816326531),
                float3(0.7901234567901234, 0.448, 0.6530612244897959)
            };



            Texture2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;

            SamplerState _sampler_point_clamp_gdepth;
            
            float _screenPixelWidth;
            float _screenPixelHeight;

            int _TAASampleIndex;

            float3 _cameraUp;

            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;

            // sun
            float3 _LightDirection;
            float3 _LightColor;
            float _LightIntensity;

            // IBL
            samplerCUBE _SpecularIBL;
            samplerCUBE _DiffuseIBL;
            sampler2D _BrdfLut;
            float _IBLIntensity;

            // CSM
            sampler2D _shadowtex0;
            sampler2D _shadowtex1;
            sampler2D _shadowtex2;
            sampler2D _shadowtex3;
            float _split0;
            float _split1;
            float _split2;
            float _split3;
            float4x4 _shadowVpMatrix0;
            float4x4 _shadowVpMatrix1;
            float4x4 _shadowVpMatrix2;
            float4x4 _shadowVpMatrix3;
            float2 _shadowMapWorldSize0;
            float2 _shadowMapWorldSize1;
            float2 _shadowMapWorldSize2;
            float2 _shadowMapWorldSize3;
            float _PCSSKernelRadiusCoefficient;
            float _PCSSLightSize;
            float _PCFDepthBias;

            // SSAO
            float _SSAOSampleRadius;
            float _SSAOStrength;
            float _SSAOSampleWeightVariability;
            float _SSAODeltaDepthFade;

            // SSR
            float _SSROriginPosDepthBias;
            float _SSRObjectThickness;
            Texture2D _SSRRadiance;
            SamplerState _sampler_point_clamp_SSRRadiance;
            sampler2D _SSRResolvedRadiance;
            sampler2D _SSRFilteredRadiance;
            sampler2D _SSRHistory;

            // TAA
            float4x4 _jitteredProjectionMatrix;
            float4x4 _previousProjectionMatrix;
            float4x4 _previousViewMatrix;


            float Rand2to1(float2 uv) {
                return frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float3 RGB2YCoCg(float3 RGBColor) {
                float Y = RGBColor.r * 0.25 + RGBColor.g * 0.5 + RGBColor.b * 0.25;
                float Co = RGBColor.r * 0.5 - RGBColor.b * 0.5;
                float Cg = -RGBColor.r * 0.25 + RGBColor.g * 0.5 - RGBColor.b * 0.25;
                return float3(Y, Co, Cg);
            }

            float3 YCoCg2RGB(float3 YCoCgColor) {
                float R = YCoCgColor.x + YCoCgColor.y - YCoCgColor.z;
                float G = YCoCgColor.x + YCoCgColor.z;
                float B = YCoCgColor.x - YCoCgColor.y - YCoCgColor.z;
                return float3(R, G, B);
            }

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
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                float d = _gdepth.Sample(_sampler_point_clamp_gdepth, i.uv);
                float4 ndcPos = float4(i.uv * 2 - 1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                float4 previousNDCPos = mul(_previousProjectionMatrix, mul(_previousViewMatrix, worldPos));
                previousNDCPos /= previousNDCPos.w;
                float2 previous_uv = previousNDCPos.xy / 2 + 0.5;
                previous_uv = clamp(previous_uv, float2(0, 0), float2(1, 1));

                float4 historyColor = tex2D(_SSRHistory, previous_uv);
                float4 currentColor = tex2D(_SSRFilteredRadiance, i.uv);

                float3 AABBMin, AABBMax;
                AABBMax = AABBMin = RGB2YCoCg(currentColor);

                for (float du = -1; du < 1.5; du++)
                {
                    for (float dv = -1; dv < 1.5; dv++) {
                        float3 C = RGB2YCoCg(tex2D(_SSRFilteredRadiance, i.uv + float2(du / _screenPixelWidth, dv / _screenPixelHeight)));
                        AABBMin = min(AABBMin, C);
                        AABBMax = max(AABBMax, C);
                    }
                }

                float3 historyYCoCg = RGB2YCoCg(historyColor);
                float3 Filtered = (AABBMin + AABBMax) * 0.5f;
                float3 RayOrigin = historyYCoCg;
                float3 RayDir = Filtered - historyYCoCg;
                RayDir = abs(RayDir) < (1.0 / 65536.0) ? (1.0 / 65536.0) : RayDir;
                float3 InvRayDir = rcp(RayDir);

                float3 MinIntersect = (AABBMin - RayOrigin) * InvRayDir;
                float3 MaxIntersect = (AABBMax - RayOrigin) * InvRayDir;
                float3 EnterIntersect = min(MinIntersect, MaxIntersect);
                float ClipBlend = max(EnterIntersect.x, max(EnterIntersect.y, EnterIntersect.z));
                ClipBlend = saturate(ClipBlend);

                float3 ResultYCoCg = lerp(historyYCoCg, Filtered, ClipBlend);
                historyColor.rgb = YCoCg2RGB(ResultYCoCg);

                float lerp_factor = 0.04;
                return float4(lerp(historyColor, currentColor, lerp_factor).xyz, currentColor.a);
            }
            ENDCG
        }
    }
}
