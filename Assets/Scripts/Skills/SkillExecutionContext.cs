using UnityEngine;

namespace Game.Skills
{
    public readonly struct SkillExecutionContext
    {
        public GameObject Caster { get; }

        public SkillExecutionContext(GameObject caster)
        {
            Caster = caster;
        }
    }
}
