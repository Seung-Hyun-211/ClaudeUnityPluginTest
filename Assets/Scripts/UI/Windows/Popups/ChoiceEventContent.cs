using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.Popups
{
    /// <summary>Example event: show text, let the player pick one of several options.</summary>
    public class ChoiceEventContent : MonoBehaviour, IPopupContent
    {
        [SerializeField] private Text bodyText;
        [SerializeField] private Button[] choiceButtons;

        public int SelectedChoice { get; private set; } = -1;
        public event Action Finished;

        public void Present()
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int choiceIndex = i;
                choiceButtons[i].onClick.AddListener(() => SelectChoice(choiceIndex));
            }
        }

        private void SelectChoice(int index)
        {
            SelectedChoice = index;
            Finished?.Invoke();
        }
    }
}
