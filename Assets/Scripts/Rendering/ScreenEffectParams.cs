using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// Docs/기획문서_디버프스크린셰이더설계.md 2장 — 모든 디버프가 기여하는 공통 파라미터 세트.
    /// 새 디버프를 추가할 때 새 셰이더가 아니라 이 값을 채운 기여(Contribution) 하나만 있으면
    /// 된다(1장 설계 원칙). 순수 데이터 구조체 — 합성 규칙은 ScreenEffectCompositor가 담당한다.
    /// </summary>
    [System.Serializable]
    public struct ScreenEffectParams
    {
        public EffectCategory effectCategory;

        public Color vignetteColor;
        [Range(0f, 1f)] public float vignetteIntensity;
        [Range(0f, 1f)] public float desaturation;

        /// <summary>rgb = 색 보정, a = 화면에 얹는 강도(3.2절 가중평균 시 이 강도로 재계산됨).</summary>
        public Color colorTint;

        [Range(0f, 1f)] public float blurAmount;
        [Range(0f, 1f)] public float chromaticAberration;
        [Range(0f, 1f)] public float distortion;

        public float pulseFrequency;
        [Range(0f, 1f)] public float pulseAmplitude;

        /// <summary>
        /// 4.4절: 출혈처럼 디버프 전용 커스텀 오버레이 텍스처(공용 파라미터 세트에 넣지 않고
        /// 디버프별 1개 슬롯으로 예외 허용). 이 트랙의 API는 단일 SetContribution 호출만
        /// 노출하므로 편의상 같은 구조체에 함께 싣되, 합성기는 이를 공통 채널처럼 섞지 않고
        /// DamageFeedback 레이어에서 severity가 가장 큰 기여자의 것 하나만 채택한다.
        /// </summary>
        public Texture overlayTexture;

        public static ScreenEffectParams Default => new ScreenEffectParams
        {
            effectCategory = EffectCategory.Perceptual,
            vignetteColor = Color.clear,
            vignetteIntensity = 0f,
            desaturation = 0f,
            colorTint = Color.clear,
            blurAmount = 0f,
            chromaticAberration = 0f,
            distortion = 0f,
            pulseFrequency = 0f,
            pulseAmplitude = 0f,
            overlayTexture = null
        };

        /// <summary>
        /// 3.2절 합성 규칙(가중평균/최강자 선택)에 쓰이는 이 기여의 "심각도". 문서 2장의
        /// 파라미터 표에는 별도 강도 채널이 없으므로, 실제로 화면에 드러나는 채널들 중
        /// 최댓값을 심각도로 사용한다 — 이미 평가된(evaluated) 최종 값만 받는 이 API 특성상
        /// 합리적인 대체 지표.
        /// </summary>
        public readonly float Severity => Mathf.Max(vignetteIntensity, Mathf.Max(desaturation, Mathf.Max(blurAmount, Mathf.Max(chromaticAberration, distortion))));

        /// <summary>7장: 접근성 전역 강도 배율. 합성 결과 전체(5장)에 곱해진다.</summary>
        public readonly ScreenEffectParams ApplyGlobalAccessibilityScale(float scale)
        {
            scale = Mathf.Clamp01(scale);
            var result = this;
            result.vignetteIntensity *= scale;
            result.desaturation *= scale;
            result.blurAmount *= scale;
            result.chromaticAberration *= scale;
            result.distortion *= scale;
            result.pulseAmplitude *= scale;
            result.colorTint.a *= scale;
            return result;
        }

        /// <summary>이 레이어 값을 셰이더의 `_{prefix}...` 프로퍼티에 바인딩한다(DebuffScreenEffect.shader).</summary>
        public readonly void ApplyToMaterial(Material material, string prefix)
        {
            material.SetColor(prefix + "VignetteColor", vignetteColor);
            material.SetFloat(prefix + "VignetteIntensity", vignetteIntensity);
            material.SetFloat(prefix + "Desaturation", desaturation);
            material.SetColor(prefix + "ColorTint", colorTint);
            material.SetFloat(prefix + "BlurAmount", blurAmount);
            material.SetFloat(prefix + "ChromaticAberration", chromaticAberration);
            material.SetFloat(prefix + "Distortion", distortion);
            material.SetFloat(prefix + "PulseFrequency", pulseFrequency);
            material.SetFloat(prefix + "PulseAmplitude", pulseAmplitude);
        }
    }
}
