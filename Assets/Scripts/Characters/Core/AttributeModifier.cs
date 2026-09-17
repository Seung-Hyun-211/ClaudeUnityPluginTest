namespace Game.Characters
{
    /// <summary>
    /// A single flat/percent bonus to one attribute, tagged with the object
    /// that granted it so it can be removed precisely later (see
    /// AttributeSet.RemoveAllFromSource).
    /// </summary>
    public sealed class AttributeModifier
    {
        public AttributeType Type { get; }
        public float FlatBonus { get; }
        public float PercentBonus { get; }
        public object Source { get; }

        public AttributeModifier(AttributeType type, float flatBonus, float percentBonus, object source)
        {
            Type = type;
            FlatBonus = flatBonus;
            PercentBonus = percentBonus;
            Source = source;
        }
    }
}
