Shader "NaiveRP/TAA"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _screenPixelWidth;
            float _screenPixelHeight;

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _gdepth;
            sampler2D _HistoryFrameBuffer;

            float _TAABlendWeight;
            
            float4x4 _previousViewMatrix;
            float4x4 _previousProjectionMatrix;
            float4x4 _jitteredProjectionMatrix;
            float4x4 _vpMatrixInv;


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
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float2(v.uv.x, v.uv.y);

                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {

                // reprojection
                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, i.uv));
                float4 ndcPos = float4(i.uv.x * 2 - 1, -i.uv.y * 2 + 1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                float4 previousNDCPos = mul(_previousProjectionMatrix, mul(_previousViewMatrix, worldPos));
                previousNDCPos /= previousNDCPos.w;
                previousNDCPos.y = -previousNDCPos.y;
                float2 previous_uv = previousNDCPos.xy * 0.5 + 0.5;
                previous_uv = clamp(previous_uv, float2(0, 0), float2(1, 1));

                // 采样当前帧和历史帧颜色
                float4 currentColor = tex2D(_MainTex, i.uv);
                float4 historyColor = tex2D(_HistoryFrameBuffer, previous_uv);

                // clip
                float3 AABBMin, AABBMax;
                AABBMax = AABBMin = RGB2YCoCg(currentColor);

                for (float du = -1; du < 1.5; du++)
                {
                    for (float dv = -1; dv < 1.5; dv++) {
                        float3 C = RGB2YCoCg(tex2D(_MainTex, i.uv + float2(du / _screenPixelWidth, dv / _screenPixelHeight)));
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

                return lerp(historyColor, currentColor, _TAABlendWeight);
                
            }
            ENDCG
        }
    }
}
