Shader "Snow/RawDeformation"
{
    Properties
    {
    }

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
            TEXTURE2D_X(_CameraDepthTexture);

            /*********************************
            *        Vertex attributes       *
            *********************************/
            struct Attributes
            {
                float4 position : POSITION;
            };

            /*********************************
            *         Shader varyings        *
            *********************************/
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            /*********************************
            *         Vertex shader          *
            *********************************/
            Varyings VertexDeform(Attributes _attributes)
            {
                Varyings varyings;
                varyings.positionCS = TransformObjectToHClip(_attributes.position.xyz);
                return varyings;
            }

            /*********************************
            *         Fragment shader        *
            *********************************/
            float4 FragmentDeform(Varyings _varyings) : SV_TARGET
            {
                // Retrieve current fragment screen coordinates and depth (no need to 
                // perform perspective division here, as the camera used is orthographic).
                int2 screenIuv = int2(_varyings.positionCS.xy);
                float depth = _varyings.positionCS.z;

                // Retrieve terrain depth from the current depth buffer (We performed depth only 
                // pass on terrain objects using the same camera right before executing this one).
                float terrainDepth = LOAD_TEXTURE2D_X(_CameraDepthTexture, screenIuv).r;

                // Only write to the deformation map if the current deformer 
                // intersects with the snow cover (= perform a depth test).
                #if defined(UNITY_REVERSED_Z)
                    float deformation = step(depth, terrainDepth);
                #else
                    float deformation = step(terrainDepth, depth);
                #endif

                return float4(deformation, 0, 0, 0);
            }

        ENDHLSL

        // RAW DEFORMATION PASS
        Pass
        {
            Name "RawDeformation"

            // -------------------------------------
            // Fixed states
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
                #pragma target 2.0
                #pragma enable_d3d11_debug_symbols

                // -------------------------------------
                // Shader Stages
                #pragma vertex VertexDeform
                #pragma fragment FragmentDeform
            ENDHLSL
        }
    }
}
