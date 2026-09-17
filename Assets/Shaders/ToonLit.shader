// Example Lit-replacement shader for characters, built on the shared
// ToonHatchLighting.hlsl include (Docs/기획문서_셀셰이딩그래픽설계.md).
// Hand-written HLSL instead of Shader Graph so it stays fully text-reviewable.
Shader "Game/ToonLit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Hatching)]
        _HatchColorScale("Hatch Color Scale", Range(0, 1)) = 0.15
        _HatchThreshold("Hatch Thresholds (L1,L2,L3,L4)", Vector) = (0.25, 0.5, 0.75, 0.9)
        _HatchAngle("Hatch Angles Degrees (L1,L2,L3,L4)", Vector) = (45, -45, 0, 90)
        _HatchFrequency("Hatch Frequencies (L1,L2,L3,L4)", Vector) = (20, 20, 20, 20)
        _HatchThickness("Hatch Line Thickness", Range(0, 1)) = 0.5

        [Header(Hatch Coordinate Space)]
        [Toggle(_USE_TRIPLANAR)] _UseTriplanar("Use Triplanar (env/mob), off = UV2 (hero char)", Float) = 0
        _TriplanarScale("Triplanar Scale", Float) = 1.0

        [Header(Rim)]
        [Toggle(_USE_RIM)] _UseRim("Enable Rim", Float) = 0
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.6
        _RimAA("Rim Softness", Range(0.001, 0.5)) = 0.05

        [HideInInspector] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature_local _USE_TRIPLANAR
            #pragma shader_feature_local _USE_RIM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Include/ToonHatchLighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _HatchColorScale;
                float4 _HatchThreshold;
                float4 _HatchAngle;
                float4 _HatchFrequency;
                float _HatchThickness;
                float _TriplanarScale;
                float4 _RimColor;
                float _RimThreshold;
                float _RimAA;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                // 4.3절: 히어로 캐릭터는 UV2를 해칭 전용 좌표로 사용.
                float2 hatchUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 hatchUV : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.worldPos = positions.positionWS;
                output.worldNormal = normals.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.hatchUV = input.hatchUV;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 worldNormal = normalize(input.worldNormal);
                float3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;

                float3 shaded = ToonHatchLighting(
                    baseColor,
                    input.worldPos,
                    worldNormal,
                    input.hatchUV,
                    #if defined(_USE_TRIPLANAR)
                        true,
                    #else
                        false,
                    #endif
                    _TriplanarScale,
                    1.0, // ambientOcclusion — 이 예시 셰이더는 AO 맵을 사용하지 않음(선택 사항, 3.1절)
                    _HatchThreshold,
                    _HatchAngle,
                    _HatchFrequency,
                    _HatchThickness,
                    _HatchColorScale);

                #if defined(_USE_RIM)
                    float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.worldPos));
                    float rim = ComputeToonRim(worldNormal, viewDirWS, _RimThreshold, _RimAA);
                    shaded = lerp(shaded, _RimColor.rgb, rim * _RimColor.a);
                #endif

                return float4(shaded, _BaseColor.a);
            }
            ENDHLSL
        }

        // 그림자 캐스팅 — 해칭 실루엣이 아니라 표준 URP 셰도우 캐스터로 충분(1.4절: 실루엣 가독성은
        // 라이트 밴드/해칭이 아니라 외곽 형태로 결정되므로 별도 커스텀 캐스터가 필요 없음).
        // Unity 표준 Lit 셰이더의 LitInput.hlsl(별도 프로퍼티 다수 요구)에 의존하지 않고,
        // ShadowCasterPass.hlsl과 동일한 최소 로직을 직접 작성해 이 셰이더의 프로퍼티만으로 완결시킨다.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowCasterPositionCS(float3 positionOS, float3 normalOS)
            {
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowCasterPositionCS(input.positionOS, input.normalOS);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // 깊이 텍스처(디버프 스크린 셰이더 등 포스트 프로세싱이 필요로 할 수 있음).
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
