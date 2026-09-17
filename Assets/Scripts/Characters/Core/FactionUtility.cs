namespace Game.Characters
{
    /// <summary>
    /// Single place that decides who is hostile to whom, so no other system
    /// hardcodes faction/tag comparisons of its own.
    /// </summary>
    public static class FactionUtility
    {
        public static bool IsHostileTo(Faction a, Faction b)
        {
            if (a == Faction.Neutral || b == Faction.Neutral)
            {
                return false;
            }

            return a != b;
        }
    }
}
