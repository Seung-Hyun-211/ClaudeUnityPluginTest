using UnityEngine;
using Game.Interaction;
using Game.Items.Equipment;

namespace Game.Items
{
    /// <summary>
    /// A rig or backpack lying in the world with its contents still inside.
    /// Interacting puts it on (like WeaponPickup equips a weapon): whatever
    /// the wearer had in that slot comes off with its contents and drops here.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ContainerPickup : MonoBehaviour, IInteractable
    {
        private ContainerItemData container;
        private ContainerContents contents = ContainerContents.Empty;

        public ContainerItemData Container => container;
        public ContainerContents Contents => contents;

        public void Initialize(ContainerItemData containerItem, ContainerContents containerContents)
        {
            container = containerItem;
            contents = containerContents ?? ContainerContents.Empty;
        }

        public string PromptText => contents.IsEmpty
            ? $"{container.DisplayName} 착용"
            : $"{container.DisplayName} 착용 (아이템 {contents.Entries.Count}개 들어있음)";

        public bool CanInteract(GameObject interactor) => interactor.GetComponent<ContainerEquipmentController>() != null;

        public void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ContainerEquipmentController equipment))
            {
                return;
            }

            var result = equipment.EquipWithContents(container, contents);
            WorldItemFactory.Instance?.Drop(result, transform.position);
            Destroy(gameObject);
        }
    }
}
