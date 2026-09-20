using UnityEngine;
using Game.UI.Windows;

namespace Game.Dialogue
{
    /// <summary>
    /// Composition root of the dialogue system for a scene: owns the
    /// DialogueRunner, wires it to the view, and registers the built-in event
    /// handlers. Quest/shop systems plug in later through RegisterEventHandler.
    /// While a sequence plays it registers as an input blocker on the
    /// WindowManager, so player movement, interaction and hotkeys are gated
    /// the same way they are while a window is open.
    /// </summary>
    public class DialoguePlayer : MonoBehaviour
    {
        [SerializeField] private DialogueFlagStore flagStore;
        [SerializeField] private MonoBehaviour viewSource;
        [SerializeField] private WindowManager windowManager;

        private DialogueRunner runner;
        private IDialogueView view;
        private bool pendingUnblock;

        public static DialoguePlayer Instance { get; private set; }

        public IDialogueFlags Flags => flagStore;
        public bool IsPlaying => runner != null && runner.State != DialogueState.Idle;
        public DialogueState State => runner.State;
        public DialogueSequence CurrentSequence => runner.Sequence;

        /// <summary>A real window (a shop, a quest offer) is open on top of the dialogue - it owns the keys (Esc closes it), not the dialogue.</summary>
        public bool IsModalWindowOpen => windowManager != null && windowManager.IsWindowOpen;

        /// <summary>Frame the current sequence started on - input handlers ignore that frame so the key press that opened it does not also advance it.</summary>
        public int StartFrame { get; private set; } = -1;

        private void Awake()
        {
            view = viewSource as IDialogueView;
            if (view == null)
            {
                Debug.LogError($"{nameof(viewSource)} must implement {nameof(IDialogueView)}.", this);
                return;
            }

            runner = new DialogueRunner(flagStore);
            runner.RegisterHandler(new SetFlagEventHandler());
            runner.RegisterHandler(new GiveItemEventHandler());

            runner.LineShown += node => view.ShowLine(node.speakerName, node.text, node.portrait, runner.NotifyLineFullyShown);
            runner.ChoicesShown += options => view.ShowChoices(options, runner.Choose);
            runner.Ended += HandleEnded;
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one DialoguePlayer in the scene - the latest one wins.", this);
            }
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterEventHandler(IDialogueEventHandler handler) => runner.RegisterHandler(handler);

        public bool Play(DialogueSequence sequence, GameObject interactor)
        {
            if (runner == null || IsPlaying || sequence == null)
            {
                return false;
            }

            StartFrame = Time.frameCount;
            pendingUnblock = false;
            if (windowManager != null)
            {
                windowManager.AddInputBlocker(this);
            }

            view.Show(sequence.Skippable);
            return runner.Start(sequence, interactor);
        }

        private void Update() => runner?.Tick(Time.deltaTime);

        // The blocker is released a frame late on purpose: the key that ends
        // the last line (F) is also the interact key, and PlayerInteractionController
        // may run after this frame's input handling - releasing immediately
        // would let that same key press start the dialogue again.
        private void LateUpdate()
        {
            if (pendingUnblock)
            {
                pendingUnblock = false;
                if (windowManager != null)
                {
                    windowManager.RemoveInputBlocker(this);
                }
            }
        }

        private void HandleEnded()
        {
            view.Hide();
            pendingUnblock = true;
        }

        // Input entry points, called by DialogueInputHandler.

        public void OnSubmit()
        {
            if (view.IsLogOpen)
            {
                view.ToggleLog(runner.Log);
                return;
            }

            switch (runner.State)
            {
                case DialogueState.Playing:
                    view.CompleteTyping();
                    break;
                case DialogueState.WaitingForAdvance:
                    runner.Submit();
                    break;
                case DialogueState.ChoicePending:
                    view.ConfirmFocusedChoice();
                    break;
            }
        }

        public void OnNavigate(int delta)
        {
            if (runner.State == DialogueState.ChoicePending)
            {
                view.MoveChoiceFocus(delta);
            }
        }

        /// <summary>Esc: abandons the dialogue; talking again starts over from the beginning.</summary>
        public void OnCancel() => runner.Cancel();

        /// <summary>Skips ahead through lines (running their events) to the next choice or the end - ignored when the sequence is not skippable.</summary>
        public void OnSkip()
        {
            runner.FastForward();
            if (IsPlaying)
            {
                view.CompleteTyping();
            }
        }

        /// <summary>Toggles the dialogue history, only between lines (not on the choice screen).</summary>
        public void OnToggleLog()
        {
            if (runner.State == DialogueState.WaitingForAdvance || view.IsLogOpen)
            {
                view.ToggleLog(runner.Log);
            }
        }
    }
}
