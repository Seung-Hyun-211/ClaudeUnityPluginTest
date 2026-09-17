using System;
using System.Collections.Generic;
using UnityEngine;
using Game.UI;

namespace Game.UI.Windows
{
    /// <summary>
    /// Single source of truth for "what's currently blocking the game view".
    ///
    /// Full-screen windows are registered once, by string id, in
    /// `fullScreenWindows` — adding a new kind (map, quest log, ...) is a new
    /// catalog entry plus an IWindow implementation, never a change here or in
    /// FullScreenWindowHotkeyRouter/FullScreenWindowTabBarUIView. At most one
    /// is visible at a time (mutually exclusive).
    ///
    /// Popups are separate: pushed/popped by direct IWindow reference on a
    /// LIFO stack on top of whatever full-screen window is open — popups are
    /// transient and dynamically created, so a fixed catalog doesn't fit them.
    ///
    /// Assumption: only the top popup is ever interactive (standard modal
    /// stack UX), so a popup's own CloseRequested always closes whatever is
    /// currently on top rather than tracking which instance raised it.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour backgroundObscurerSource;
        [SerializeField] private FullScreenWindowEntry[] fullScreenWindows = Array.Empty<FullScreenWindowEntry>();

        private readonly Dictionary<string, IWindow> fullScreenById = new();
        private readonly List<IWindow> popupStack = new();
        private IBackgroundObscurer obscurer;
        private IWindow currentFullScreen;

        public IReadOnlyList<FullScreenWindowEntry> FullScreenWindows => fullScreenWindows;
        public string CurrentFullScreenId { get; private set; }
        public event Action FullScreenChanged;

        private void Awake()
        {
            obscurer = backgroundObscurerSource as IBackgroundObscurer;
            if (obscurer == null)
            {
                Debug.LogError($"{nameof(backgroundObscurerSource)} must implement {nameof(IBackgroundObscurer)}.", this);
            }

            foreach (var entry in fullScreenWindows)
            {
                if (entry.Window == null)
                {
                    Debug.LogError($"Full-screen window entry '{entry.Id}' does not implement {nameof(IWindow)}.", this);
                    continue;
                }

                fullScreenById[entry.Id] = entry.Window;
            }
        }

        public void OpenFullScreen(string id)
        {
            if (CurrentFullScreenId == id)
            {
                return;
            }

            if (!fullScreenById.TryGetValue(id, out var window))
            {
                Debug.LogError($"No full-screen window registered with id '{id}'.", this);
                return;
            }

            CloseFullScreen();

            currentFullScreen = window;
            currentFullScreen.CloseRequested += CloseFullScreen;
            currentFullScreen.Show();
            CurrentFullScreenId = id;

            RefreshObscurer();
            FullScreenChanged?.Invoke();
        }

        public void CloseFullScreen()
        {
            if (currentFullScreen == null)
            {
                return;
            }

            currentFullScreen.CloseRequested -= CloseFullScreen;
            currentFullScreen.Hide();
            currentFullScreen = null;
            CurrentFullScreenId = null;

            RefreshObscurer();
            FullScreenChanged?.Invoke();
        }

        public void ToggleFullScreen(string id)
        {
            if (CurrentFullScreenId == id)
            {
                CloseFullScreen();
            }
            else
            {
                OpenFullScreen(id);
            }
        }

        public void PushPopup(IWindow popup)
        {
            popupStack.Add(popup);
            popup.CloseRequested += PopTopPopup;
            popup.Show();
            RefreshObscurer();
        }

        public void PopTopPopup()
        {
            if (popupStack.Count == 0)
            {
                return;
            }

            int topIndex = popupStack.Count - 1;
            var top = popupStack[topIndex];
            top.CloseRequested -= PopTopPopup;
            top.Hide();
            popupStack.RemoveAt(topIndex);
            RefreshObscurer();
        }

        private void RefreshObscurer()
        {
            bool shouldObscure = currentFullScreen != null || popupStack.Count > 0;
            obscurer?.SetActive(shouldObscure);
        }
    }
}
