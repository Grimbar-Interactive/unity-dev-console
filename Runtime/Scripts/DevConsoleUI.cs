using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GI.UnityToolkit.DevConsole
{
    /// <summary>
    /// The in-game dev console. Toggled with ~, it focuses the input field when opened and runs each submitted
    /// line through <see cref="DevCommandRegistry"/>. Put this on an object that stays active, not on the canvas
    /// it toggles, or Update stops running while the console is closed.
    /// </summary>
    public class DevConsoleUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform logContent;
        [SerializeField] private TMP_Text logEntryPrefab;
        [SerializeField, Min(10)] private int maxEntries = 100;

        // One TMP_Text per line of output. Appending only rebuilds the new entry's mesh, and past the limit the
        // oldest entry is recycled, so the cost of a line doesn't grow with the history.
        private readonly Queue<TMP_Text> _entries = new();

        public static bool IsOpen { get; private set; }

        /// <summary>Raised when the console opens or closes, so gameplay input can be paused while typing.</summary>
        public static event Action<bool> OpenChanged;

        private void Awake()
        {
            inputField.onValidateInput += (_, _, c) => c is '`' or '~' ? '\0' : c;
            inputField.onSubmit.AddListener(Submit);
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (IsOpen) SetOpen(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
                SetOpen(!IsOpen);
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;
            canvas.enabled = open;

            if (open)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
            else
            {
                inputField.DeactivateInputField();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }

            OpenChanged?.Invoke(open);
        }

        private void Submit(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                Refocus();
                return;
            }

            // noparse so what the user typed is shown as typed, never read as markup.
            Append($"<color=#9a9a9a>> <noparse>{line}</noparse></color>");
            var result = DevCommandRegistry.Execute(line);
            if (!string.IsNullOrEmpty(result)) Append(result);

            inputField.text = string.Empty;
            Refocus();
        }

        private void Refocus()
        {
            inputField.Select();
            inputField.ActivateInputField();
        }

        private void Append(string text)
        {
            TMP_Text entry;
            if (_entries.Count >= maxEntries)
            {
                entry = _entries.Dequeue();
                entry.transform.SetAsLastSibling();
            }
            else
            {
                entry = Instantiate(logEntryPrefab, logContent);
            }

            entry.text = text;
            _entries.Enqueue(entry);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}