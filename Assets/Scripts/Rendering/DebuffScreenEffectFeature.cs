using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Rendering
{
    /// <summary>
    /// Docs/기획문서_디버프스크린셰이더설계.md 6장 — 단일 통합 풀스크린 디버프 이펙트를
    /// 삽입하는 Renderer Feature. AfterRenderingPostProcessing에 삽입해 URP 기본 포스트
    /// 프로세싱 이후 · UI 렌더링 이전에 실행되도록 한다. Base Camera에서만 동작해야 하며
    /// (Overlay UI 카메라에는 절대 부착 금지, 1.3절/6장), 이 클래스는 UniversalAdditionalCameraData
    /// 의 renderType으로 그 경계를 코드로도 한 번 더 강제한다.
    ///
    /// 디버프 게임플레이 시스템(별도 트랙)은 이 기능이 켜져 있는 동안 언제든
    /// <see cref="Compositor"/>.SetContribution(sourceId, value) / ClearContribution(sourceId)
    /// 을 호출해 화면 이펙트에 기여하면 된다 — 새 디버프 추가에 새 셰이더/새 Renderer Feature가
    /// 필요하지 않다(1장 설계 원칙).
    /// </summary>
    public class DebuffScreenEffectFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader shader;
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        [Header("Accessibility (7장)")]
        [SerializeField, Range(0f, 1f)] private float globalEffectStrength = 1f;

        [Header("Center Safe Zone (3.1절 — 전역 상수, 디버프별로 다르게 두지 않음)")]
        [SerializeField, Range(0f, 1f)] private float innerSafeRadius = 0.3f;
        [SerializeField, Range(0f, 1f)] private float outerSafeRadius = 0.6f;

        private Material material;
        private DebuffScreenEffectPass pass;

        /// <summary>
        /// 디버프 기여자(Contributor)들이 값을 밀어넣는 진입점. Renderer Feature는 URP가
        /// 소유/생성하므로 씬에 배치된 MonoBehaviour가 아니라 정적 접근을 제공한다 — 이 기능이
        /// 렌더러에 붙어있지 않으면(Create() 미호출) null일 수 있으니 호출측에서 널 체크 권장.
        /// </summary>
        public static ScreenEffectCompositor Compositor { get; private set; }

        public override void Create()
        {
            Compositor ??= new ScreenEffectCompositor();

            if (shader == null)
            {
                pass = null;
                return;
            }

            material = CoreUtils.CreateEngineMaterial(shader);
            pass = new DebuffScreenEffectPass(material)
            {
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null || material == null)
            {
                return;
            }

            // 6장: Base Camera 전용 — Overlay(UI) 카메라의 렌더러에는 이 기능을 절대 부착하지
            // 말 것. 여기서는 실수로 부착되더라도 안전하도록 렌더 타입을 한 번 더 확인한다.
            if (renderingData.cameraData.renderType != CameraRenderType.Base)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            var perceptualLayer = Compositor.CompositePerceptual().ApplyGlobalAccessibilityScale(globalEffectStrength);
            var damageFeedbackLayer = Compositor.CompositeDamageFeedback().ApplyGlobalAccessibilityScale(globalEffectStrength);

            pass.Setup(perceptualLayer, damageFeedbackLayer, innerSafeRadius, outerSafeRadius);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
    }
}
