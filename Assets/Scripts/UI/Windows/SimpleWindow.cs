using System;
using UnityEngine;

namespace Game.UI.Windows
{
    /// <summary>
    /// Base for a window that's just "a root GameObject to show/hide" —
    /// covers full-screen screens like inventory/settings. Subclasses add
    /// their own content wiring and call RequestClose() for their own close
    /// affordance (Escape key, close button, ...).
    /// </summary>
    public abstract class SimpleWindow : MonoBehaviour, IWindow
    {
        [SerializeField] private GameObject windowRoot;

        public bool IsVisible { get; private set; }
        public event Action CloseRequested;

        public virtual void Show()
        {
            IsVisible = true;
            windowRoot.SetActive(true);
        }

        public virtual void Hide()
        {
            IsVisible = false;
            windowRoot.SetActive(false);
        }

        protected void RequestClose() => CloseRequested?.Invoke();
    }
}
