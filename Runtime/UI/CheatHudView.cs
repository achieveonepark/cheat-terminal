using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>
    /// UI Toolkit cheat HUD that slides in from the left edge. Every command registered on the
    /// terminal shows up here automatically, grouped by category: no-argument cheats run on tap,
    /// cheats that take arguments open a small inline input pre-filled with their usage.
    /// The panel is fully hidden (no rendering, no picking) while closed.
    /// </summary>
    [AddComponentMenu("Achieve.CheatTerminal/Cheat HUD")]
    public sealed class CheatHudView : MonoBehaviour
    {
        private const string SystemCategory = "System";

        private static readonly Color PanelColor = new Color(0.03f, 0.03f, 0.05f, 0.95f);
        private static readonly Color HeaderColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        private static readonly Color RowColor = new Color(0.10f, 0.12f, 0.16f, 0.9f);
        private static readonly Color RowBorderColor = new Color(0.20f, 0.24f, 0.30f, 1f);
        private static readonly Color AccentColor = new Color(0.42f, 0.78f, 1f, 1f);
        private static readonly Color NameColor = new Color(0.62f, 0.82f, 1f, 1f);
        private static readonly Color MutedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color SuccessColor = new Color(0.49f, 0.99f, 0.49f, 1f);

        [SerializeField] private float _widthPercent = 42f;
        [SerializeField] private float _slideDuration = 0.18f;
        [SerializeField] private bool _includeSystemCommands = true;

        private Terminal _terminal;
        private UIDocument _document;
        private PanelSettings _panelSettings;
        private ThemeStyleSheet _generatedTheme;
        private VisualElement _root;
        private VisualElement _panel;
        private ScrollView _list;
        private Label _statusLabel;
        private Label _countLabel;

        private readonly HashSet<string> _expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _filter = string.Empty;
        private bool _isOpen;
        private bool _dirty;
        private bool _rootVisible = true; // matches the default display of a fresh root element
        private float _slide; // 0 = off screen, 1 = fully visible

        public bool IsOpen => _isOpen;

        /// <summary>Raised when the user asks for the full terminal from the HUD header.</summary>
        public event Action OnConsoleRequested;

        private void Awake()
        {
            BuildUi();
            ApplySlide(0f);
            SetRootVisible(false);
        }

        public void Bind(Terminal terminal)
        {
            if (_terminal != null)
                _terminal.Registry.Changed -= MarkDirty;

            _terminal = terminal;

            if (_terminal != null)
                _terminal.Registry.Changed += MarkDirty;

            MarkDirty();
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            SetRootVisible(true);
            MarkDirty();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        private void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            UpdateSlide();

            if (!_dirty || !_isOpen) return;
            RebuildList();
            _dirty = false;
        }

        private void UpdateSlide()
        {
            float target = _isOpen ? 1f : 0f;
            if (Mathf.Approximately(_slide, target))
                return;

            float step = _slideDuration <= 0f
                ? 1f
                : Time.unscaledDeltaTime / _slideDuration;
            ApplySlide(Mathf.MoveTowards(_slide, target, step));

            // Fully closed: drop the panel out of the layout so it costs nothing.
            if (!_isOpen && _slide <= 0f)
                SetRootVisible(false);
        }

        private void SetRootVisible(bool visible)
        {
            if (_rootVisible == visible || _root == null) return;
            _rootVisible = visible;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplySlide(float t)
        {
            _slide = t;
            float eased = 1f - (1f - t) * (1f - t); // ease-out
            _panel.style.translate = new Translate(Length.Percent(-100f * (1f - eased)), 0f);
            _panel.style.opacity = Mathf.Lerp(0.2f, 1f, eased);
        }

        // ---- Cheat list ------------------------------------------------------

        private void RebuildList()
        {
            _list.Clear();

            var commands = CollectCommands();
            _countLabel.text = commands.Count.ToString();

            if (commands.Count == 0)
            {
                _list.Add(Muted(_terminal == null
                    ? "Terminal is not bound."
                    : "No cheats registered."));
                return;
            }

            foreach (var group in commands.GroupBy(c => c.Category))
            {
                _list.Add(CategoryHeader(group.Key));
                foreach (var command in group)
                    _list.Add(CommandRow(command));
            }
        }

        private List<ICommand> CollectCommands()
        {
            if (_terminal == null)
                return new List<ICommand>();

            IEnumerable<ICommand> query = _terminal.Registry.All;

            if (!_includeSystemCommands)
                query = query.Where(c => !IsSystem(c));

            if (!string.IsNullOrEmpty(_filter))
            {
                string filter = _filter.Trim();
                query = query.Where(c =>
                    Contains(c.Name, filter) ||
                    Contains(c.Description, filter) ||
                    Contains(c.Category, filter));
            }

            // Gameplay cheats first, engine/system helpers last.
            return query
                .OrderBy(c => IsSystem(c) ? 1 : 0)
                .ThenBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsSystem(ICommand command)
            => string.Equals(command.Category, SystemCategory, StringComparison.OrdinalIgnoreCase);

        private static bool Contains(string source, string value)
            => !string.IsNullOrEmpty(source) &&
               source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        private VisualElement CategoryHeader(string category)
        {
            var label = new Label(string.IsNullOrEmpty(category) ? "General" : category);
            label.enableRichText = false;
            label.style.color = AccentColor;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 20;
            label.style.marginTop = 14;
            label.style.marginBottom = 6;
            label.style.marginLeft = 4;
            return label;
        }

        private VisualElement CommandRow(ICommand command)
        {
            var row = new VisualElement();
            row.style.marginBottom = 6;
            row.style.backgroundColor = RowColor;
            row.style.borderTopLeftRadius = 8;
            row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8;
            row.style.borderBottomRightRadius = 8;
            SetBorder(row, RowBorderColor, 1f);

            bool needsArgs = RequiresArguments(command);

            var head = new Button { text = string.Empty };
            head.style.flexDirection = FlexDirection.Column;
            head.style.alignItems = Align.FlexStart;
            head.style.backgroundColor = Color.clear;
            head.style.marginTop = 0;
            head.style.marginBottom = 0;
            head.style.marginLeft = 0;
            head.style.marginRight = 0;
            head.style.paddingTop = 10;
            head.style.paddingBottom = 10;
            head.style.paddingLeft = 12;
            head.style.paddingRight = 12;
            SetBorder(head, Color.clear, 0f);

            var title = new Label(needsArgs ? command.Name + "  ›" : command.Name);
            title.enableRichText = false;
            title.pickingMode = PickingMode.Ignore;
            title.style.color = NameColor;
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            head.Add(title);

            string subtitle = string.IsNullOrEmpty(command.Description) ? command.Usage : command.Description;
            if (!string.IsNullOrEmpty(subtitle))
            {
                var description = new Label(subtitle);
                description.enableRichText = false;
                description.pickingMode = PickingMode.Ignore;
                description.style.color = MutedColor;
                description.style.fontSize = 15;
                description.style.marginTop = 2;
                description.style.whiteSpace = WhiteSpace.Normal;
                head.Add(description);
            }

            row.Add(head);

            if (!needsArgs)
            {
                head.clicked += () => Execute(command.Name);
                return row;
            }

            // Expansion is remembered by name so a list rebuild keeps open editors open.
            var argsRow = BuildArgumentEditor(command);
            argsRow.style.display = _expanded.Contains(command.Name) ? DisplayStyle.Flex : DisplayStyle.None;
            row.Add(argsRow);

            head.clicked += () =>
            {
                bool expand = !_expanded.Contains(command.Name);
                if (expand) _expanded.Add(command.Name);
                else _expanded.Remove(command.Name);
                argsRow.style.display = expand ? DisplayStyle.Flex : DisplayStyle.None;
            };

            return row;
        }

        private VisualElement BuildArgumentEditor(ICommand command)
        {
            var container = new VisualElement();
            container.style.paddingLeft = 12;
            container.style.paddingRight = 12;
            container.style.paddingBottom = 10;

            if (!string.IsNullOrEmpty(command.Usage))
            {
                var usage = new Label(command.Usage);
                usage.enableRichText = false;
                usage.style.color = MutedColor;
                usage.style.fontSize = 14;
                usage.style.marginBottom = 4;
                usage.style.whiteSpace = WhiteSpace.Normal;
                container.Add(usage);
            }

            var editor = new VisualElement();
            editor.style.flexDirection = FlexDirection.Row;
            editor.style.alignItems = Align.Center;

            var field = new TextField { value = command.Name + " " };
            field.style.flexGrow = 1f;
            field.style.marginRight = 8;
            StyleTextField(field, null);

            var run = new Button { text = "RUN" };
            run.style.color = SuccessColor;
            run.style.backgroundColor = new Color(0.14f, 0.20f, 0.14f, 1f);
            run.style.paddingLeft = 14;
            run.style.paddingRight = 14;
            run.style.paddingTop = 8;
            run.style.paddingBottom = 8;
            run.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorder(run, RowBorderColor, 1f);
            run.clicked += () => Execute(field.value);

            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                    return;
                Execute(field.value);
                evt.StopPropagation();
            });

            editor.Add(field);
            editor.Add(run);
            container.Add(editor);
            return container;
        }

        private static bool RequiresArguments(ICommand command)
        {
            string usage = command.Usage;
            if (string.IsNullOrEmpty(usage))
                return false;
            return usage.IndexOf('<') >= 0 || usage.IndexOf('[') >= 0;
        }

        private void Execute(string input)
        {
            if (_terminal == null || string.IsNullOrWhiteSpace(input))
                return;

            _terminal.Execute(input);
            _statusLabel.text = "▶ " + input.Trim();
        }

        // ---- UI construction -------------------------------------------------

        private void BuildUi()
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.name = "Achieve.CheatHudPanelSettings";
            _panelSettings.themeStyleSheet = LoadTheme();
            _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panelSettings.referenceResolution = new Vector2Int(1080, 1920);
            _panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _panelSettings.match = 0.5f;
            _panelSettings.sortingOrder = 1000f;

            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = _panelSettings;

            _root = _document.rootVisualElement;
            if (_root == null)
            {
                // The document builds its tree on enable; it had no panel settings back then.
                _document.enabled = false;
                _document.enabled = true;
                _root = _document.rootVisualElement;
            }

            // The empty area next to the panel must never swallow gameplay input.
            _root.pickingMode = PickingMode.Ignore;
            _root.style.flexDirection = FlexDirection.Row;
            // Inherited by every child, so text renders even with an empty theme.
            _root.style.unityFont = GetDefaultFont();
            _root.style.fontSize = 18;
            _root.style.color = Color.white;

            _panel = new VisualElement();
            _panel.style.width = Length.Percent(Mathf.Clamp(_widthPercent, 20f, 100f));
            _panel.style.minWidth = 300;
            _panel.style.height = Length.Percent(100f);
            _panel.style.backgroundColor = PanelColor;
            _panel.style.borderRightWidth = 1;
            _panel.style.borderRightColor = RowBorderColor;
            _root.Add(_panel);

            BuildHeader();
            BuildSearch();
            BuildList();
            BuildStatusBar();
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.backgroundColor = HeaderColor;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;
            header.style.paddingLeft = 14;
            header.style.paddingRight = 10;

            var title = new Label("CHEATS");
            title.style.color = AccentColor;
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            _countLabel = new Label("0");
            _countLabel.style.color = MutedColor;
            _countLabel.style.fontSize = 16;
            _countLabel.style.marginLeft = 8;
            _countLabel.style.flexGrow = 1f;
            header.Add(_countLabel);

            var console = new Button { text = ">_" };
            StyleHeaderButton(console, NameColor);
            console.clicked += () => OnConsoleRequested?.Invoke();
            header.Add(console);

            var close = new Button { text = "X" };
            StyleHeaderButton(close, new Color(1f, 0.55f, 0.55f, 1f));
            close.clicked += Close;
            header.Add(close);

            _panel.Add(header);
        }

        private void BuildSearch()
        {
            var search = new TextField();
            search.style.marginTop = 10;
            search.style.marginLeft = 12;
            search.style.marginRight = 12;
            StyleTextField(search, "search cheats...");
            search.RegisterValueChangedCallback(evt =>
            {
                _filter = evt.newValue ?? string.Empty;
                MarkDirty();
            });
            _panel.Add(search);
        }

        private void BuildList()
        {
            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.style.flexGrow = 1f;
            _list.style.paddingLeft = 12;
            _list.style.paddingRight = 12;
            _list.style.marginTop = 6;
            _list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _list.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _list.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            _panel.Add(_list);
        }

        private void BuildStatusBar()
        {
            _statusLabel = new Label(string.Empty);
            _statusLabel.enableRichText = false;
            _statusLabel.style.color = SuccessColor;
            _statusLabel.style.fontSize = 15;
            _statusLabel.style.backgroundColor = HeaderColor;
            _statusLabel.style.paddingTop = 8;
            _statusLabel.style.paddingBottom = 8;
            _statusLabel.style.paddingLeft = 14;
            _statusLabel.style.paddingRight = 14;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _panel.Add(_statusLabel);
        }

        private static Label Muted(string text)
        {
            var label = new Label(text);
            label.enableRichText = false;
            label.style.color = MutedColor;
            label.style.marginTop = 12;
            return label;
        }

        private static void StyleHeaderButton(Button button, Color color)
        {
            button.style.color = color;
            button.style.backgroundColor = new Color(0.14f, 0.17f, 0.22f, 1f);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.fontSize = 20;
            button.style.width = 52;
            button.style.height = 44;
            button.style.marginLeft = 6;
            button.style.marginRight = 0;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            SetBorder(button, RowBorderColor, 1f);
        }

        private static void StyleTextField(TextField field, string placeholder)
        {
            field.style.fontSize = 18;

            // "unity-text-input" is the inner editable element of every TextField.
            var input = field.Q("unity-text-input");
            if (input != null)
            {
                input.style.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 1f);
                input.style.color = Color.white;
                input.style.paddingTop = 8;
                input.style.paddingBottom = 8;
                input.style.paddingLeft = 10;
                input.style.paddingRight = 10;
                SetBorder(input, RowBorderColor, 1f);
            }

            if (!string.IsNullOrEmpty(placeholder))
                AddPlaceholder(field, placeholder);
        }

        /// <summary>
        /// Draws hint text over an empty field. Done by hand instead of relying on a
        /// theme-provided placeholder so the HUD looks the same with or without a theme.
        /// </summary>
        private static void AddPlaceholder(TextField field, string placeholder)
        {
            var hint = new Label(placeholder);
            hint.enableRichText = false;
            hint.pickingMode = PickingMode.Ignore;
            hint.style.position = Position.Absolute;
            hint.style.left = 12;
            hint.style.top = 0;
            hint.style.bottom = 0;
            hint.style.unityTextAlign = TextAnchor.MiddleLeft;
            hint.style.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            hint.style.unityFontStyleAndWeight = FontStyle.Italic;
            hint.style.fontSize = 16;
            hint.style.display = string.IsNullOrEmpty(field.value) ? DisplayStyle.Flex : DisplayStyle.None;

            field.Add(hint);
            field.RegisterValueChangedCallback(evt =>
                hint.style.display = string.IsNullOrEmpty(evt.newValue)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None);
        }

        private static void SetBorder(VisualElement element, Color color, float width)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopLeftRadius = 6;
            element.style.borderTopRightRadius = 6;
            element.style.borderBottomLeftRadius = 6;
            element.style.borderBottomRightRadius = 6;
        }

        private ThemeStyleSheet LoadTheme()
        {
            // Runtime-built panels have no theme asset to reference. Use the project's default
            // runtime theme when it is reachable, otherwise an empty one: every element in this
            // HUD is styled inline, so no theme rules are required.
            var theme = Resources.Load<ThemeStyleSheet>("unity-default-runtime-theme");
            if (theme != null)
                return theme;

            _generatedTheme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            _generatedTheme.name = "Achieve.CheatHudTheme";
            return _generatedTheme;
        }

        private static Font _cachedFont;

        private static Font GetDefaultFont()
        {
            if (_cachedFont != null) return _cachedFont;
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedFont == null)
                _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _cachedFont;
        }

        private void OnDestroy()
        {
            if (_terminal != null)
                _terminal.Registry.Changed -= MarkDirty;

            if (_panelSettings != null)
                Destroy(_panelSettings);
            if (_generatedTheme != null)
                Destroy(_generatedTheme);
        }
    }
}
