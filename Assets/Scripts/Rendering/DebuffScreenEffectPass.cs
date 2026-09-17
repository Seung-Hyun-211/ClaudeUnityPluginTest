using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Game.Rendering
{
    /// <summary>
    /// Render Graph fullscreen blit pass for DebuffScreenEffect.shader. Unity 6 / URP 17
    /// renders exclusively through Render Graph, so this uses RenderGraphUtils.AddBlitPass
    /// rather than the legacy Blit(CommandBuffer, ...) API (see Assets/Settings/UniversalRP.asset
    /// — this project is on com.unity.render-pipelines.universal 17.6.0).
    /// Single responsibility: push already-composited params onto the material and blit.
    /// Composition itself lives in ScreenEffectCompositor.
    /// </summary>
    internal sealed class DebuffScreenEffectPass : ScriptableRenderPass
    {
        private readonly Material material;
        private ScreenEffectParams perceptual;
        private ScreenEffectParams damageFeedback;
        private float innerSafeRadius;
        private float outerSafeRadius;

        private static readonly int InnerSafeRadiusId = Shader.PropertyToID("_InnerSafeRadius");
        private static readonly int OuterSafeRadiusId = Shader.PropertyToID("_OuterSafeRadius");
        private static readonly int OverlayTexId = Shader.PropertyToID("_OverlayTex");
        private static readonly int DamageFeedbackOverlayAlphaId = Shader.PropertyToID("_DamageFeedbackOverlayAlpha");

        public DebuffScreenEffectPass(Material material)
        {
            this.material = material;
        }

        public void Setup(ScreenEffectParams perceptualLayer, ScreenEffectParams damageFeedbackLayer, float innerSafeRadius, float outerSafeRadius)
        {
            perceptual = perceptualLayer;
            damageFeedback = damageFeedbackLayer;
            this.innerSafeRadius = innerSafeRadius;
            this.outerSafeRadius = outerSafeRadius;
        }

        private void ApplyParamsToMaterial()
        {
            perceptual.ApplyToMaterial(material, "_Perceptual");
            damageFeedback.ApplyToMaterial(material, "_DamageFeedback");

            material.SetFloat(InnerSafeRadiusId, innerSafeRadius);
            material.SetFloat(OuterSafeRadiusId, outerSafeRadius);

            // 4.4절: 핏방울 등 커스텀 오버레이는 DamageFeedback 레이어에만 존재하고,
            // 알파는 vignetteIntensity에 연동한다.
            if (damageFeedback.overlayTexture != null)
            {
                material.SetTexture(OverlayTexId, damageFeedback.overlayTexture);
                material.SetFloat(DamageFeedbackOverlayAlphaId, damageFeedback.vignetteIntensity);
            }
            else
            {
                material.SetFloat(DamageFeedbackOverlayAlphaId, 0f);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
            {
                return;
            }

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                // 백버퍼는 직접 읽고 쓸 수 없다 — 이 프레임은 건너뛴다(드문 카메라 스택 구성에서만 발생).
                return;
            }

            ApplyParamsToMaterial();

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc textureDesc = renderGraph.GetTextureDesc(source);
            textureDesc.clearBuffer = false;

            // 문서 3.1절: Perceptual 레이어를 먼저 전체 화면에 적용한 뒤(패스 0), 그 결과 위에
            // DamageFeedback 레이어를 RadialMask로 가장자리에만 얹는다(패스 1) — 두 패스를
            // 순차 블릿해야 DamageFeedback의 블러/색수차가 Perceptual의 결과 위에서 계산된다.
            textureDesc.name = "_DebuffPerceptualTarget";
            TextureHandle perceptualTarget = renderGraph.CreateTexture(textureDesc);
            RenderGraphUtils.BlitMaterialParameters perceptualBlit = new(source, perceptualTarget, material, 0);
            renderGraph.AddBlitPass(perceptualBlit, "Debuff Screen Effect - Perceptual");

            textureDesc.name = "_DebuffDamageFeedbackTarget";
            TextureHandle finalTarget = renderGraph.CreateTexture(textureDesc);
            RenderGraphUtils.BlitMaterialParameters damageFeedbackBlit = new(perceptualTarget, finalTarget, material, 1);
            renderGraph.AddBlitPass(damageFeedbackBlit, "Debuff Screen Effect - DamageFeedback");

            resourceData.cameraColor = finalTarget;
        }
    }
}
