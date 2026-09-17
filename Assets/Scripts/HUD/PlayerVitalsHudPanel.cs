using UnityEngine;
using Game.Combat;
using Game.UI;

namespace Game.HUD
{
    /// <summary>
    /// Thin top-left composition: binds the generic StatBarUIView to the
    /// player's HealthComponent/ArmorComponent. WeaponInfoUIView binds itself
    /// (it is not a generic reusable widget), so this panel only wires the
    /// two bars — the widgets never reference each other (documents/hud-system.md).
    /// </summary>
    public class PlayerVitalsHudPanel : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private ArmorComponent armor;
        [SerializeField] private StatBarUIView healthBar;
        [SerializeField] private StatBarUIView armorBar;

        private void OnEnable()
        {
            healthBar.Bind(health);
            armorBar.Bind(armor);
        }
    }
}
