using UnityEngine;
using Game.Characters;

namespace Game.Characters.Npc
{
    /// <summary>
    /// Per-NPC identity/config asset: name, faction, base stats and which role
    /// bucket the NPC belongs to. Reuses CharacterStatsData exactly as
    /// Player/Enemy do so every actor shares the same stat concept instead of
    /// each domain inventing its own (see character-system.md, npc-roles.md).
    /// </summary>
    [CreateAssetMenu(menuName = "Characters/Npc Data", fileName = "New Npc")]
    public class NpcData : ScriptableObject
    {
        [SerializeField] private NpcRole role;
        [SerializeField] private string displayName;
        [SerializeField] private Faction faction;
        [SerializeField] private CharacterStatsData baseStats;

        public NpcRole Role => role;
        public string DisplayName => displayName;
        public Faction Faction => faction;
        public CharacterStatsData BaseStats => baseStats;
    }
}
