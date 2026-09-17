using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Items.Grid;

namespace Game.Items.UI
{
    /// <summary>An item icon sized to its footprint and positioned at its origin cell.</summary>
    public class GridItemUIView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text quantityText;

        public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;
        public PlacedItem Placed { get; private set; }
        public event Action<PlacedItem> Clicked;

        public void Bind(PlacedItem placed)
        {
            Placed = placed;
            iconImage.sprite = placed.Stack.Item.Icon;
            quantityText.text = placed.Stack.Quantity > 1 ? placed.Stack.Quantity.ToString() : string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(Placed);
        }
    }
}
