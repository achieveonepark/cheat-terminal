using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Achieve.CheatTerminal.Core;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>
    /// Default terminal view: a self-building uGUI overlay on its own Canvas.
    /// The canvas object is fully deactivated while closed, so it costs nothing
    /// (no rendering, no raycasts) when the terminal is not in use.
    /// </summary>
    [AddComponentMenu("Achieve.CheatTerminal/uGUI Terminal View")]
    public sealed class UGuiTerminalView : MonoBehaviour, ITerminalView
    {
        [SerializeField] private int _maxLines = 300;
        [SerializeField] private int _fontSize = 22;
        [SerializeField] private float _heightRatio = 0.5f;

        private GameObject _canvasGo;
        private Text _outputText;
        private InputField _input;
        private ScrollRect _scroll;
        private Text _suggestionText;
        private CommandHistory _history;

        private readonly Queue<string> _lines = new Queue<string>();
        private readonly StringBuilder _sb = new StringBuilder(4096);
        private bool _dirty;
        private bool _scrollToBottom;

        public bool IsOpen => _canvasGo != null && _canvasGo.activeSelf;

        public event Action<string> OnSubmit;
        public event Action<string> OnInputChanged;

        private void Awake()
        {
            BuildUi();
            SetOpen(false);
        }

        public void Bind(Terminal terminal) => _history = terminal?.History;

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);
        public void Toggle() => SetOpen(!IsOpen);

        public void AppendLine(string text, LogLevel level)
        {
            _lines.Enqueue(Colorize(text, level));
            while (_lines.Count > _maxLines)
                _lines.Dequeue();

            _dirty = true;
            _scrollToBottom = true;
        }

        public void Clear()
        {
            _lines.Clear();
            _dirty = true;
        }

        public void ShowSuggestions(IReadOnlyList<string> suggestions)
        {
            if (_suggestionText == null) return;
            if (suggestions == null || suggestions.Count == 0)
            {
                _suggestionText.text = string.Empty;
                return;
            }
            _suggestionText.text = string.Join("   ", suggestions);
        }

        private void LateUpdate()
        {
            if (!_dirty || !IsOpen) return;
            RebuildText();
            _dirty = false;
        }

        private void RebuildText()
        {
            _sb.Clear();
            foreach (var line in _lines)
                _sb.AppendLine(line);
            _outputText.text = _sb.ToString();

            if (_scrollToBottom)
            {
                Canvas.ForceUpdateCanvases();
                _scroll.verticalNormalizedPosition = 0f;
                _scrollToBottom = false;
            }
        }

        private void SetOpen(bool open)
        {
            if (_canvasGo == null) return;
            _canvasGo.SetActive(open);
            if (!open) return;

            _dirty = true;
            _scrollToBottom = true;
            _history?.ResetCursor();
            if (_input != null)
            {
                _input.text = string.Empty;
                _input.ActivateInputField();
            }
        }

        private void OnInputValueChanged(string value) => OnInputChanged?.Invoke(value);

        private void OnInputEndEdit(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            OnSubmit?.Invoke(value);
            _input.text = string.Empty;
            _input.ActivateInputField();
            if (_suggestionText != null) _suggestionText.text = string.Empty;
        }

        private void RecallPrevious()
        {
            string prev = _history?.Previous();
            if (prev != null) SetInputText(prev);
        }

        private void RecallNext()
        {
            string next = _history?.Next();
            if (next != null) SetInputText(next);
        }

        private void SetInputText(string text)
        {
            if (_input == null) return;
            _input.text = text;
            _input.caretPosition = text.Length;
            _input.selectionAnchorPosition = text.Length;
            _input.selectionFocusPosition = text.Length;
        }

        /// <summary>
        /// Receives uGUI navigation moves on the input field. Using IMoveHandler keeps
        /// arrow-key history navigation working under both the legacy Input Manager and
        /// the Input System package (no direct UnityEngine.Input access).
        /// </summary>
        private sealed class InputNavigator : MonoBehaviour, IMoveHandler
        {
            private UGuiTerminalView _view;
            public void Init(UGuiTerminalView view) => _view = view;

            public void OnMove(AxisEventData eventData)
            {
                if (_view == null) return;
                if (eventData.moveDir == MoveDirection.Up) _view.RecallPrevious();
                else if (eventData.moveDir == MoveDirection.Down) _view.RecallNext();
            }
        }

        private string Colorize(string text, LogLevel level)
        {
            string hex = level switch
            {
                LogLevel.Success => "7CFC7C",
                LogLevel.Warning => "FFD15C",
                LogLevel.Error => "FF6B6B",
                LogLevel.System => "6CC6FF",
                _ => "E6E6E6"
            };
            return $"<color=#{hex}>{text}</color>";
        }

        // ---- UI construction -------------------------------------------------

        private void BuildUi()
        {
            _canvasGo = new GameObject("Achieve.CheatTerminalCanvas");
            _canvasGo.transform.SetParent(transform, false);

            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasGo.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            // Root panel anchored to the top, covering the upper part of the screen.
            var panel = CreateRect("Panel", _canvasGo.transform);
            panel.anchorMin = new Vector2(0f, 1f - Mathf.Clamp01(_heightRatio));
            panel.anchorMax = new Vector2(1f, 1f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0.03f, 0.03f, 0.05f, 0.92f);

            BuildCloseButton(panel);
            BuildOutput(panel);
            BuildSuggestion(panel);
            BuildInput(panel);
        }

        private void BuildOutput(RectTransform parent)
        {
            var scrollGo = CreateRect("Output", parent);
            scrollGo.anchorMin = new Vector2(0f, 0f);
            scrollGo.anchorMax = new Vector2(1f, 1f);
            scrollGo.offsetMin = new Vector2(12f, 96f);
            scrollGo.offsetMax = new Vector2(-12f, -64f);

            _scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.scrollSensitivity = 30f;
            _scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect("Viewport", scrollGo);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.pivot = new Vector2(0f, 1f);
            var viewportImg = viewport.gameObject.AddComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0.25f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _scroll.viewport = viewport;

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.offsetMin = new Vector2(8f, 0f);
            content.offsetMax = new Vector2(-8f, 0f);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = content;

            _outputText = content.gameObject.AddComponent<Text>();
            ConfigureText(_outputText, TextAnchor.UpperLeft);
            _outputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _outputText.verticalOverflow = VerticalWrapMode.Overflow;
            _outputText.text = string.Empty;
        }

        private void BuildSuggestion(RectTransform parent)
        {
            var sg = CreateRect("Suggestions", parent);
            sg.anchorMin = new Vector2(0f, 0f);
            sg.anchorMax = new Vector2(1f, 0f);
            sg.pivot = new Vector2(0f, 0f);
            sg.offsetMin = new Vector2(12f, 64f);
            sg.offsetMax = new Vector2(-12f, 96f);

            _suggestionText = sg.gameObject.AddComponent<Text>();
            ConfigureText(_suggestionText, TextAnchor.MiddleLeft);
            _suggestionText.fontSize = Mathf.Max(12, _fontSize - 6);
            _suggestionText.color = new Color(0.55f, 0.75f, 0.95f, 1f);
            _suggestionText.text = string.Empty;
        }

        private void BuildInput(RectTransform parent)
        {
            var inputGo = CreateRect("Input", parent);
            inputGo.anchorMin = new Vector2(0f, 0f);
            inputGo.anchorMax = new Vector2(1f, 0f);
            inputGo.pivot = new Vector2(0.5f, 0f);
            inputGo.offsetMin = new Vector2(12f, 12f);
            inputGo.offsetMax = new Vector2(-12f, 56f);

            var bg = inputGo.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.16f, 1f);

            _input = inputGo.gameObject.AddComponent<InputField>();
            _input.lineType = InputField.LineType.SingleLine;

            var textRect = CreateRect("Text", inputGo);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);
            var text = textRect.gameObject.AddComponent<Text>();
            ConfigureText(text, TextAnchor.MiddleLeft);
            text.supportRichText = false;

            var phRect = CreateRect("Placeholder", inputGo);
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(12f, 4f);
            phRect.offsetMax = new Vector2(-12f, -4f);
            var placeholder = phRect.gameObject.AddComponent<Text>();
            ConfigureText(placeholder, TextAnchor.MiddleLeft);
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            placeholder.text = "Type a command and press Enter...";

            _input.textComponent = text;
            _input.placeholder = placeholder;
            _input.onValueChanged.AddListener(OnInputValueChanged);
            _input.onEndEdit.AddListener(OnInputEndEdit);

            // Disable selectable navigation so Up/Down won't jump to other widgets;
            // our InputNavigator turns those moves into command-history recall.
            var nav = _input.navigation;
            nav.mode = Navigation.Mode.None;
            _input.navigation = nav;
            _input.gameObject.AddComponent<InputNavigator>().Init(this);
        }

        private void BuildCloseButton(RectTransform parent)
        {
            var btnGo = CreateRect("Close", parent);
            btnGo.anchorMin = new Vector2(1f, 1f);
            btnGo.anchorMax = new Vector2(1f, 1f);
            btnGo.pivot = new Vector2(1f, 1f);
            btnGo.sizeDelta = new Vector2(52f, 52f);
            btnGo.anchoredPosition = new Vector2(-8f, -8f);

            var img = btnGo.gameObject.AddComponent<Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var btn = btnGo.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(Close);

            var labelRect = CreateRect("X", btnGo);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelRect.gameObject.AddComponent<Text>();
            ConfigureText(label, TextAnchor.MiddleCenter);
            label.text = "X";
            label.fontStyle = FontStyle.Bold;
        }

        private void ConfigureText(Text text, TextAnchor anchor)
        {
            text.font = GetDefaultFont();
            text.fontSize = _fontSize;
            text.color = Color.white;
            text.alignment = anchor;
            text.supportRichText = true;
            text.raycastTarget = false;
        }

        private static Font _cachedFont;

        private static Font GetDefaultFont()
        {
            if (_cachedFont != null) return _cachedFont;
            // LegacyRuntime.ttf is the built-in font in Unity 2022+/Unity 6.
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedFont == null)
                _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _cachedFont;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;

            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));

            // Pick the UI input module matching the project's "Active Input Handling".
            // If the Input System package is installed we must use its module: the legacy
            // StandaloneInputModule reads UnityEngine.Input and throws when legacy input
            // is disabled. Resolved by reflection so this package needs no hard dependency
            // on com.unity.inputsystem.
            var inputSystemModule = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemModule != null)
            {
                var module = go.AddComponent(inputSystemModule);
                // Assign default UI actions so the module actually receives input
                // (no-op if the project already configured it).
                var assign = inputSystemModule.GetMethod("AssignDefaultActions",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                assign?.Invoke(module, null);
            }
            else
            {
                go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            DontDestroyOnLoad(go);
        }
    }
}
