namespace Game.Rendering
{
    /// <summary>
    /// Docs/기획문서_디버프스크린셰이더설계.md 3.1절: 화면 중앙(조준 영역)까지 가릴지 여부를
    /// 결정하는 이펙트 유형. Perceptual = 캐릭터의 지각/초점 자체가 망가짐(중앙 포함 적용).
    /// DamageFeedback = 단순 상태 경고(중앙 안전 영역 제외, 가장자리 링에만 적용).
    /// </summary>
    public enum EffectCategory
    {
        Perceptual,
        DamageFeedback
    }
}
