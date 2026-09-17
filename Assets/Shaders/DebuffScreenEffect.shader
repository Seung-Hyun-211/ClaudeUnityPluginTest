// Single unified fullscreen post-process shader for all status-effect (debuff) screen
// overlays. Docs/기획문서_디버프스크린셰이더설계.md — driven entirely by two composited
// ScreenEffectParams-shaped layers (Perceptual / DamageFeedback, 3.1절) supplied by
// Game.Rendering.DebuffScreenEffectFeature. Adding a new debuff must never require a
// new shader (1장 설계 원칙) — only new contribution values pushed from C#.
//
// Two passes, blitted sequentially by DebuffScreenEffectPass.cs (source -> temp -> dest):
// Pass 0 "PerceptualLayer" applies unmasked, full-screen (3.1절: 중앙 포함).
// Pass 1 "DamageFeedbackLayer" reads Pass 0's output and applies its own effect only
// outside the RadialMask center safe zone, so it genuinely layers on top of the
// Perceptual result rather than re-deriving from the untouched source (3.1절 마지막 문단:
// "두 레이어를 순차적으로 화면에 얹는다").
Shader "Hidden/Game/DebuffScreenEffect"
{
    Properties
    {
        _BlitTexture("Source", 2D) = "white" {}
        _OverlayTex("Damage Feedback Overlay (4.4)", 2D) = "black" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    CBUFFER_START(UnityPerMaterial)
        // ---- Perceptual 레이어 (3.1절: 화면 전체, 중앙 포함) ----
        float4 _PerceptualVignetteColor;
        float _PerceptualVignetteIntensity;
        float _PerceptualDesaturation;
        float4 _PerceptualColorTint; // .a = 적용 강도(합성 시 severity로 채움)
        float _PerceptualBlurAmount;
        float _PerceptualChromaticAberration;
        float _PerceptualDistortion;
        float _PerceptualPulseFrequency;
        float _PerceptualPulseAmplitude;

        // ---- DamageFeedback 레이어 (3.1절: 중앙 안전 영역 제외, 가장자리 링만) ----
        float4 _DamageFeedbackVignetteColor;
        float _DamageFeedbackVignetteIntensity;
        float _DamageFeedbackDesaturation;
        float4 _DamageFeedbackColorTint;
        float _DamageFeedbackBlurAmount;
        float _DamageFeedbackChromaticAberration;
        float _DamageFeedbackDistortion;
        float _DamageFeedbackPulseFrequency;
        float _DamageFeedbackPulseAmplitude;
        float _DamageFeedbackOverlayAlpha; // 4.4절: 알파는 vignetteIntensity에 연동

        // 3.1절: 전역 상수로 고정된 반경 마스크 파라미터(디버프마다 다르게 두지 않음).
        float _InnerSafeRadius;
        float _OuterSafeRadius;
    CBUFFER_END

    TEXTURE2D(_OverlayTex);
    SAMPLER(sampler_OverlayTex);

    #define TAU 6.28318530718

    // 3.1절 RadialMask — 화면 짧은 변 기준으로 정규화된 중심 거리. 0 = 중앙 안전 영역(완전 클리어).
    float RadialMask(float2 uv)
    {
        float aspect = _ScreenParams.x / max(_ScreenParams.y, 1e-5);
        float2 centered = (uv - 0.5) * 2.0;
        if (aspect >= 1.0)
        {
            centered.x *= aspect;
        }
        else
        {
            centered.y /= aspect;
        }
        float dist = length(centered) * 0.5;
        return smoothstep(_InnerSafeRadius, _OuterSafeRadius, dist);
    }

    // 4.1/4.2절: 심장박동처럼 주기적으로 강도가 맥동.
    float ApplyPulse(float baseValue, float frequency, float amplitude)
    {
        float pulse = 1.0 + sin(_Time.y * frequency * TAU) * amplitude;
        return max(baseValue * pulse, 0.0);
    }

    // 값싼 원형(Poisson-ish) 8탭 블러 근사 — 단일 패스 제약 하의 단순화(후속 권장:
    // 실제 프로덕션 품질을 위해서는 다운샘플+분리형 가우시안 2패스 권장).
    float3 SampleBlurred(float2 uv, float blurAmount)
    {
        float3 center = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
        if (blurAmount <= 0.0001)
        {
            return center;
        }

        float2 texel = _BlitTexture_TexelSize.xy * (1.0 + blurAmount * 12.0);
        float3 sum = center;
        const float2 kOffsets[8] = {
            float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1),
            float2(0.707, 0.707), float2(-0.707, 0.707), float2(0.707, -0.707), float2(-0.707, -0.707)
        };
        UNITY_UNROLL
        for (int i = 0; i < 8; i++)
        {
            sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + kOffsets[i] * texel).rgb;
        }
        return sum / 9.0;
    }

    // 4.2절: 색수차 — 중심에서 멀어질수록 RGB 채널을 어긋나게 샘플링. 입력은 이미 블러된 UV 결과 위에 얹는다.
    float3 ApplyChromaticAberration(float3 baseColor, float2 uv, float amount)
    {
        if (amount <= 0.0001)
        {
            return baseColor;
        }
        float2 dir = uv - 0.5;
        float2 offset = dir * amount * 0.02;
        float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset).r;
        float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset).b;
        return float3(r, baseColor.g, b);
    }

    // 4.2절: "핑 도는" 느낌을 이중시야 대신 저비용 사인파 UV 왜곡으로 대체.
    float2 ApplyDistortion(float2 uv, float amount)
    {
        if (amount <= 0.0001)
        {
            return uv;
        }
        float2 wave;
        wave.x = sin(uv.y * 18.0 + _Time.y * 3.0) * amount * 0.01;
        wave.y = sin(uv.x * 18.0 + _Time.y * 2.6) * amount * 0.01;
        return uv + wave;
    }

    float3 ApplyVignette(float3 color, float2 uv, float mask, float3 vignetteColor, float intensity)
    {
        float edge = distance(uv, float2(0.5, 0.5)) * 2.0;
        float vign = smoothstep(0.4, 1.2, edge) * saturate(intensity) * mask;
        return lerp(color, vignetteColor, saturate(vign));
    }

    float3 ApplyDesaturation(float3 color, float amount)
    {
        float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
        return lerp(color, luma.xxx, saturate(amount));
    }

    float3 ApplyColorTint(float3 color, float4 tint, float mask)
    {
        return lerp(color, tint.rgb, saturate(tint.a) * mask);
    }

    // 한 레이어의 전체 파라미터 세트를 입력 텍스처(_BlitTexture, 이전 패스의 출력) 위에 적용한다.
    // mask=1이면 전체 적용(Perceptual), RadialMask 결과를 넘기면 가장자리에만 적용(DamageFeedback, 3.1절).
    float3 ApplyLayer(
        float2 uv, float mask,
        float vignetteIntensity, float3 vignetteColor,
        float desaturation, float4 colorTint,
        float blurAmount, float chromaticAberration, float distortion,
        float pulseFrequency, float pulseAmplitude)
    {
        float pulsedVignette = ApplyPulse(vignetteIntensity, pulseFrequency, pulseAmplitude) * mask;
        float maskedBlur = blurAmount * mask;
        float maskedAberration = chromaticAberration * mask;
        float maskedDistortion = distortion * mask;
        float maskedDesaturation = desaturation * mask;

        float2 distortedUV = ApplyDistortion(uv, maskedDistortion);
        float3 color = SampleBlurred(distortedUV, maskedBlur);
        color = ApplyChromaticAberration(color, distortedUV, maskedAberration);
        color = ApplyDesaturation(color, maskedDesaturation);
        color = ApplyColorTint(color, colorTint, mask);
        color = ApplyVignette(color, uv, mask, vignetteColor, pulsedVignette);
        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "PerceptualLayer"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float3 color = ApplyLayer(
                    input.texcoord, 1.0,
                    _PerceptualVignetteIntensity, _PerceptualVignetteColor.rgb,
                    _PerceptualDesaturation, _PerceptualColorTint,
                    _PerceptualBlurAmount, _PerceptualChromaticAberration, _PerceptualDistortion,
                    _PerceptualPulseFrequency, _PerceptualPulseAmplitude);
                return float4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DamageFeedbackLayer"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float mask = RadialMask(uv);

                float3 color = ApplyLayer(
                    uv, mask,
                    _DamageFeedbackVignetteIntensity, _DamageFeedbackVignetteColor.rgb,
                    _DamageFeedbackDesaturation, _DamageFeedbackColorTint,
                    _DamageFeedbackBlurAmount, _DamageFeedbackChromaticAberration, _DamageFeedbackDistortion,
                    _DamageFeedbackPulseFrequency, _DamageFeedbackPulseAmplitude);

                // 4.4절: 출혈 전용 핏방울 오버레이 — DamageFeedback 레이어 소속이므로 동일한 반경 마스크 적용.
                float2 overlayUV = uv + float2(0, _Time.y * 0.05);
                float4 overlaySample = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, overlayUV);
                float overlayAlpha = overlaySample.a * _DamageFeedbackOverlayAlpha * mask;
                color = lerp(color, overlaySample.rgb, saturate(overlayAlpha));

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
