// Upgrade NOTE: replaced 'glstate_matrix_projection' with 'UNITY_MATRIX_P'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NaiveRP/gbuffer"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        
        [Toggle] _UseNormalMap("Use Normal Map", float) = 0
        _NormalMap("Normal Map", 2D) = "normal" {}

        _Roughness("Roughness", Range(0.0, 1.0)) = 0.5
        [Toggle] _UseRoughnessMap("Use Roughness Map", float) = 0
        _RoughnessMap("RoughnessMap", 2D) = "white" {}

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Toggle] _UseMetallicMap("Use Metallic Map", float) = 0
        _MetallicMap("MetallicMap", 2D) = "white" {}
    }

        SubShader
    {

        Pass
        {
            Tags { "LightMode" = "depthonly" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 depth : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = o.vertex.zw;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = i.depth.x / i.depth.y;
            #if defined (UNITY_REVERSED_Z)
                d = 1.0 - d;
            #endif
                fixed4 c = EncodeFloatRGBA(d);
                return c;
            }
            ENDCG
        }

        
        Pass
        {
            Tags { "LightMode" = "gbuffer" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal_world : TEXCOORD2;
                float3 tangent_world : TEXCOORD3;
                float3 binormal_world : TEXCOORD4;
                float4 position_world : TEXCOORD5;
                float4 position : SV_POSITION;
            };

            float4x4 _jitteredProjectionMatrix;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _UseNormalMap;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;

            float _Roughness;
            float _UseRoughnessMap;
            sampler2D _RoughnessMap;
            float4 _RoughnessMap_ST;

            float _Metallic;
            float _UseMetallicMap;
            sampler2D _MetallicMap;
            float4 _MetallicMap_ST;

            v2f vert(appdata v)
            {
                v2f o;
                //o.position = UnityObjectToClipPos(v.position);
                //o.position = mul(mul(UNITY_MATRIX_P, UNITY_MATRIX_MV), v.position);
                o.position = mul(mul(_jitteredProjectionMatrix, UNITY_MATRIX_MV), v.position);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal_world = UnityObjectToWorldNormal(v.normal);
                o.tangent_world = normalize(mul(unity_ObjectToWorld, float4(v.tangent, 0))).xyz;
                o.binormal_world = normalize(cross(o.normal_world, o.tangent_world));
                o.position_world = mul(unity_ObjectToWorld, v.position);
                return o;
            }
            
            void frag(
                v2f i,
                out float4 GT0 : SV_Target0,
                out float4 GT1 : SV_Target1,
                out float4 GT2 : SV_Target2,
                out float4 GT3 : SV_Target3,
                out float depth : SV_Depth)
            {

                // 采样纹理
                float3 color = tex2D(_MainTex, i.uv).rgb;

                // 采样法线贴图
                float3 normal_local = float3(0, 0, 1);
                if (_UseNormalMap) {
                    float4 normal_compressed = tex2D(_NormalMap, i.uv);
                    normal_local = UnpackNormal(normal_compressed);
                }
                float3x3 TBN_matrix = float3x3 (
                    i.tangent_world.xyz,
                    i.binormal_world,
                    i.normal_world
                    );
                float3 normal_world = normalize(mul(normal_local, TBN_matrix));

                // 采样roughness map
                float roughness = _Roughness;
                if (_UseRoughnessMap) {
                    roughness = tex2D(_RoughnessMap, i.uv);
                }

                // 采样metallic map
                float metallic = _Metallic;
                if (_UseMetallicMap) {
                    metallic = tex2D(_MetallicMap, i.uv);
                }

                // 未完成
                float3 emission = float3(0, 0, 0);
                float ao = 0;

                GT0 = float4(color, 1);
                GT1 = float4(normal_world * 0.5 + 0.5, 0);
                GT2 = float4(0, 0, roughness, metallic);
                GT3 = float4(emission, ao);
                depth = i.position.z;
            }
            

            ENDCG
        }
    }
}