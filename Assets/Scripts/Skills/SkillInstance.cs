using UnityEngine;

namespace Game.Skills
{
    /// <summary>
    /// Runtime pairing of a shared SkillData asset with per-owner cooldown
    /// state — mirrors how ItemStack pairs ItemData with a runtime quantity.
    /// </summary>
    public class SkillInstance
    {
        public SkillData Data { get; }
        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f;

        public SkillInstance(SkillData data)
        {
            Data = data;
        }

        public void Tick(float deltaTime)
        {
            if (CooldownRemaining > 0f)
            {
                CooldownRemaining = Mathf.Max(0f, CooldownRemaining - deltaTime);
            }
        }

        public bool TryActivate(SkillExecutionContext context)
        {
            if (!IsReady)
            {
                return false;
            }

            Data.Activate(context);
            CooldownRemaining = Data.Cooldown;
            return true;
        }
    }
}
