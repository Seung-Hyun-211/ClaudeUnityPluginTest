using System;
using UnityEngine;

namespace Game.ActionMode
{
    /// <summary>Scene/prefab component holding the mode. Lives in a leaf module so QuickSlot and Weapons can depend on it without a cycle.</summary>
    public class PlayerActionModeSwitch : MonoBehaviour, IPlayerActionMode, IPlayerActionModeSetter
    {
        [SerializeField] private PlayerActionMode startMode = PlayerActionMode.Combat;

        private PlayerActionMode current;
        private bool initialized;

        public PlayerActionMode Current
        {
            get
            {
                EnsureInitialized();
                return current;
            }
        }

        public bool IsCombat => Current == PlayerActionMode.Combat;

        public event Action<PlayerActionMode> Changed;

        /// <summary>Sets the mode; raises Changed only when it actually changes.</summary>
        public void Set(PlayerActionMode mode)
        {
            EnsureInitialized();
            if (current == mode)
            {
                return;
            }

            current = mode;
            Changed?.Invoke(mode);
        }

        public void Toggle()
        {
            Set(Current == PlayerActionMode.Combat ? PlayerActionMode.Build : PlayerActionMode.Combat);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            current = startMode;
        }
    }
}
