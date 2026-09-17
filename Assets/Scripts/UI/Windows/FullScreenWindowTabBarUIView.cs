using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Windows
{
    /// <summary>
    /// Renders one tab button per registered full-screen window. Adding a new
    /// kind (map, quest log, ...) to WindowManager's catalog makes a button
    /// appear here automatically — this view never lists kinds by name.
    /// </summary>
    public class FullScreenWindowTabBarUIView : MonoBehaviour
    {
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private Transform tabContainer;
        [SerializeField] private FullScreenWindowTabButtonUIView tabPrefab;

        private readonly List<FullScreenWindowTabButtonUIView> tabs = new();

        private void OnEnable()
        {
            BuildTabs();
            windowManager.FullScreenChanged += RefreshSelection;
            RefreshSelection();
        }

        private void OnDisable()
        {
            windowManager.FullScreenChanged -= RefreshSelection;
        }

        private void BuildTabs()
        {
            foreach (Transform child in tabContainer)
            {
                Destroy(child.gameObject);
            }
            tabs.Clear();

            foreach (var entry in windowManager.FullScreenWindows)
            {
                var tab = Instantiate(tabPrefab, tabContainer);
                tab.Bind(entry.Id, entry.Label);
                tab.Clicked += windowManager.ToggleFullScreen;
                tabs.Add(tab);
            }
        }

        private void RefreshSelection()
        {
            foreach (var tab in tabs)
            {
                tab.SetSelected(windowManager.CurrentFullScreenId == tab.Id);
            }
        }
    }
}
