Shader "Snow/SnowyTerrain"
{
    Properties
    {
        [Space]
        [Header(## Snow Texturing)][Space]
        [NoScaleOffset]_AlbedoMap("Albedo Map", 2D) = "white" {}
        [NoScaleOffset]_NormalMap("Normal Map", 2D) = "bump" {}
        [NoScaleOffset]_RoughMap ("Rough Map", 2D)  = "white" {}
        [NoScaleOffset]_CavityMap("Cavity Map", 2D) = "white" {}
        _WorldUvScale("World UV Scale", Range(0, 1)) = 0.5
        _WorldUvOffset("World UV Offset", Float) = 0.0

        [Space]
        [Header(## Snow Depth)][Space]
        _SnowBaseDepth("Snow Base Depth (cm)", Range(0, 100)) = 20
        [NoScaleOffset]_NoiseDepthMap("Noise Depth Map", 2D) = "gray" {}
        _NoiseIntensity("Noise Intensity", Range(0, 100)) = 1.0
        _NoiseUvScale("Noise UV Scale", Range(0, 1)) = 0.5
        _NoiseUvOffset("Noise UV Offset", Float) = 0.0

        [Space]
        [Header(## Snow Deformation)][Space]
        _SnowTrackTint("Snow Track Tint", Color) = (1, 1, 1, 1)
        [Toggle(RECONSTRUCT_NORMALS)] _ReconstructNormals("Reconstruct Normals", Float) = 1

        [Space]
        [Header(## Terrain Tessellation)][Space]
        _TessellationFactor("Tessellation Factor", Range(0.1, 100)) = 15
        _TessellationDistance("Tessellation Distance (m)", Range(1, 100)) = 30

        // unity lighting
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        LOD 300

        // SHADER CODE
        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            #if defined(GBUFFER_OUT)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

            #elif defined(SHADOW_OUT)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #endif

            /*********************************
            *       Material resources       *
            *********************************/
            TEXTURE2D(_AlbedoMap);
            SAMPLER(sampler_AlbedoMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            TEXTURE2D(_RoughMap);
            SAMPLER(sampler_RoughMap);

            TEXTURE2D(_CavityMap);
            SAMPLER(sampler_CavityMap);

            TEXTURE2D(_NoiseDepthMap);
            SAMPLER(sampler_NoiseDepthMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _SnowTrackTint;

                float _WorldUvScale;
                float _WorldUvOffset;
                float _TessellationFactor;
                float _TessellationDistance;

                float _SnowBaseDepth;
                float _NoiseIntensity;
                float _NoiseUvScale;
                float _NoiseUvOffset;
            CBUFFER_END

            /*********************************
            *        Global resources        *
            *********************************/
            TEXTURE2D(_CurSnowDeformationMap);
            SAMPLER(sampler_CurSnowDeformationMap);

            float3 _CurSnowDeformationOrigin;
            float _SnowDeformationAreaMeters;
            float _SnowDeformationAreaPixels;

            #if defined(SHADOW_OUT)
                // Shadow Casting Light geometric parameters, used when applying the shadow Normal Bias and are set by method
                // SetupShadowCasterConstantBuffer() defined in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
                float3 _LightDirection;
                float3 _LightPosition;
            #endif

            /*********************************
            *        Helper functions        *
            *********************************/
            float3 Utils_WorldToTan(in float3 _vectorWS, in float3 _T, in float3 _B, in float3 _N)
            {
                return float3(dot(_vectorWS, _T), dot(_vectorWS, _B), dot(_vectorWS, _N));
            }

            float3 Utils_TanToWorld(in float3 _vectorTS, in float3 _T, in float3 _B, in float3 _N)
            {
                return _vectorTS.x * _T + _vectorTS.y * _B + _vectorTS.z * _N;
            }

            bool Utils_CheckUvBounds(in float2 _uv)
            {
                return (_uv.x >= 0 && _uv.x <= 1 && _uv.y >= 0 && _uv.y <= 1);     
            }

            float2 Utils_GetWorldUv(in float3 _positionWS, in float _scale, in float _offset)
            {
                // assuming Y-axis is the world up
                return _positionWS.xz * _scale + _offset;
            }

            float Utils_GetDistanceBasedTessellation(float3 _positionWS)
            {
                const float minDistance = 0.2f;
                const float maxDistance = _TessellationDistance;

                float distanceToOrigin = distance(_positionWS, _CurSnowDeformationOrigin);
                float distanceRatio = 1.f - (distanceToOrigin - minDistance) / (maxDistance - minDistance);

                return clamp(distanceRatio, 0.01f, 1.0f);
            }

            float3 Utils_GetTransformScale(in float4x4 _transform)
            {
                float3 scale;
                scale.x = length(_transform[0].xyz);
                scale.y = length(_transform[1].xyz);
                scale.z = length(_transform[2].xyz);
                return scale;
            }

            float2 Snow_WorldToUv(in float3 _positionWS)
            {
                // assuming Y-axis is the world up
                return 0.5f + (_positionWS.xz - _CurSnowDeformationOrigin.xz) / _SnowDeformationAreaMeters;
            }

            float Snow_SampleDepthNoise(in float3 _positionWS)
            {
                // sample noise map based on world UVs to ensure continuity of the snow depth across the surface
                float2 noiseUv = Utils_GetWorldUv(_positionWS, _NoiseUvScale, _NoiseUvOffset);
                float noiseDepth = SAMPLE_TEXTURE2D_LOD(_NoiseDepthMap, sampler_NoiseDepthMap, noiseUv, 0).r;

                // re-range depth in [-noiseIntensity, noiseIntensity]
                noiseDepth = 2.f * noiseDepth - 1.f;
                noiseDepth *= _NoiseIntensity;

                return noiseDepth;
            }

            float Snow_SampleDeformation(in float2 _snowUv)
            {
                // early discard when sampling out of the snow map
                if (!Utils_CheckUvBounds(_snowUv))
                {
                    return 0.f;
                }

                float snowDeformation = SAMPLE_TEXTURE2D_LOD(_CurSnowDeformationMap, sampler_CurSnowDeformationMap, _snowUv, 0).r;

                // fade out on edge to prevent bleeding
                snowDeformation *= smoothstep(0.99f, 0.9f, _snowUv.x) * smoothstep(0.99f, 0.9f, 1.0f - _snowUv.x);
                snowDeformation *= smoothstep(0.99f, 0.9f, _snowUv.y) * smoothstep(0.99f, 0.9f, 1.0f - _snowUv.y);

                return snowDeformation;
            }

            float3 Snow_ReconstructNormal(in float2 _snowUv)
            {
                // early discard when sampling out of the snow map
                if (!Utils_CheckUvBounds(_snowUv))
                {
                    return float3(0.0f, 0.0f, 1.0f);
                }

                int2 iuv = int2(_SnowDeformationAreaPixels * _snowUv);
                float snowInitialDepthMeters = _SnowBaseDepth * 1e-2f;

                // sample nearest pixels in the deformation map
                float4 snowSamples;
                snowSamples[0] = LOAD_TEXTURE2D_LOD(_CurSnowDeformationMap, iuv + int2(-1, 0), 0).r;
                snowSamples[1] = LOAD_TEXTURE2D_LOD(_CurSnowDeformationMap, iuv + int2( 1, 0), 0).r;
                snowSamples[2] = LOAD_TEXTURE2D_LOD(_CurSnowDeformationMap, iuv + int2( 0,-1), 0).r;
                snowSamples[3] = LOAD_TEXTURE2D_LOD(_CurSnowDeformationMap, iuv + int2( 0, 1), 0).r;

                // reconstruct tangent space normal using finite difference
                float3 normalTS;
                normalTS.x = (snowSamples[1] - snowSamples[0]) * snowInitialDepthMeters;
                normalTS.y = (snowSamples[3] - snowSamples[2]) * snowInitialDepthMeters;
                normalTS.z = 2.f * _SnowDeformationAreaMeters / _SnowDeformationAreaPixels;

                return normalize(normalTS);
            }

            /*********************************
            *        Vertex attributes       *
            *********************************/
            struct Attributes
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texCoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID

                #if defined(LIGHTMAP_ON)
                    float2 staticLightmapUV : TEXCOORD1;
                #endif
                #if defined(DYNAMICLIGHTMAP_ON)
                    float2 dynamicLightmapUV : TEXCOORD2;
                #endif
            };

            /*********************************
            *         Control point          *
            *********************************/
            struct ControlPoint
            {
                float4 position : INTERNALTESSPOS;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texCoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID

                #if defined(LIGHTMAP_ON)
                    float2 staticLightmapUV : TEXCOORD1;
                #endif
                #if defined(DYNAMICLIGHTMAP_ON)
                    float2 dynamicLightmapUV : TEXCOORD2;
                #endif
            };

            /*********************************
            *      Tessellation factors      *
            *********************************/
            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            /*********************************
            *         Shader varyings        *
            *********************************/
            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO

                #if defined(GBUFFER_OUT)
                    float2 worldUv : TEXCOORD0;
                    float3 positionWS : TEXCOORD1;
                    float3 normalWS : TEXCOORD2;
                    float3 tangentWS : TEXCOORD3;
                    float3 bitangentWS : TEXCOORD4;
                    float deformation : TEXCOORD5;

                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                        float4 shadowCoord : TEXCOORD6;
                    #endif

                    #if defined(LIGHTMAP_ON)
                        float2 staticLightmapUV : TEXCOORD7;
                    #else
                        half3 vertexSH : TEXCOORD7;
                    #endif

                    #if defined(DYNAMICLIGHTMAP_ON)
                        float2 dynamicLightmapUV : TEXCOORD8;
                    #endif
                #endif
            };

            /*********************************
            *         Vertex shaders         *
            *********************************/
            Varyings VertexDefault(Attributes _attributes)
            {
                Varyings varyings = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(_attributes);
                UNITY_TRANSFER_INSTANCE_ID(_attributes, varyings);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyings);

                //____________________________________________________________________________
                // 1 - Compute initial world space attributes
                float3 positionWS = TransformObjectToWorld(_attributes.position.xyz);
                float3 normalWS = TransformObjectToWorldDir(_attributes.normal.xyz);
                float3 tangentWS = TransformObjectToWorldDir(_attributes.tangent.xyz);
                float3 bitangentWS = normalize(cross(normalWS, tangentWS) * _attributes.tangent.w);
                float2 worldUv = Utils_GetWorldUv(positionWS, _WorldUvScale, _WorldUvOffset);

                //____________________________________________________________________________
                // 2 - Apply vertex displacement
                float snowDeformation = 0.0f;

                #if defined(GBUFFER_OUT) // || defined(SHADOW_OUT)
                {
                    // Sample snow deformation map
                    float2 snowUv = Snow_WorldToUv(positionWS);
                    snowDeformation = Snow_SampleDeformation(snowUv);

                    // Sample snow depth noise map
                    float snowDepthOffset = Snow_SampleDepthNoise(positionWS);

                    // Deduce current depth of the snow cover
                    float snowCoverDepth = (_SnowBaseDepth + snowDepthOffset) * 1e-2f;
                    snowCoverDepth *= saturate(1.f - snowDeformation);

                    // Displace vertex up to the current snow depth
                    positionWS += normalWS * snowCoverDepth;

                    #if defined(RECONSTRUCT_NORMALS)
                        // Reconstruct normal using finite difference
                        float3 normalTS = Snow_ReconstructNormal(snowUv);

                        // Convert to world space
                        normalWS = Utils_TanToWorld(normalTS, tangentWS, bitangentWS, normalWS);
                        normalWS = normalize(normalWS);
                    #endif
                }
                #elif defined(DEPTH_ONLY_OUT)
                {
                    // Just apply the base snow depth
                    float snowCoverDepth = _SnowBaseDepth * 1e-2f;
                    positionWS += normalWS * snowCoverDepth;
                }
                #endif

                //____________________________________________________________________________
                // 3 - Export varyings
                #if defined(GBUFFER_OUT)
                {
                    varyings.positionWS = positionWS;
                    varyings.normalWS = normalWS;
                    varyings.tangentWS  = tangentWS;
                    varyings.bitangentWS = bitangentWS;
                    varyings.deformation = snowDeformation;
                    varyings.worldUv = worldUv;

                    // Compute clip position
                    varyings.positionCS = TransformWorldToHClip(varyings.positionWS);

                    // Compute UVs for static lighting
                    #if defined(LIGHTMAP_ON)
                        OUTPUT_LIGHTMAP_UV(_attributes.staticLightmapUV, unity_LightmapST, varyings.staticLightmapUV);
                    #else
                        OUTPUT_SH(varyings.normalWS, varyings.vertexSH);
                    #endif

                    // Compute UVs for dynamic lighting
                    #if defined(DYNAMICLIGHTMAP_ON)
                        OUTPUT_LIGHTMAP_UV(_attributes.dynamicLightmapUV, unity_DynamicLightmapST, varyings.dynamicLightmapUV);
                    #endif

                    // Compute shadow coordinates
                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                        #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                            varyings.shadowCoord = ComputeScreenPos(varyings.positionCS);
                        #else
                            varyings.shadowCoord = TransformWorldToShadowCoord(varyings.positionWS);
                        #endif
                    #endif
                }
                #elif defined(SHADOW_OUT)
                {
                    // Apply shadow bias
                    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                        positionWS = ApplyShadowBias(positionWS, normalWS, normalize(_LightPosition - positionWS));
                    #else
                        positionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);
                    #endif

                    // Compute clip position
                    varyings.positionCS = TransformWorldToHClip(positionWS);

                    #if defined(UNITY_REVERSED_Z)
                        varyings.positionCS.z = min(varyings.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                    #else
                        varyings.positionCS.z = max(varyings.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                    #endif
                }
                #elif defined(DEPTH_ONLY_OUT)
                {
                    // Compute clip position
                    varyings.positionCS = TransformWorldToHClip(positionWS);
                }
                #endif

                return varyings;
            }

            ControlPoint VertexTessellation(Attributes _attributes)
            {
                ControlPoint controlPoint = (ControlPoint)0;

                UNITY_SETUP_INSTANCE_ID(_attributes);
                UNITY_TRANSFER_INSTANCE_ID(_attributes, controlPoint);

                controlPoint.position = _attributes.position;
                controlPoint.normal = _attributes.normal;
                controlPoint.tangent = _attributes.tangent;
                controlPoint.texCoord = _attributes.texCoord;

                #if defined(LIGHTMAP_ON)
                    controlPoint.staticLightmapUV = _attributes.staticLightmapUV;
                #endif

                #if defined(DYNAMICLIGHTMAP_ON)
                    controlPoint.dynamicLightmapUV = _attributes.dynamicLightmapUV;
                #endif

                return controlPoint;
            }

            /*********************************
            *     Tessellation shaders       *
            *********************************/
            TessellationFactors PatchConstantFunction(InputPatch<ControlPoint, 3> _patch)
            {
                // Compute distance-based tessellation factors
                float3 factors;
                factors.x = Utils_GetDistanceBasedTessellation(mul(unity_ObjectToWorld, _patch[0].position).xyz);
                factors.y = Utils_GetDistanceBasedTessellation(mul(unity_ObjectToWorld, _patch[1].position).xyz);
                factors.z = Utils_GetDistanceBasedTessellation(mul(unity_ObjectToWorld, _patch[2].position).xyz);

                // Scale the tessellation factor based on the transform scale
                float3 scale = Utils_GetTransformScale(unity_ObjectToWorld);
                factors *= max(max(scale.x, scale.y), scale.z);
                factors *= _TessellationFactor;

                // Deduce the final tessellation factors
                TessellationFactors tessFactors;
                tessFactors.edge[0] = 0.5f * (factors.y + factors.z);
                tessFactors.edge[1] = 0.5f * (factors.x + factors.z);
                tessFactors.edge[2] = 0.5f * (factors.x + factors.y);

                tessFactors.inside = (factors.x + factors.y + factors.z) / 3.0f;
                return tessFactors;
            }

            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("fractional_odd")]
            [patchconstantfunc("PatchConstantFunction")]
            ControlPoint HullTessellation(InputPatch<ControlPoint, 3> _patch, uint _id : SV_OutputControlPointID)
            {
                return _patch[_id];
            }

            [domain("tri")]
            Varyings DomainTessellation(TessellationFactors _factors, OutputPatch<ControlPoint, 3> _patch, float3 _barycentricCoord : SV_DomainLocation)
            {
                Attributes attributes = (Attributes)0;
                UNITY_TRANSFER_INSTANCE_ID(_patch[0], attributes);

                #define Interpolate(fieldName) attributes.fieldName =   \
                            _patch[0].fieldName * _barycentricCoord.x + \
                            _patch[1].fieldName * _barycentricCoord.y + \
                            _patch[2].fieldName * _barycentricCoord.z;  \

                Interpolate(position)
                Interpolate(texCoord)

                Interpolate(normal)
                attributes.normal = normalize(attributes.normal);

                Interpolate(tangent)
                attributes.tangent = normalize(attributes.tangent);

                #if defined(LIGHTMAP_ON)
                    Interpolate(staticLightmapUV);
                #endif

                #if defined(DYNAMICLIGHTMAP_ON)
                    Interpolate(dynamicLightmapUV);
                #endif

                return VertexDefault(attributes);
            }

            /*********************************
            *        Fragment shaders        *
            *********************************/
            half4 FragmentDefault(Varyings _varyings) : SV_TARGET
            {
                return 0;
            }

            #if defined(GBUFFER_OUT)
                FragmentOutput FragmentGBuffer(Varyings _varyings)
                {
                    UNITY_SETUP_INSTANCE_ID(_varyings);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(_varyings);

                    //____________________________________________________________________________
                    // Re-normalize interpolated word directions
                    float3 tangentWS = SafeNormalize(_varyings.tangentWS);
                    float3 bitangentWS = SafeNormalize(_varyings.bitangentWS);
                    float3 normalWS = SafeNormalize(_varyings.normalWS);

                    //____________________________________________________________________________
                    // Compute surface data
                    SurfaceData surfaceData = (SurfaceData)0;
                    {
                        // tint the snow tracks using multiply blending
                        float3 albedo = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, _varyings.worldUv).rgb;
                        albedo = lerp(albedo, _SnowTrackTint.rgb * albedo, _varyings.deformation);
                        surfaceData.albedo = albedo;

                        // apply normal mapping
                        float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, _varyings.worldUv));
                        normalWS = Utils_TanToWorld(normalTS, tangentWS, bitangentWS, normalWS);
                        surfaceData.normalTS = normalTS;

                        // sample PBR rough map
                        float roughness = SAMPLE_TEXTURE2D(_RoughMap, sampler_RoughMap, _varyings.worldUv).r;
                        surfaceData.smoothness = 1.f - roughness;

                        // sample baked cavity map
                        float cavity = SAMPLE_TEXTURE2D(_CavityMap, sampler_CavityMap, _varyings.worldUv).r;
                        surfaceData.occlusion = cavity;

                        // no metallic / emissive support
                        surfaceData.emission = half3(0, 0, 0);
                        surfaceData.specular = half3(0, 0, 0);
                        surfaceData.metallic = 0.0;
                        surfaceData.alpha = 1.0;
                    }

                    //____________________________________________________________________________
                    // Compute input data
                    InputData inputData = (InputData)0;
                    {
                        inputData.positionCS = _varyings.positionCS;
                        inputData.positionWS = _varyings.positionWS;
                        inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(_varyings.positionWS);
                        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(_varyings.positionCS);
                        inputData.normalWS = normalWS;

                        // shadows
                        #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                            inputData.shadowCoord = _varyings.shadowCoord;
                        #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                            inputData.shadowCoord = TransformWorldToShadowCoord(_varyings.positionWS);
                        #else
                            inputData.shadowCoord = float4(0, 0, 0, 0);
                        #endif

                        // static light maps
                        #if defined(LIGHTMAP_ON)
                            inputData.shadowMask = SAMPLE_SHADOWMASK(_varyings.staticLightmapUV);
                        #else
                            inputData.shadowMask = float4(0, 0, 0, 0);
                        #endif

                        // dynamic light maps
                        #if defined(DYNAMICLIGHTMAP_ON)
                            inputData.bakedGI = SAMPLE_GI(_varyings.staticLightmapUV, _varyings.dynamicLightmapUV, _varyings.vertexSH, normalWS);
                        #else
                            inputData.bakedGI = SAMPLE_GI(_varyings.staticLightmapUV, _varyings.vertexSH, normalWS);
                        #endif

                        // no vertex lighting support
                        inputData.vertexLighting = half3(0, 0, 0);

                        // no fog in the gbuffer pass
                        inputData.fogCoord = 0;
                    }

                    //____________________________________________________________________________
                    // Compute BRDF data
                    BRDFData brdfData;
                    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                    //____________________________________________________________________________
                    // Compute Global Illumination
                    half3 GIColor = half3(0, 0, 0);
                    {
                        Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                        MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
                        GIColor = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);
                    }

                    //____________________________________________________________________________
                    // Write to G-Buffer
                    return BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + GIColor, surfaceData.occlusion);
                }
            #endif

        ENDHLSL

        // GBUFFER PASS
        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "UniversalGBuffer" }

            // -------------------------------------
            // Fixed states
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
                #pragma target 4.5

                // Deferred Rendering Path does not support the OpenGL-based graphics API
                #pragma exclude_renderers gles3 glcore

                // Tessellation control and evaluation stages are used in the G-Buffer pass
                #pragma require tessellation tessHW

                // -------------------------------------
                // Shader Stages
                // #pragma vertex VertexDefault
                #pragma vertex VertexTessellation
                #pragma hull HullTessellation
                #pragma domain DomainTessellation
                #pragma fragment FragmentGBuffer

                // -------------------------------------
                // Unity keywords
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
                #pragma multi_compile _ SHADOWS_SHADOWMASK
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

                // -------------------------------------
                // Universal Pipeline keywords
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile_fragment _ _SHADOWS_SOFT

                // -------------------------------------
                // Custom keywords
                #pragma multi_compile GBUFFER_OUT
                #pragma multi_compile_vertex _ RECONSTRUCT_NORMALS
                #pragma multi_compile_domain _ RECONSTRUCT_NORMALS
            ENDHLSL
        }

        // SHADOW PASS
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            // -------------------------------------
            // Fixed states
            ZWrite On
            ZTest LEqual
            Cull Back
            ColorMask 0

            HLSLPROGRAM
                #pragma target 2.0

                // -------------------------------------
                // Shader Stages
                #pragma vertex VertexDefault
                #pragma fragment FragmentDefault

                // -------------------------------------
                // Unity keywords
                #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

                // -------------------------------------
                // Custom keywords
                #pragma multi_compile SHADOW_OUT
            ENDHLSL
        }

        // DEPTH PASS
        Pass
        {
            Name "TerrainDepth"
            Tags { "LightMode" = "TerrainDepth" }

            // -------------------------------------
            // Fixed states
            ZWrite On
            ZTest LEqual
            Cull Back
            ColorMask 0

            HLSLPROGRAM
                #pragma target 2.0

                // -------------------------------------
                // Shader Stages
                #pragma vertex VertexDefault
                #pragma fragment FragmentDefault

                // -------------------------------------
                // Custom keywords
                #pragma multi_compile DEPTH_ONLY_OUT
            ENDHLSL
        }
    }

    Fallback  "Hidden/Universal Render Pipeline/FallbackError"
}