using System;

namespace Game.ActionMode
{
    /// <summary>
    /// Read side of the player's action mode. Combat-only input handlers depend
    /// on this (optionally, like their WindowManager) and ignore input while
    /// the mode is not Combat. Unlike WindowManager.IsAnyWindowOpen this does
    /// not stop movement - building is done while walking around.
    /// </summary>
    public interface IPlayerActionMode
    {
        PlayerActionMode Current { get; }

        event Action<PlayerActionMode> Changed;
    }

    /// <summary>Write side: only whatever switches modes (the build input handler) needs it.</summary>
    public interface IPlayerActionModeSetter
    {
        void Set(PlayerActionMode mode);

        void Toggle();
    }

    public static class PlayerActionModeExtensions
    {
        /// <summary>True in combat - and when there is no mode source at all (gating is optional), so handlers can write <c>if (!mode.IsCombat()) return;</c>.</summary>
        public static bool IsCombat(this IPlayerActionMode mode) => mode == null || mode.Current == PlayerActionMode.Combat;
    }
}
