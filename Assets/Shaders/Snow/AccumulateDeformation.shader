Shader "Universal Render Pipeline/Custom/AccumulateDeformation"
{
    SubShader
    {
        LOD 0

        // SHADER CODE
        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            /*********************************
            *        Global resources        *
            *********************************/
            TEXTURE2D(_PrevSnowDeformationMap);
            SAMPLER(sampler_PrevSnowDeformationMap);

            TEXTURE2D(_RawSnowDeformationMap);
            SAMPLER(sampler_RawSnowDeformationMap);

            uniform float3 _SnowDeformationOriginOffset;
            uniform float _SnowDeformationAreaPixels;

            /*********************************
            *        Global constants        *
            *********************************/
            // 3x3 Gaussian kernel
            static const float kWeights[9] = {
                0.077847, 0.123317, 0.077847,
                0.123317, 0.195346, 0.123317,
                0.077847, 0.123317, 0.077847,
            };

            static const float2 kOffsets[9] = {
                float2(-1.0, -1.0), float2( 0.0, -1.0), float2( 1.0, -1.0),
                float2(-1.0,  0.0), float2( 0.0,  0.0), float2( 1.0,  0.0),
                float2(-1.0,  1.0), float2( 0.0,  1.0), float2( 1.0,  1.0),
            };

            /*********************************
            *        Vertex attributes       *
            *********************************/
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            /*********************************
            *         Shader varyings        *
            *********************************/
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            /*********************************
            *         Vertex shader          *
            *********************************/
            Varyings VertexFullscreen(Attributes _attributes)
            {
                Varyings varyings;
                varyings.pos = GetFullScreenTriangleVertexPosition(_attributes.vertexID);
                varyings.uv  = GetFullScreenTriangleTexCoord(_attributes.vertexID);
                return varyings;
            }

            /*********************************
            *         Fragment shader        *
            *********************************/
            float4 FragmentAccumulation(Varyings _varyings) : SV_TARGET
            {
                // float rawDeformation = 0.f;
                // for (int i = 0; i < 9; ++i)
                // {
                //     const float2 offsetUv = _varyings.uv + 5.f * kOffsets[i] / _SnowDeformationAreaPixels;
                //     rawDeformation += SAMPLE_TEXTURE2D_LOD(_RawSnowDeformationMap, sampler_RawSnowDeformationMap, offsetUv, 0).r * kWeights[i];
                // }

                // 1 - sample current raw deformation
                float rawDeformation = SAMPLE_TEXTURE2D_LOD(_RawSnowDeformationMap, sampler_RawSnowDeformationMap, _varyings.uv, 0).r;

                // 2 - sample previous blended deformation
                float prevDeformation = 0.f;
                float2 previousUv = _varyings.uv + _SnowDeformationOriginOffset.xz;

                if (previousUv.x >= 0 && previousUv.x <= 1 && previousUv.y >= 0 && previousUv.y <= 1)
                {
                    prevDeformation = SAMPLE_TEXTURE2D_LOD(_PrevSnowDeformationMap, sampler_PrevSnowDeformationMap, previousUv, 0).r;
                }

                // 3 - accumulate previous and current deformation
                float deformation = max(prevDeformation, rawDeformation);
                return float4(deformation, 0, 0, 0);
            }

        ENDHLSL

        // ACCUMULATION PASS
        Pass
        {
            Name "AccumulateDeformation"

            // -------------------------------------
            // Fixed states
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
                #pragma target 2.0

                // -------------------------------------
                // Shader Stages
                #pragma vertex VertexFullscreen
                #pragma fragment FragmentAccumulation
            ENDHLSL
        }
    }
}