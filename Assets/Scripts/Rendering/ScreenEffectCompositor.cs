using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// Docs/기획문서_디버프스크린셰이더설계.md 1장/5장 — "기여자(Contributor)" 모델.
    /// 디버프 게임플레이 시스템(별도 트랙)은 이 클래스의 SetContribution만 호출하면 되고,
    /// 실제 채널별 합성 규칙(3.2절)과 카테고리별 레이어 분리(3.1절)는 여기서 전담한다.
    /// 디버프의 스택/지속시간/해제 조건 자체는 범위 밖 — 호출자가 이미 평가한 최종
    /// ScreenEffectParams 값을 넘겨준다고 가정한다.
    /// </summary>
    public class ScreenEffectCompositor
    {
        private readonly Dictionary<string, ScreenEffectParams> contributions = new();

        /// <summary>
        /// 기여자 하나의 현재 값을 등록/갱신한다. 같은 sourceId로 다시 호출하면 값을 덮어쓴다
        /// — 디버프 스택/중증도 변화나 시간 경과에 따른 재평가를 이 호출 하나로 반영한다.
        /// </summary>
        public void SetContribution(string sourceId, ScreenEffectParams value)
        {
            contributions[sourceId] = value;
        }

        /// <summary>디버프가 해제되면 호출 — 해당 기여자를 완전히 제거한다.</summary>
        public void ClearContribution(string sourceId)
        {
            contributions.Remove(sourceId);
        }

        public ScreenEffectParams CompositePerceptual()
        {
            return Composite(EffectCategory.Perceptual);
        }

        public ScreenEffectParams CompositeDamageFeedback()
        {
            return Composite(EffectCategory.DamageFeedback);
        }

        private ScreenEffectParams Composite(EffectCategory category)
        {
            var result = ScreenEffectParams.Default;
            result.effectCategory = category;

            float vignetteIntensitySum = 0f;
            float chromaticAberrationSum = 0f;
            float blurMax = 0f, blurSecond = 0f;
            float distortionMax = 0f, distortionSecond = 0f;
            float desaturationMax = 0f;

            float tintWeightSum = 0f;
            Vector4 tintWeighted = Vector4.zero;
            Vector4 vignetteColorWeighted = Vector4.zero;

            float strongestSeverity = -1f;
            float pulseFrequency = 0f, pulseAmplitude = 0f;
            Texture overlayTexture = null;

            bool any = false;

            foreach (var contribution in contributions.Values)
            {
                if (contribution.effectCategory != category)
                {
                    continue;
                }

                any = true;

                // vignetteIntensity, chromaticAberration: 합산 후 클램프(3.2절).
                vignetteIntensitySum += contribution.vignetteIntensity;
                chromaticAberrationSum += contribution.chromaticAberration;

                // blurAmount, distortion: max(a,b) + min(a,b)*0.25 — 전체 집합에서는
                // "최댓값 + 두 번째로 큰 값의 25%"로 등가 계산한다(3.2절 표와 동일한 결과,
                // 기여자가 3개 이상일 때도 순서에 무관하게 안정적으로 계산하기 위한 구현 선택).
                UpdateTopTwo(contribution.blurAmount, ref blurMax, ref blurSecond);
                UpdateTopTwo(contribution.distortion, ref distortionMax, ref distortionSecond);

                // desaturation: 최댓값(3.2절).
                desaturationMax = Mathf.Max(desaturationMax, contribution.desaturation);

                // colorTint, vignetteColor: 강도(Severity) 가중 평균(3.2절).
                float weight = Mathf.Max(contribution.Severity, 0.0001f);
                tintWeightSum += weight;
                tintWeighted += ToWeightedVector(contribution.colorTint, weight);
                vignetteColorWeighted += ToWeightedVector(contribution.vignetteColor, weight);

                // pulseFrequency(및 pulseAmplitude): 가장 강한(Severity 최대) 기여자의 값 채택(3.2절).
                if (contribution.Severity > strongestSeverity)
                {
                    strongestSeverity = contribution.Severity;
                    pulseFrequency = contribution.pulseFrequency;
                    pulseAmplitude = contribution.pulseAmplitude;
                }

                // 4.4절: 커스텀 오버레이 텍스처는 공용 채널이 아니므로, 가장 심각한 기여자의
                // 것 하나만 채택한다(문서가 다중 오버레이 합성 규칙을 규정하지 않음 — 이 트랙의
                // 판단으로 단순화).
                if (contribution.overlayTexture != null && contribution.Severity >= strongestSeverity)
                {
                    overlayTexture = contribution.overlayTexture;
                }
            }

            if (!any)
            {
                return result;
            }

            result.vignetteIntensity = Mathf.Clamp01(vignetteIntensitySum);
            result.chromaticAberration = Mathf.Clamp01(chromaticAberrationSum);
            result.blurAmount = Mathf.Clamp01(blurMax + blurSecond * 0.25f);
            result.distortion = Mathf.Clamp01(distortionMax + distortionSecond * 0.25f);
            result.desaturation = Mathf.Clamp01(desaturationMax);
            result.colorTint = FromWeightedVector(tintWeighted, tintWeightSum);
            result.vignetteColor = FromWeightedVector(vignetteColorWeighted, tintWeightSum);
            result.pulseFrequency = pulseFrequency;
            result.pulseAmplitude = pulseAmplitude;
            result.overlayTexture = overlayTexture;
            return result;
        }

        private static void UpdateTopTwo(float value, ref float max, ref float second)
        {
            if (value >= max)
            {
                second = max;
                max = value;
            }
            else if (value > second)
            {
                second = value;
            }
        }

        private static Vector4 ToWeightedVector(Color color, float weight)
        {
            return new Vector4(color.r, color.g, color.b, color.a) * weight;
        }

        private static Color FromWeightedVector(Vector4 weightedSum, float weightSum)
        {
            if (weightSum <= 0f)
            {
                return Color.clear;
            }
            var average = weightedSum / weightSum;
            return new Color(average.x, average.y, average.z, average.w);
        }
    }
}
