// Translucent, unlit tint for the building preview: it must read the same in a dark room
// as in daylight, so it deliberately ignores scene lighting (and the toon shading).
Shader "Game/BuildGhost"
{
    Properties
    {
        [MainColor] _Color ("Color", Color) = (0.2, 1, 0.3, 0.4)
        _FresnelPower ("Edge Highlight Power", Range(0.5, 8)) = 3
        _FresnelStrength ("Edge Highlight Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _FresnelPower;
                half _FresnelStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 viewDir = normalize(input.viewDirWS);
                half fresnel = pow(1.0h - saturate(dot(normal, viewDir)), _FresnelPower);

                half3 color = lerp(_Color.rgb, half3(1, 1, 1), fresnel * _FresnelStrength);
                half alpha = saturate(_Color.a + fresnel * _FresnelStrength * 0.5h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
