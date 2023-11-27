Shader "Skybox/SkyboxGradient"
{
    Properties
    {
        _SkyColor ("", Color) = (0,0,0,0)
        _FogColor ("", Color) = (0,0,0,0)

        _ColorBlending ("Color blending", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "QUEUE"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        LOD 0
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // -----------------------------------------------------------------
            // Vertex Program

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            // -----------------------------------------------------------------
            // Fragment Program

            float _ColorBlending;

            float4 _SkyColor;
            float4 _FogColor;

            // source: https://stackoverflow.com/questions/3451553/value-remapping
            float remap(float value, float low1, float high1, float low2, float high2)
            {
                return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // color gradient for the sky
                float y = normalize(i.worldPos).y;
                y = remap(y, -1, 1, 0, 1);
                y = pow(y, _ColorBlending);
                float3 skyColor = lerp(_FogColor, _SkyColor, y);

                return fixed4(skyColor, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
