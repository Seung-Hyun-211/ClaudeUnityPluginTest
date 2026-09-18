using Game.Interaction;
using UnityEngine;

namespace Game.Persistence
{
    /// <summary>
    /// A world-placed save point. Implements the existing
    /// Game.Interaction.IInteractable rather than inventing a new concept,
    /// so it rides the existing detector/input/prompt pipeline for free (see
    /// scene-and-persistence-system.md §4-3).
    ///
    /// The sink is wired the same way WindowManager wires
    /// backgroundObscurerSource: a plain MonoBehaviour field cast to the
    /// target interface. ISaveRequestSink cannot be assigned directly from
    /// the inspector (Unity can only serialize UnityEngine.Object
    /// references), so the field is typed as MonoBehaviour and validated
    /// in Awake.
    /// </summary>
    public class SaveTriggerPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptText = "Save";
        [SerializeField] private MonoBehaviour saveSinkSource;

        private ISaveRequestSink saveSink;

        public string PromptText => promptText;

        private void Awake()
        {
            saveSink = saveSinkSource as ISaveRequestSink;
            if (saveSink == null)
            {
                Debug.LogError($"{nameof(saveSinkSource)} must implement {nameof(ISaveRequestSink)}.", this);
            }
        }

        public bool CanInteract(GameObject interactor) => saveSink != null;

        public void Interact(GameObject interactor) => saveSink?.RequestSave(SaveTriggerReason.ManualSavePoint);
    }
}
