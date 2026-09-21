using System;
using UnityEngine;

namespace Game.ActionMode
{
    /// <summary>What the player's mouse buttons and number keys currently mean.</summary>
    public enum PlayerActionMode
    {
        /// <summary>LMB attacks, 1-3 switch weapons, 4-0 use quickslots.</summary>
        Combat,

        /// <summary>LMB places, RMB demolishes, 1-5 pick a building category. Movement is unchanged.</summary>
        Build,
    }

    /// <summary>
    /// The player's current action mode. Combat-only input handlers hold a
    /// reference to it (optional, like their WindowManager) and ignore input
    /// while the mode is not Combat. Unlike WindowManager.IsAnyWindowOpen this
    /// does not stop movement - building is done while walking around.
    /// </summary>
    public interface IPlayerActionMode
    {
        PlayerActionMode Current { get; }

        event Action<PlayerActionMode> Changed;
    }

    /// <summary>Scene/prefab component holding the mode. Lives in a leaf module so QuickSlot and Weapons can depend on it without a cycle.</summary>
    public class PlayerActionModeSwitch : MonoBehaviour, IPlayerActionMode
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
