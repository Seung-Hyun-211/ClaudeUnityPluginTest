using System;
using UnityEngine;
using Game.Items;
using Game.Items.Equipment;

namespace Game.Dialogue
{
    /// <summary>
    /// Puts an item into the interactor's containers (grid first, flat
    /// inventory as fallback); whatever does not fit drops at their feet.
    /// </summary>
    public class GiveItemEventHandler : IDialogueEventHandler
    {
        public DialogueEventType EventType => DialogueEventType.GiveItem;

        public void Execute(DialogueNode node, DialogueEventContext context, Action<bool> onCompleted)
        {
            var interactor = context.Interactor;
            if (node.eventItem == null || interactor == null)
            {
                Debug.LogWarning($"GiveItem node [{node.id}] needs an item and an interactor - skipping it.");
                onCompleted(true);
                return;
            }

            int leftover = node.eventCount;
            if (interactor.TryGetComponent(out ContainerEquipmentController equipment))
            {
                leftover = equipment.AddToContainers(node.eventItem, leftover);
            }
            else if (interactor.TryGetComponent(out IInventory inventory))
            {
                leftover = inventory.AddItem(node.eventItem, leftover);
            }

            if (leftover > 0)
            {
                WorldItemFactory.Instance.Spawn(new WorldSpawnRequest(node.eventItem, leftover), interactor.transform.position);
            }

            onCompleted(true);
        }
    }
}
