using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Minimal component that attaches a faction to a GameObject, so AI
    /// sensors can look for a hostile IDamageable without any tag comparisons.
    /// </summary>
    public class FactionMember : MonoBehaviour
    {
        [SerializeField] private Faction faction;

        public Faction Faction => faction;

        public void SetFaction(Faction value)
        {
            faction = value;
        }
    }
}
