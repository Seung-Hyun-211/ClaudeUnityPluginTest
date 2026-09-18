using UnityEngine;
using Game.Combat;
using Game.Player;

namespace Game.DebugHarness
{
    /// <summary>
    /// OnGUI harness for the real PlayerVitals/HealthComponent save adapters
    /// (Game.Player.PlayerVitalsSaveProvider / Game.Combat.HealthSaveProvider).
    /// Damage/heal and drain vitals here, then use SceneFlowTestHarness's
    /// Save/Load buttons (persistent Boot object) to confirm the values
    /// round-trip through the real Persistence pipeline, not just the dummy
    /// counter.
    /// </summary>
    public class PlayerStateTestHarness : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerVitals vitals;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 570, 340, 110), GUI.skin.box);
            GUILayout.Label($"Health: {health.Current:0}/{health.Max:0}");
            GUILayout.Label($"Hunger: {vitals.Hunger.Current:0}/{vitals.Hunger.Max:0}   Thirst: {vitals.Thirst.Current:0}/{vitals.Thirst.Max:0}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage 10"))
            {
                health.TakeDamage(new DamageInfo(10f, DamageType.Physical, null, health.transform.position));
            }
            if (GUILayout.Button("Heal 10"))
            {
                health.Heal(10f);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Drain vitals by 20"))
            {
                vitals.RestoreVitals(vitals.Hunger.Current - 20f, vitals.Thirst.Current - 20f);
            }
            GUILayout.EndArea();
        }
    }
}
