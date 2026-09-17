using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Windows
{
    public class FullScreenWindowTabButtonUIView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private GameObject selectedIndicator;

        public string Id { get; private set; }
        public event Action<string> Clicked;

        public void Bind(string id, string labelText)
        {
            Id = id;
            label.text = labelText;
        }

        public void SetSelected(bool selected)
        {
            selectedIndicator.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(Id);
        }
    }
}
