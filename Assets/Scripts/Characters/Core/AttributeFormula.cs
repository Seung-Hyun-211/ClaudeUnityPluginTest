using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Data-defined "attribute value -> game number" curve (crit chance from
    /// Luck, carry capacity from Strength, etc.) so designers can tune or add
    /// new conversions without touching code (see player-attributes.md).
    /// </summary>
    [CreateAssetMenu(menuName = "Characters/Attribute Formula", fileName = "New Attribute Formula")]
    public class AttributeFormula : ScriptableObject
    {
        [SerializeField] private AttributeType attribute;
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public AttributeType Attribute => attribute;

        public float Evaluate(AttributeSet attributes) => curve.Evaluate(attributes.GetValue(attribute));
    }
}
