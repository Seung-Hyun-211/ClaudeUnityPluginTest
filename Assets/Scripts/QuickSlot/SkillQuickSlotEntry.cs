using UnityEngine;
using Game.Skills;

namespace Game.QuickSlot
{
    public class SkillQuickSlotEntry : IQuickSlottable
    {
        public SkillInstance Skill { get; }

        public SkillQuickSlotEntry(SkillInstance skill)
        {
            Skill = skill;
        }

        public Sprite Icon => Skill.Data.Icon;
        public bool IsUsable => Skill.IsReady;

        public void Use(QuickSlotUseContext context)
        {
            Skill.TryActivate(new SkillExecutionContext(context.User));
        }
    }
}
