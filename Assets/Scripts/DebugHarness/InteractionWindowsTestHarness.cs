using UnityEngine;
using Game.Combat;
using Game.UI.Windows;

namespace Game.DebugHarness
{
    /// <summary>
    /// Manual test harness for interaction-system.md/window-system.md/
    /// hud-system.md. Door/Dialogue interactables and the hotkeys (1-4 to open
    /// windows via FullScreenWindowHotkeyRouter, Escape to close via
    /// WindowCloseInputHandler) all work with zero harness code - this only
    /// adds damage/heal buttons so PlayerVitalsHudPanel's bars visibly move,
    /// plus buttons as an alternative to memorizing hotkeys. OnGUI only;
    /// never shipped in a real build.
    /// </summary>
    public class InteractionWindowsTestHarness : MonoBehaviour
    {
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private ArmorComponent playerArmor;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 460, 320), GUI.skin.box);
            GUILayout.Label("Interaction + Windows/HUD Test Harness");
            GUILayout.Label("Walk up to the door or NPC and press F. Number keys open windows, Escape closes.");

            GUILayout.Space(8);
            if (GUILayout.Button("Open Inventory")) windowManager.OpenFullScreen("inventory");
            if (GUILayout.Button("Open Map")) windowManager.OpenFullScreen("map");
            if (GUILayout.Button("Open Quest")) windowManager.OpenFullScreen("quest");
            if (GUILayout.Button("Open Settings")) windowManager.OpenFullScreen("settings");
            if (GUILayout.Button("Close current window")) windowManager.CloseFullScreen();
            GUILayout.Label($"Current full-screen window: {windowManager.CurrentFullScreenId ?? "(none)"}, any open (incl. popups): {windowManager.IsAnyWindowOpen}");

            GUILayout.Space(8);
            if (GUILayout.Button("Damage player (15)") && playerHealth.IsAlive)
            {
                playerHealth.TakeDamage(new DamageInfo(15f, DamageType.Physical, null, playerHealth.transform.position));
            }
            if (GUILayout.Button("Heal player (15)")) playerHealth.Heal(15f);
            GUILayout.Label($"Player HP: {playerHealth.Current}/{playerHealth.Max}   Armor: {playerArmor.Current}/{playerArmor.Max}");
            GUILayout.EndArea();
        }
    }
}
