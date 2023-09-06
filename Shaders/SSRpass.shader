Shader "NaiveRP/SSRpass"
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
            SamplerState _sampler_linear_clamp_gdepth;
            
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
            int _SSRSampleH;
            Texture2D _SSRData;
            SamplerState _sampler_point_clamp_SSRData;
            Texture2D _SSRRadiance;
            SamplerState _sampler_point_clamp_SSRRadiance;
            sampler2D _SSRResolvedRadiance;

            float Rand2to1(float2 uv) {
                return frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float Rand21to1(float2 uv, float t) {
                float s = frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453123);
                return frac(sin(dot(float2(s, t), float2(12.9898, 78.233))) * 43758.5453123);
            }

            float Rand3to1(float u, float v, float t) {
                float s = frac(sin(dot(float2(u, v), float2(12.9898, 78.233))) * 43758.5453123);
                return frac(sin(dot(float2(s, t), float2(12.9898, 78.233))) * 43758.5453123);
            }

            float3 PBRShading(float3 normal, float3 view_dir, float3 light_dir, float3 light_color, float roughness, float3 albedo, float metallic) {

                roughness = max(roughness, 0.05);

                float3 n = normal;
                float3 v = view_dir;
                float3 l = light_dir;
                float3 h = normalize(v + l);
                float3 r = normalize(reflect(-v, n));

                if (dot(n, v) <= 0) return 0;

                float v_dot_h = max(dot(v, h), 0);
                float n_dot_v = max(dot(n, v), 0);
                float n_dot_h = max(dot(n, h), 0);
                float n_dot_l = max(dot(n, l), 0);

                float3 F0_diffuse = float3(0.04, 0.04, 0.04);

                // F
                float3 F0 = lerp(F0_diffuse, albedo, float3(metallic, metallic, metallic));
                float power = (-5.55473 * v_dot_h - 6.98316) * v_dot_h;
                float3 F = F0 + (1 - F0) * pow(2, power);

                // D
                float a = roughness * roughness;
                float a_2 = a * a;
                float tmp = n_dot_h * n_dot_h * (a_2 - 1) + 1;
                float D = a_2 / (UNITY_PI * tmp * tmp);

                // G
                float k = (roughness + 1) * (roughness + 1) * 0.125;
                float G1 = n_dot_v / (n_dot_v * (1 - k) + k);
                float G2 = n_dot_l / (n_dot_l * (1 - k) + k);
                float G = G1 * G2;

                // specular
                float3 specular_brdf = D * F * G / (4 * n_dot_l * n_dot_v + 0.001);
                float3 specular_color = specular_brdf * light_color * n_dot_l;

                // diffuse
                float3 weight_diffuse = lerp(float3(1, 1, 1) - F, float3(0, 0, 0), float3(metallic, metallic, metallic));
                float3 diffuse_brdf = albedo / UNITY_PI * weight_diffuse;
                float3 diffuse_color = diffuse_brdf * light_color * n_dot_l;

                return (specular_color + diffuse_color) * UNITY_PI;
            }

            float3 IBL(float3 normal, float3 view_dir, float roughness, float3 albedo, float metallic) {
                
                float3 n = normal;
                float3 v = view_dir;
                float3 r = normalize(reflect(-v, n));

                float n_dot_v = max(dot(n, v), 0);

                float3 F0_diffuse = float3(0.04, 0.04, 0.04);
                float3 F0 = lerp(F0_diffuse, albedo, float3(metallic, metallic, metallic));
                //float power = (-5.55473 * n_dot_v - 6.98316) * n_dot_v;
                //float3 F = F0 + (1 - F0) * pow(2, power);
                float3 F = F0;
                float3 weight_diffuse = lerp(float3(1, 1, 1) - F, float3(0, 0, 0), float3(metallic, metallic, metallic));

                // specular IBL
                float roughness_clamp = min(roughness, 0.97);
                float roughness_lod = roughness_clamp * (1.7 - 0.7 * roughness_clamp);
                float lod = 6.0 * roughness_lod;  // Unity mipmap: [0, 1, 2, 3, 4, 5, 6]
                float3 specular_light_IBL = texCUBElod(_SpecularIBL, float4(r, lod)).rgb;
                float2 specular_brdf_IBL = tex2D(_BrdfLut, float2(n_dot_v, roughness_clamp)).rg;
                float3 specular_color_IBL = specular_light_IBL * (F0 * specular_brdf_IBL.x + specular_brdf_IBL.y);

                // diffuse IBL
                float3 diffuse_light_IBL = texCUBE(_DiffuseIBL, n).rgb;
                float3 diffuse_color_IBL = weight_diffuse * albedo * diffuse_light_IBL;

                // sum
                float3 color_IBL = specular_color_IBL + diffuse_color_IBL;

                return color_IBL * _IBLIntensity;
            }

            float3 SSR(float3 worldPos, float3 normal, float3 reflectionDir, out float hit, out float3 hitWorldPos) {
                
                float stepLength = 0.2;
                hit = 0;
                float3 currentWorldPos = worldPos;
                float4 currentNDCPos = mul(_vpMatrix, float4(currentWorldPos, 1));
                currentNDCPos /= currentNDCPos.w;
                currentNDCPos.z += _SSROriginPosDepthBias;
                float4 currentWorldPos4 = mul(_vpMatrixInv, currentNDCPos);
                currentWorldPos = currentWorldPos4.xyz / currentWorldPos4.w;
                currentNDCPos.y = -currentNDCPos.y;
                float2 currentUV = (currentNDCPos.xy + 1) * 0.5;
                float2 nextUV;
                UNITY_LOOP
                for (int step = 0; step < 150; step++) {
                    float4 currentNDCPos = mul(_vpMatrix, float4(currentWorldPos, 1));
                    currentNDCPos /= currentNDCPos.w;
                    currentNDCPos.y = -currentNDCPos.y;
                    currentUV = (currentNDCPos.xy + 1) * 0.5;
                    float3 nextWorldPos = currentWorldPos + reflectionDir * stepLength;
                    float4 nextNDCPos = mul(_vpMatrix, float4(nextWorldPos, 1));
                    nextNDCPos /= nextNDCPos.w;
                    nextNDCPos.y = -nextNDCPos.y;
                    nextUV = (nextNDCPos.xy + 1) * 0.5;
                    float nextNDCDepth = nextNDCPos.z;
                    float NextGbuffeDepth = UNITY_SAMPLE_DEPTH(_gdepth.Sample(_sampler_point_clamp_gdepth, nextUV));
                    if (nextUV.x < 0 || nextUV.x > 1 || nextUV.y < 0 || nextUV.y > 1) {
                        break;
                    }
                    else if (nextNDCDepth <= NextGbuffeDepth) {
                        if (length((nextUV - currentUV) * float2(_screenPixelWidth, _screenPixelHeight)) > 1.0f) {
                            stepLength *= 0.5;
                        }
                        else {
                            currentWorldPos = nextWorldPos;
                            if (Linear01Depth(nextNDCDepth) < Linear01Depth(NextGbuffeDepth) + _SSRObjectThickness) { 
                                hit = 1;
                                hitWorldPos = nextWorldPos;
                            }
                            break;
                        }
                    }
                    else {
                        currentWorldPos = nextWorldPos;
                    }
                }

                float3 screenSpaceRadiance = float3(0, 0, 0);
                if (hit == 1) {
                    float3 V = -reflectionDir;
                    float3 N = normalize(tex2D(_GT1, nextUV).rgb * 2 - 1);
                    float3 L = normalize(_LightDirection.xyz);
                    float3 lightRadiance = _LightColor * _LightIntensity;
                    float4 GT2 = tex2D(_GT2, nextUV);
                    float roughness = GT2.b;
                    float metallic = GT2.a;
                    float3 albedo = tex2D(_GT0, nextUV).rgb;
                    float shadow = 1;
                    float SSAOWeight = 1;
                    float3 directLightReflectionRadiance = PBRShading(N, V, L, lightRadiance, roughness, albedo, metallic);
                    float3 indirectLightReflectionRadiance = IBL(N, V, roughness, albedo, metallic);
                    screenSpaceRadiance = directLightReflectionRadiance * shadow + indirectLightReflectionRadiance * SSAOWeight;
                }
                
                return screenSpaceRadiance;
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

            void frag(v2f i, 
                      out float4 SSRData : SV_Target0, 
                      out float4 SSRH : SV_Target1,
                      out float SSRPDF : SV_Target2)
            {

                float3 albedo = tex2D(_GT0, i.uv).rgb;
                float3 normal = normalize(tex2D(_GT1, i.uv).rgb * 2 - 1);
                float4 GT2 = tex2D(_GT2, i.uv);
                float4 GT3 = tex2D(_GT3, i.uv);
                float2 motionVec = GT2.rg;
                float roughness = max(GT2.b, 0.05);
                float a = roughness * roughness;
                float metallic = GT2.a;
                float3 emission = GT3.rgb;
                float occlusion = GT3.a;
                float d = _gdepth.Sample(_sampler_point_clamp_gdepth, i.uv);
                float d_linear = Linear01Depth(d);
                float4 ndcPos = float4(i.uv * 2 - 1, d, 1);
                ndcPos.y = -ndcPos.y;
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;
                
                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);

                // sample H
                float3 H = normal;
                if (_SSRSampleH) {
                    float3 randomVector = hammersleySequenceL32D3[_TAASampleIndex];
                    float3 tangent = normalize(cross(normal, randomVector));
                    float3 binormal = normalize(cross(normal, tangent));
                    float rand1 = Rand21to1(i.uv, _Time.x);
                    float rand2 = Rand21to1(i.uv, _Time.y);
                    float theta = atan(a * sqrt(rand1 / (1 - rand1)));
                    float phi = 2 * UNITY_PI * rand2;
                    H = normal * cos(theta) + tangent * sin(phi) * sin(theta) + binormal * cos(phi) * sin(theta);
                }
                
                // compute pdf
                float a_2 = a * a;
                float n_dot_h = dot(normal, H);
                float PDF_tmp = n_dot_h * n_dot_h * (a_2 - 1) + 1;
                float PDF = a_2 / (UNITY_PI * PDF_tmp * PDF_tmp);

                // compute radiance
                float3 R = normalize(reflect(-V, H));
                float hit;
                float3 hitWorldPos;
                float3 col = SSR(worldPos.xyz, normal, R, hit, hitWorldPos);
                float4 hitNDCPos = mul(_vpMatrix, float4(hitWorldPos, 1));
                hitNDCPos /= hitNDCPos.w;
                hitNDCPos.y = -hitNDCPos.y;
                float2 hitUV = hitNDCPos.xy * 0.5 + 0.5;

                SSRData = float4(hitUV, hitNDCPos.z, hit);
                SSRH = float4(H * 0.5 + 0.5, hit);
                SSRPDF = PDF;
            }
            ENDCG
        }
    }
}
