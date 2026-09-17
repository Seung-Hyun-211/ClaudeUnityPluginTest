#ifndef GAME_TOON_HATCH_LIGHTING_INCLUDED
#define GAME_TOON_HATCH_LIGHTING_INCLUDED

// Shared cel-shading + procedural ink-hatching lighting model.
// Source: Docs/기획문서_셀셰이딩그래픽설계.md (3장~5장).
// One function, reused by every character/weapon/environment shader instead of
// duplicating the lighting math per material type (문서 2장 "재사용성" 요구사항).

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 3.1절 기본 가중치: lightWeight=0.6, shadowWeight=0.4, aoWeight=0.2 (합이 1을 넘을 수 있어 최종 saturate로 클램프).
#define TOON_LIGHT_WEIGHT 0.6
#define TOON_SHADOW_WEIGHT 0.4
#define TOON_AO_WEIGHT 0.2

// 0 = 완전히 밝음(Lit), 1 = 완전히 어두움(Shadow). 3.1절 수식.
float ComputeToneValue(float NdotL, float shadowAttenuation, float ambientOcclusion)
{
    float lit = (1.0 - NdotL) * TOON_LIGHT_WEIGHT;
    float shadow = (1.0 - shadowAttenuation) * TOON_SHADOW_WEIGHT;
    float ao = (1.0 - ambientOcclusion) * TOON_AO_WEIGHT;
    return saturate(lit + shadow + ao);
}

// 4.3절: 환경/다수 스폰 오브젝트용 좌표계. 세 축을 블렌딩하지 않고 지배적인 축 하나만
// 선택한다 — 해칭 선의 각도가 블렌드 경계에서 뒤섞이지 않고 또렷하게 유지되도록 하는
// 절차적 해칭 특유의 요구사항(각 레이어가 고정된 각도를 가져야 크로스해치가 또렷함, 4.2절).
float2 TriplanarHatchCoord(float3 worldPos, float3 worldNormal, float scale)
{
    float3 blend = abs(worldNormal);
    float maxBlend = max(blend.x, max(blend.y, blend.z));

    if (maxBlend == blend.x)
    {
        return worldPos.zy * scale;
    }
    if (maxBlend == blend.y)
    {
        return worldPos.xz * scale;
    }
    return worldPos.xy * scale;
}

// 4.2절: 레이어 1개의 절차적 빗금 마스크. 삼각파(triangle wave)로 반복되는 선을 만들고
// thickness로 선 굵기를, fwidth 기반 안티에일리어싱으로 시밍을 방지한다(4.4절).
float HatchLineMask(float2 hatchCoord, float angleDegrees, float frequency, float thickness)
{
    float rad = radians(angleDegrees);
    float2 lineDir = float2(cos(rad), sin(rad));
    float linePattern = abs(frac(dot(hatchCoord, lineDir) * frequency) * 2.0 - 1.0);
    float aa = max(fwidth(linePattern), 1e-4) * 1.5;
    return 1.0 - smoothstep(thickness - aa, thickness + aa, linePattern);
}

// 4.2절: N=4 해칭 레이어. toneValue가 임계값을 넘을 때마다 레이어가 하나씩 추가로
// 활성화되어 겹쳐진다(크로스해치). 임계값 경계도 fwidth 기반으로 부드럽게 켠다(4.4절)
// — 하드 if 분기로 자르면 카메라가 조금만 움직여도 레이어가 픽셀 단위로 깜빡인다.
float ComputeHatchCoverage(float toneValue, float2 hatchCoord, float4 thresholds, float4 angles, float4 frequencies, float thickness)
{
    float coverage = 0.0;
    float bandAA = max(fwidth(toneValue), 1e-4) * 2.0;

    UNITY_UNROLL
    for (int i = 0; i < 4; i++)
    {
        float threshold = thresholds[i];
        float activation = smoothstep(threshold - bandAA, threshold + bandAA, toneValue);
        if (activation <= 0.0)
        {
            continue;
        }
        coverage += HatchLineMask(hatchCoord, angles[i], frequencies[i], thickness) * activation;
    }
    return saturate(coverage);
}

// 2장: Additional Light도 같은 톤 양자화 로직을 거치되, 레이어별 해칭을 다시 그리지
// 않고 명암 기여만 더한다 — 문서가 다중 라이트 해칭 합성 규칙까지는 규정하지 않으므로,
// hatchCoverage로 이미 어두워진 영역 위에 보조광의 밝은 기여만 얹는 절충으로 구현한다.
float3 AccumulateAdditionalToonLights(float3 worldPos, float3 worldNormal, float hatchCoverage)
{
    float3 additional = 0;
#if defined(_ADDITIONAL_LIGHTS)
    uint lightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < lightCount; lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, worldPos);
        float NdotL = saturate(dot(worldNormal, light.direction));
        float toneValue = ComputeToneValue(NdotL, light.shadowAttenuation, 1.0);
        additional += light.color * light.distanceAttenuation * (1.0 - toneValue) * (1.0 - hatchCoverage);
    }
#endif
    return additional;
}

// 문서 5장 의사코드의 실사용 버전. 캐릭터/무기/환경 셰이더가 공통으로 호출하는 진입점.
// hatchColorScale 기본값 0.15 (4.5절: 순수 검정이 아니라 베이스 컬러를 어둡게 한 색).
float3 ToonHatchLighting(
    float3 baseColor,
    float3 worldPos,
    float3 worldNormal,
    float2 hatchUV,
    bool useTriplanar,
    float triplanarScale,
    float ambientOcclusion,
    float4 hatchThreshold,
    float4 hatchAngle,
    float4 hatchFrequency,
    float hatchThickness,
    float hatchColorScale)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPos));
    float NdotL = saturate(dot(worldNormal, mainLight.direction));
    float toneValue = ComputeToneValue(NdotL, mainLight.shadowAttenuation, ambientOcclusion);

    float2 hatchCoord = useTriplanar ? TriplanarHatchCoord(worldPos, worldNormal, triplanarScale) : hatchUV;
    float hatchCoverage = ComputeHatchCoverage(toneValue, hatchCoord, hatchThreshold, hatchAngle, hatchFrequency, hatchThickness);

    float3 litColor = baseColor * mainLight.color + SampleSH(worldNormal) * baseColor;
    float3 hatchColor = baseColor * hatchColorScale;
    // 4.5절: 곱연산이 아니라 Lerp — 완전히 덮인 영역도 hatchColor로 은은하게 유지.
    float3 shaded = lerp(litColor, hatchColor, hatchCoverage);

    return shaded + AccumulateAdditionalToonLights(worldPos, worldNormal, hatchCoverage);
}

// 7장 권장: 실루엣 강조용 페넬 림. 항상 뚜렷한 경계(스텝)로 처리해 툰 톤 일관성 유지.
float ComputeToonRim(float3 worldNormal, float3 viewDirWS, float rimThreshold, float rimAA)
{
    float NdotV = saturate(dot(worldNormal, viewDirWS));
    float rim = 1.0 - NdotV;
    return smoothstep(rimThreshold - rimAA, rimThreshold + rimAA, rim);
}

#endif // GAME_TOON_HATCH_LIGHTING_INCLUDED
