Shader "Hidden/Universal Render Pipeline/Volumetric Light"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile_local _ _USE_RGBM
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        float4 _BlitTexture_TexelSize;

        half _Scattering;;
        half _Steps;
        half _JitterVolumetric;
        half _MaxDistance;
        half _Intensity;

        TEXTURE2D(_VolumetricTexture);
        SAMPLER(sampler_VolumetricTexture);
        TEXTURE2D(_DepthTexture);
        SAMPLER(sampler_DepthTexture);
        half2 _VolumetricTexture_TexelSize;
        half2 _DepthTexture_TexelSize;

        real ShadowAtten(real3 worldPosition)
        {
            return MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));
        }

        half3 GetWorldPos(real2 uv)
        {
            #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(uv);
            #else
                // Adjust z to match NDC for OpenGL
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
            #endif
            return ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
        }

        half random( real2 p ){
            return frac(sin(dot(p, real2(41, 289)))*45758.5453 )-0.5; 
        }
        half random01( real2 p ){
            return frac(sin(dot(p, real2(41, 289)))*45758.5453 ); 
        }
        
        //from Ronja https://www.ronja-tutorials.com/post/047-invlerp_remap/
        half invLerp(real from, real to, real value){
            return (value - from) / (to - from);
        }
        half remap(real origFrom, real origTo, real targetFrom, real targetTo, real value){
            real rel = invLerp(origFrom, origTo, value);
            return lerp(targetFrom, targetTo, rel);
        }

        // Mie scaterring approximated with Henyey-Greenstein phase function.
        half ComputeScattering(real lightDotView)
        {
            real result = 1.0f - _Scattering * _Scattering;
            result /= (4.0f * PI * pow(1.0f + _Scattering * _Scattering - (2.0f * _Scattering) *  lightDotView, 1.5f));
            return result;
        }
        
        half RaymarchFragment(Varyings input) : SV_Target
        {
            half3 worldPos = GetWorldPos(input.texcoord);
            
            half3 startPosition = _WorldSpaceCameraPos.xyz;
            half3 rayVector = worldPos - startPosition;
            half3 rayDirection = normalize(rayVector);
            half rayLength = length(rayVector);
            rayLength = min(rayLength, _MaxDistance);
            //worldPos = startPosition + rayDirection * rayLength;
            half stepLength = rayLength / _Steps;
            half3 step = rayDirection * stepLength;
            half rayStartOffset = random01(input.texcoord) * stepLength * _JitterVolumetric/100;
            half3 currentPosition = startPosition + rayStartOffset * rayDirection;
            half accumulatedFog = 0;
            for (int j = 0; j < _Steps - 1; j++) {
                half4 shadowCoord = TransformWorldToShadowCoord(currentPosition);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lightDir = mainLight.direction;
                half shadowAttenuation = mainLight.shadowAttenuation;
                // if it is in light
                if (shadowAttenuation > 0) {    
                    half kernelColor = ComputeScattering(dot(rayDirection, lightDir));
                    accumulatedFog += kernelColor;
                }
                currentPosition += step;
            }
            accumulatedFog /= _Steps;
            return accumulatedFog;
        }


        int _GaussSamples;
        half _GaussAmount;
        static const half gauss_filter_weights[] = { 0.06136, 0.24477, 0.38774, 0.24477, 0.06136 };
        #define BLUR_DEPTH_FALLOF 100.0
        
        half GaussBlurX(Varyings input) : SV_Target {
            half col = 0;
            half accumResult = 0;
            half accumWeights = 0;
            half depthCenter;
            #if UNITY_REVERSED_Z
                depthCenter = SampleSceneDepth(input.texcoord);
            #else
                // Adjust z to match NDC for OpenGL
                depthCenter = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(input.texcoord));
            #endif
            
            for (int index = -_GaussSamples; index <= _GaussSamples; index++) {
                // offset uvs by small amount   
                half2 uv = input.texcoord + half2(index * _GaussAmount/1000.0, 0);
                half kernelSample = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).r;
                half depthKernel;
                #if UNITY_REVERSED_Z
                    depthKernel = SampleSceneDepth(uv);
                #else
                    // Adjust z to match NDC for OpenGL
                    depthKernel = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif
                half depthDiff = abs(depthCenter - depthKernel);
                half2 r2 = depthDiff * BLUR_DEPTH_FALLOF;
                half g = exp(-r2*r2);
                half weight = gauss_filter_weights[abs(index)] * g;
                accumResult += kernelSample * weight;
                accumWeights += weight;
            }
            col = accumResult / accumWeights;

            return col;
        }


        half GaussBlurY(Varyings input) : SV_Target {
            half col = 0;
            half accumResult = 0;
            half accumWeights = 0;
            half depthCenter;
            #if UNITY_REVERSED_Z
                depthCenter = SampleSceneDepth(input.texcoord);
            #else
                // Adjust z to match NDC for OpenGL
                depthCenter = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(input.texcoord));
            #endif
            
            for (int index = -_GaussSamples; index <= _GaussSamples; index++) {
                // offset uvs by small amount   
                half2 uv = input.texcoord + half2(0, index * _GaussAmount/1000.0);
                half kernelSample = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).r;
                half depthKernel;
                #if UNITY_REVERSED_Z
                    depthKernel = SampleSceneDepth(uv);
                #else
                    // Adjust z to match NDC for OpenGL
                    depthKernel = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif
                half depthDiff = abs(depthCenter - depthKernel);
                half2 r2 = depthDiff * BLUR_DEPTH_FALLOF;
                half g = exp(-r2*r2);
                half weight = gauss_filter_weights[abs(index)] * g;
                accumResult += kernelSample * weight;
                accumWeights += weight;
            }
            col = accumResult / accumWeights;

            return col;
        }

        half4 CompositingFragment(Varyings input) : SV_TARGET {
            half color = 0;
            
            
            /// TEMP //////////////////////
            // WE are not using downsampling for now
            half4 shadowCoord = TransformWorldToShadowCoord(GetWorldPos(input.texcoord));
            Light mainLight = GetMainLight(shadowCoord);
            half3 lightDir = mainLight.direction;
            half3 lightColor = mainLight.color;
            color = SAMPLE_TEXTURE2D(_VolumetricTexture, sampler_VolumetricTexture, input.texcoord).r;
            //return half4(half3(color, color, color), 1);
            half3 finalShaft = (saturate(color) * _Intensity) *(lightColor); 
            half3 screen = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
            //return half4(finalShft, 1);

            half3 finalColor = screen + finalShaft;
            return half4(finalColor, 1);
            /// TEMP //////////////////////

            // The following code is if we want to use downsampling to improve performance
            /*
            int offset = 0;
            half d0 = SampleSceneDepth(input.texcoord);
            
            // calculating the distances between the depths of the 
            // current pixel and its neighbors and the full resolution depth
            // value
            half d1 = SAMPLE_TEXTURE2D(_DepthTexture, sampler_DepthTexture, input.texcoord + half2(0, _BlitTexture_TexelSize.y)).r;
            half d2 = SAMPLE_TEXTURE2D(_DepthTexture, sampler_DepthTexture, input.texcoord + half2(_BlitTexture_TexelSize.x, 0)).r;
            half d3 = SAMPLE_TEXTURE2D(_DepthTexture, sampler_DepthTexture, input.texcoord + half2(-_BlitTexture_TexelSize.x, 0)).r;
            half d4 = SAMPLE_TEXTURE2D(_DepthTexture, sampler_DepthTexture, input.texcoord + half2(0, -_BlitTexture_TexelSize.y)).r;

            d1 = abs(d0 - d1);
            d2 = abs(d0 - d2);
            d3 = abs(d0 - d3);
            d4 = abs(d0 - d4);

            half dmin = min(min(min(d1, d2), d3), d4);
            if (dmin == d1) offset = 0;
            if (dmin == d2) offset = 1;
            if (dmin == d3) offset = 2;
            if (dmin == d4) offset = 3;

            switch (offset) {
                case 0:
                    color = SAMPLE_TEXTURE2D(_VolumetricTexture, sampler_VolumetricTexture, input.texcoord + half2(0, _VolumetricTexture_TexelSize.y)).r;
                    break;
                case 1:
                    color = SAMPLE_TEXTURE2D(_VolumetricTexture, sampler_VolumetricTexture, input.texcoord + half2(_VolumetricTexture_TexelSize.x, 0)).r;
                    break;
                case 2:
                    color = SAMPLE_TEXTURE2D(_VolumetricTexture, sampler_VolumetricTexture, input.texcoord + half2(-_VolumetricTexture_TexelSize.x, 0)).r;
                    break;
                case 3:
                    color = SAMPLE_TEXTURE2D(_VolumetricTexture, sampler_VolumetricTexture, input.texcoord + half2(0, -_VolumetricTexture_TexelSize.y)).r;
                    break;
            }
            half4 shadowCoord = TransformWorldToShadowCoord(GetWorldPos(input.texcoord));
            Light mainLight = GetMainLight(shadowCoord);
            half3 lightDir = mainLight.direction;
            half3 lightColor = mainLight.color;

            half3 finalShaft = (saturate(color) * _Intensity) * normalize(lightColor);
            half3 screen = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
            //return half4(finalShft, 1);

            half3 finalColor = screen + finalShaft;
            return half4(finalColor, 1);*/
        }

        // This pass is used to sample the depth texture
        // It will be used properly if we want to use downsampling
        half SampleDepth(Varyings input) : SV_Target {
            #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(input.texcoord);
            #else
                // Adjust z to match NDC for OpenGL
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(input.texcoord));
            #endif
            return depth;
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Ray Marching Pass"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment RaymarchFragment
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Blur Horizontal"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment GaussBlurX
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Blur Vertical"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment GaussBlurY
            ENDHLSL
        }
    
        Pass {
            Name "Compositing Pass"
           
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment CompositingFragment
            ENDHLSL
        }

        Pass
        {
            Name "Sample Depth"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment SampleDepth
            ENDHLSL 
        }
    }
}
