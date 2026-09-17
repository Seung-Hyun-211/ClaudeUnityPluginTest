using UnityEngine;

namespace Game.Skills
{
    /// <summary>
    /// Shared, static definition of one skill. Concrete skills subclass this
    /// and implement Activate — same ScriptableObject-as-data pattern used by
    /// ItemData/EnemyData elsewhere in the project.
    /// </summary>
    public abstract class SkillData : ScriptableObject
    {
        [SerializeField] private string skillId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0f)] private float cooldown;

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float Cooldown => cooldown;

        public abstract void Activate(SkillExecutionContext context);
    }
}
