using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows
{
    /// <summary>
    /// Generic popup shell (frame, dim, close button, content slot). Never
    /// knows which IPopupContent it hosts — new puzzle/event types are new
    /// IPopupContent implementations, not new window classes.
    /// </summary>
    public class PopupWindow : MonoBehaviour, IWindow
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Transform contentSlot;
        [SerializeField] private Button closeButton;

        private IPopupContent content;

        public Transform ContentSlot => contentSlot;
        public bool IsVisible { get; private set; }
        public event Action CloseRequested;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        public void SetContent(IPopupContent popupContent)
        {
            if (content != null)
            {
                content.Finished -= OnContentFinished;
            }

            content = popupContent;
            content.Finished += OnContentFinished;
        }

        public void Show()
        {
            IsVisible = true;
            windowRoot.SetActive(true);
            content?.Present();
        }

        public void Hide()
        {
            IsVisible = false;
            windowRoot.SetActive(false);
        }

        private void OnContentFinished() => CloseRequested?.Invoke();
    }
}
