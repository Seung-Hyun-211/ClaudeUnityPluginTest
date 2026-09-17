using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI.Windows
{
    /// <summary>
    /// One registered full-screen "kind" (inventory, settings, map, quest, ...).
    /// Adding a new kind is adding one of these to WindowManager's catalog —
    /// no change to WindowManager, the hotkey router, or the tab bar UI.
    /// </summary>
    [Serializable]
    public struct FullScreenWindowEntry
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField] private MonoBehaviour windowSource;
        [SerializeField] private Key hotkey;

        public string Id => id;
        public string Label => label;
        public IWindow Window => windowSource as IWindow;
        public Key Hotkey => hotkey;
    }
}
