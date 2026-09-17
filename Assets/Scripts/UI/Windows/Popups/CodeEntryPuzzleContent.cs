using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.Popups
{
    /// <summary>Example puzzle: type the correct code to finish. Proves the IPopupContent extension point.</summary>
    public class CodeEntryPuzzleContent : MonoBehaviour, IPopupContent
    {
        [SerializeField] private InputField codeInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private string expectedCode;

        public bool WasSolved { get; private set; }
        public event Action Finished;

        public void Present()
        {
            submitButton.onClick.AddListener(OnSubmit);
        }

        private void OnSubmit()
        {
            if (codeInput.text != expectedCode)
            {
                return;
            }

            WasSolved = true;
            Finished?.Invoke();
        }
    }
}
