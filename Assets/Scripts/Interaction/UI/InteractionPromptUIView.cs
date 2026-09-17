using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction.UI
{
    /// <summary>HUD prompt ("F: 문 열기") driven purely by IInteractable.PromptText.</summary>
    public class InteractionPromptUIView : MonoBehaviour
    {
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;

        private void OnEnable()
        {
            detector.TargetChanged += Refresh;
            Refresh(detector.Current);
        }

        private void OnDisable()
        {
            detector.TargetChanged -= Refresh;
        }

        private void Refresh(IInteractable target)
        {
            bool hasTarget = target != null;
            promptRoot.SetActive(hasTarget);
            if (hasTarget)
            {
                promptText.text = target.PromptText;
            }
        }
    }
}
