using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialogue
{
    /// <summary>
    /// The text-dialogue presentation (bottom box with portrait, speaker name,
    /// typed-out body, choices above the box, and a history log overlay) -
    /// Docs/기획문서_대화시네마틱구조설계.md ch.4. Contains no playback logic; the
    /// DialoguePlayer drives it through IDialogueView.
    /// </summary>
    public class DialogueBoxUIView : MonoBehaviour, IDialogueView
    {
        private static readonly Color FocusedChoiceColor = new(1f, 0.9f, 0.3f);

        [SerializeField] private GameObject root;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private Text hintText;
        [SerializeField] private RectTransform choiceContainer;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private GameObject logPanel;
        [SerializeField] private Text logText;
        [SerializeField, Min(0f)] private float charactersPerSecond = 40f;

        private readonly List<Text> choiceLabels = new();
        private Action<int> onChosen;
        private Action onFullyShown;
        private string fullText = string.Empty;
        private float shownCharacters;
        private bool typing;
        private int focusedChoice;

        public bool IsLogOpen => logPanel != null && logPanel.activeSelf;

        private void Awake() => root.SetActive(false);

        private void Update()
        {
            if (!typing)
            {
                return;
            }

            shownCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            int count = Mathf.Min(fullText.Length, Mathf.FloorToInt(shownCharacters));
            bodyText.text = fullText.Substring(0, count);

            if (count >= fullText.Length)
            {
                FinishTyping();
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

            fullText = text ?? string.Empty;
            onFullyShown = fullyShown;
            continueIndicator.SetActive(false);

            if (charactersPerSecond <= 0f || fullText.Length == 0)
            {
                bodyText.text = fullText;
                FinishTyping();
                return;
            }

            shownCharacters = 0f;
            bodyText.text = string.Empty;
            typing = true;
        }

        public void CompleteTyping()
        {
            if (typing)
            {
                bodyText.text = fullText;
                FinishTyping();
            }
        }

        private void FinishTyping()
        {
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
                var label = button.GetComponentInChildren<Text>();
                label.text = options[i].text;
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
                builder.AppendLine(entry.Text);
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
