using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialogue
{
    /// <summary>
    /// The text-dialogue presentation (bottom box with portrait, speaker name,
    /// typed-out body, choices above the box, and a history log overlay) -
    /// Docs/기획문서_대화시네마틱구조설계.md ch.4. Contains no playback logic; the
    /// DialoguePlayer drives it through IDialogueView. Lines may carry effect
    /// markup (documents/dialogue-text-effects.md): the body is typed out with
    /// the effects animated; the log and choices show the static colours only.
    /// </summary>
    public class DialogueBoxUIView : MonoBehaviour, IDialogueView
    {
        private static readonly Color FocusedChoiceColor = new(1f, 0.9f, 0.3f);

        [SerializeField] private GameObject root;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private DialogueTextAnimator bodyAnimator;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private RectTransform choiceContainer;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private GameObject logPanel;
        [SerializeField] private TMP_Text logText;
        [SerializeField, Min(0f)] private float charactersPerSecond = 40f;

        [Tooltip("Font for every label in the box (incl. choices). Empty = keep each label's own font.")]
        [SerializeField] private TMP_FontAsset fontOverride;

        private readonly List<TMP_Text> choiceLabels = new();
        private readonly TextTypist typist = new();
        private Action<int> onChosen;
        private Action onFullyShown;
        private bool typing;
        private int focusedChoice;

        public bool IsLogOpen => logPanel != null && logPanel.activeSelf;

        private void Awake()
        {
            ApplyFont(speakerText);
            ApplyFont(bodyText);
            ApplyFont(hintText);
            ApplyFont(logText);

            // The body is laid out from the parsed plain text; TMP must not interpret it again.
            bodyText.richText = false;
            root.SetActive(false);
        }

        private void Update()
        {
            if (!typing)
            {
                return;
            }

            typist.Advance(Time.unscaledDeltaTime, charactersPerSecond);
            bodyText.maxVisibleCharacters = typist.VisibleCount;

            if (typist.IsDone)
            {
                FinishTyping();
            }
        }

        private void ApplyFont(TMP_Text label)
        {
            if (fontOverride != null && label != null)
            {
                label.font = fontOverride;
            }
        }

        public void Show(bool skippable)
        {
            root.SetActive(true);
            logPanel.SetActive(false);
            ClearChoices();
            hintText.text = skippable
                ? "F/Enter: 다음    Tab: 건너뛰기    L: 기록    Esc: 취소"
                : "F/Enter: 다음    L: 기록    Esc: 취소";
        }

        public void Hide()
        {
            typing = false;
            ClearChoices();
            logPanel.SetActive(false);
            root.SetActive(false);
        }

        public void ShowLine(string speaker, string text, Sprite portrait, Action fullyShown)
        {
            ClearChoices();
            speakerText.text = speaker;
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);

            var parsed = DialogueMarkup.Parse(text);
            foreach (var warning in parsed.Warnings)
            {
                Debug.LogWarning($"Dialogue text markup: {warning}");
            }

            onFullyShown = fullyShown;
            continueIndicator.SetActive(false);

            // The whole line is laid out up front and only revealed, so words do not jump between lines while typing.
            bodyText.text = parsed.Plain;
            if (bodyAnimator != null)
            {
                bodyAnimator.SetSpans(parsed.Spans);
            }

            if (charactersPerSecond <= 0f || parsed.Plain.Length == 0)
            {
                FinishTyping();
                return;
            }

            typist.Reset(parsed.Plain.Length, parsed.Pauses);
            bodyText.maxVisibleCharacters = 0;
            typing = true;
        }

        public void CompleteTyping()
        {
            if (typing)
            {
                typist.Complete();
                FinishTyping();
            }
        }

        private void FinishTyping()
        {
            bodyText.maxVisibleCharacters = int.MaxValue;
            typing = false;
            continueIndicator.SetActive(true);
            onFullyShown?.Invoke();
        }

        public void ShowChoices(IReadOnlyList<DialogueChoiceOption> options, Action<int> chosenCallback)
        {
            ClearChoices();
            continueIndicator.SetActive(false);
            onChosen = chosenCallback;

            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var button = Instantiate(choiceButtonPrefab, choiceContainer);
                var label = button.GetComponentInChildren<TMP_Text>();
                ApplyFont(label);
                label.text = DialogueMarkup.Parse(options[i].text).ToStaticMarkup();
                button.onClick.AddListener(() => Choose(index));
                choiceLabels.Add(label);
            }

            focusedChoice = 0;
            RefreshChoiceFocus();
        }

        public void MoveChoiceFocus(int delta)
        {
            if (choiceLabels.Count == 0)
            {
                return;
            }

            focusedChoice = (focusedChoice + delta + choiceLabels.Count) % choiceLabels.Count;
            RefreshChoiceFocus();
        }

        public void ConfirmFocusedChoice()
        {
            if (choiceLabels.Count > 0)
            {
                Choose(focusedChoice);
            }
        }

        public void ToggleLog(IReadOnlyList<DialogueLogEntry> log)
        {
            bool open = !logPanel.activeSelf;
            logPanel.SetActive(open);
            if (!open)
            {
                return;
            }

            var builder = new StringBuilder();
            foreach (var entry in log)
            {
                builder.Append(string.IsNullOrEmpty(entry.Speaker) ? string.Empty : entry.Speaker + ": ");
                builder.AppendLine(DialogueMarkup.Parse(entry.Text).ToStaticMarkup());
            }
            logText.text = builder.ToString();
        }

        private void Choose(int index)
        {
            var callback = onChosen;
            ClearChoices();
            callback?.Invoke(index);
        }

        private void RefreshChoiceFocus()
        {
            for (int i = 0; i < choiceLabels.Count; i++)
            {
                choiceLabels[i].color = i == focusedChoice ? FocusedChoiceColor : Color.white;
            }
        }

        private void ClearChoices()
        {
            foreach (Transform child in choiceContainer)
            {
                // Deactivate first: Destroy is deferred to end of frame, and a
                // just-clicked button must not stay clickable until then.
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            choiceLabels.Clear();
            onChosen = null;
        }
    }
}
