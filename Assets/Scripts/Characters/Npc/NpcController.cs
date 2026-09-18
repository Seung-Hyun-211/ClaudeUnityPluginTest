using UnityEngine;
using Game.Characters;
using Game.Combat;

namespace Game.Characters.Npc
{
    /// <summary>
    /// The only NPC component — Village and CombatHelper are the same
    /// NpcController, distinguished by NpcData.Role plus whichever optional
    /// components a prefab attaches alongside it (DialogueInteractable,
    /// HealthComponent, AiSensor/AiStateMachine/CompanionBrain/
    /// CompanionOrderReceiver, ...). Adding a third role never means adding a
    /// subclass here (open-closed) — see npc-roles.md.
    ///
    /// This class intentionally stays narrow: identity (NpcData) and faction
    /// only. It does NOT implement IInteractable itself — the Village NPC
    /// worked example in npc-roles.md attaches the existing
    /// DialogueInteractable component side by side instead, avoiding two
    /// competing IInteractable implementations fighting over one GameObject.
    /// It also does not wire up the AI framework (AiStateMachine/IAiBrain);
    /// that composition/driving concern belongs to whatever ticks the state
    /// machine, which is out of this class's single responsibility (see the
    /// implementation report for why that driver isn't built in this pass).
    /// </summary>
    [RequireComponent(typeof(FactionMember))]
    public class NpcController : MonoBehaviour
    {
        [SerializeField] private NpcData data;
        [SerializeField] private FactionMember faction;
        [SerializeField] private HealthComponent health;

        public NpcData Data => data;
        public FactionMember Faction => faction;

        /// <summary>Null on NPCs that can't be damaged (see npc-roles.md) — HealthComponent is attached only on prefabs that need it.</summary>
        public HealthComponent Health => health;

        private void Awake()
        {
            if (data != null)
            {
                faction.SetFaction(data.Faction);
            }
        }
    }
}
