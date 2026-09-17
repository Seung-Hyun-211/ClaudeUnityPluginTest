using UnityEngine;

namespace Game.Skills
{
    /// <summary>
    /// Example skill proving out the extension point ("scan nearby"). Detection
    /// radius and what gets highlighted are left for a follow-up pass.
    /// </summary>
    [CreateAssetMenu(menuName = "Skills/Scan Skill", fileName = "New Scan Skill")]
    public class ScanSkillData : SkillData
    {
        [SerializeField, Min(0f)] private float scanRadius = 10f;

        public float ScanRadius => scanRadius;

        public override void Activate(SkillExecutionContext context)
        {
            // TODO: 반경 내 IInteractable/IDamageable 탐지 및 하이라이트 연출.
        }
    }
}
