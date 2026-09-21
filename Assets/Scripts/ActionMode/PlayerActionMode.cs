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
}
