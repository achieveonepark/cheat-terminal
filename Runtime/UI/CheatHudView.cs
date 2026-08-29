using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>
    /// UI Toolkit cheat HUD. By default it covers the whole screen so every row is a large,
    /// thumb-sized touch target; set <see cref="FullScreen"/> to false for the old panel that
    /// slides in from the left edge. Every command registered on the terminal shows up here
    /// automatically, grouped by category: no-argument cheats run on tap, cheats that take
    /// arguments open a small inline input pre-filled with their usage.
    /// The panel is fully hidden (no rendering, no picking) while closed.
    /// </summary>
    [AddComponentMenu("Achieve.CheatTerminal/Cheat HUD")]
    public sealed class CheatHudView : MonoBehaviour
    {
        private const string SystemCategory = "System";

        /// <summary>List width (in panel units) one cheat column needs before a second one is added.</summary>
        private const float ColumnWidth = 620f;
        private const int MaxColumns = 3;

        private static readonly Color PanelColor = new Color(0.03f, 0.03f, 0.05f, 0.96f);
        private static readonly Color HeaderColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        private static readonly Color RowColor = new Color(0.10f, 0.12f, 0.16f, 0.9f);
        private static readonly Color RowBorderColor = new Color(0.20f, 0.24f, 0.30f, 1f);
        private static readonly Color AccentColor = new Color(0.42f, 0.78f, 1f, 1f);
        private static readonly Color NameColor = new Color(0.62f, 0.82f, 1f, 1f);
        private static readonly Color MutedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color SuccessColor = new Color(0.49f, 0.99f, 0.49f, 1f);
        private static readonly Color CloseColor = new Color(1f, 0.55f, 0.55f, 1f);

        [SerializeField] private bool _fullScreen = true;
        [SerializeField] private float _widthPercent = 42f;
        [SerializeField] private float _slideDuration = 0.18f;
        [SerializeField] private bool _includeSystemCommands = true;

        private Terminal _terminal;
        private UIDocument _document;
        private PanelSettings _panelSettings;
        private ThemeStyleSheet _generatedTheme;
        private VisualElement _root;
        private VisualElement _panel;
        private VisualElement _content;
        private ScrollView _list;
        private Label _statusLabel;
        private Label _countLabel;

        private readonly HashSet<string> _expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _filter = string.Empty;
        private bool _isOpen;
        private bool _dirty;
        private bool _rootVisible = true; // matches the default display of a fresh root element
        private float _slide; // 0 = off screen, 1 = fully visible
        private int _columns = 1;
        private Rect _appliedSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private Vector2Int _appliedScreen = new Vector2Int(-1, -1);

        public bool IsOpen => _isOpen;

        /// <summary>Raised when the user asks for the full terminal from the HUD header.</summary>
        public event Action OnConsoleRequested;

        /// <summary>Raised the moment the HUD starts opening (before the slide finishes).</summary>
        public event Action OnOpened;

        /// <summary>Raised the moment the HUD starts closing.</summary>
        public event Action OnClosed;

        /// <summary>Raised on every open/close with the new state, for one-line handlers.</summary>
        public event Action<bool> OnVisibilityChanged;

        /// <summary>
        /// True: the HUD covers the whole screen. False: it slides in from the left edge
        /// and takes <see cref="WidthPercent"/> of the screen.
        /// </summary>
        public bool FullScreen
        {
            get => _fullScreen;
            set
            {
                if (_fullScreen == value) return;
                _fullScreen = value;
                ApplyPanelMetrics();
                ApplySlide(_slide);
            }
        }

        /// <summary>Width of the sliding panel, in percent, when <see cref="FullScreen"/> is off.</summary>
        public float WidthPercent
        {
            get => _widthPercent;
            set
            {
                _widthPercent = value;
                ApplyPanelMetrics();
            }
        }

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
            OnOpened?.Invoke();
            OnVisibilityChanged?.Invoke(true);
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            OnClosed?.Invoke();
            OnVisibilityChanged?.Invoke(false);
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

            if (!_rootVisible) return;
            UpdateSafeArea();

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
            if (_panel == null) return;
            float eased = 1f - (1f - t) * (1f - t); // ease-out
            // Full screen rises into place, the narrow panel keeps sliding in from the left.
            _panel.style.translate = _fullScreen
                ? new Translate(0f, Length.Percent(4f * (1f - eased)))
                : new Translate(Length.Percent(-100f * (1f - eased)), 0f);
            _panel.style.opacity = Mathf.Lerp(0f, 1f, eased);
        }

        /// <summary>
        /// Keeps the content clear of notches and home indicators. Style units are not screen
        /// pixels (the panel scales with the screen), so the insets are converted through the
        /// ratio between the real screen and the resolved root size.
        /// </summary>
        private void UpdateSafeArea()
        {
            if (_content == null) return;

            var screen = new Vector2Int(Screen.width, Screen.height);
            var safe = _fullScreen ? Screen.safeArea : new Rect(0f, 0f, screen.x, screen.y);
            if (safe == _appliedSafeArea && screen == _appliedScreen)
                return;

            float rootWidth = _root.layout.width;
            if (float.IsNaN(rootWidth) || rootWidth <= 1f)
                return; // layout not resolved yet, try again next frame

            float pixelsPerUnit = screen.x / rootWidth;
            if (pixelsPerUnit <= 0f || float.IsNaN(pixelsPerUnit))
                return;

            _content.style.paddingLeft = safe.xMin / pixelsPerUnit;
            _content.style.paddingRight = (screen.x - safe.xMax) / pixelsPerUnit;
            _content.style.paddingTop = (screen.y - safe.yMax) / pixelsPerUnit;
            _content.style.paddingBottom = safe.yMin / pixelsPerUnit;

            _appliedSafeArea = safe;
            _appliedScreen = screen;
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

        /// <summary>
        /// Wide screens (tablets, the editor Game view) get two or three cheat columns instead
        /// of one very long list. Only a changed column count costs a rebuild.
        /// </summary>
        private void OnListResized(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            if (float.IsNaN(width) || width <= 0f)
                return;

            int columns = Mathf.Clamp(Mathf.FloorToInt(width / ColumnWidth), 1, MaxColumns);
            if (columns == _columns)
                return;

            _columns = columns;
            MarkDirty();
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
            label.style.fontSize = 26;
            label.style.marginTop = 18;
            label.style.marginBottom = 8;
            label.style.marginLeft = 4;
            // Always on a line of its own, whatever the column count is.
            label.style.flexBasis = Length.Percent(100f);
            return label;
        }

        private VisualElement CommandRow(ICommand command)
        {
            var row = new VisualElement();
            row.style.marginBottom = 10;
            row.style.backgroundColor = RowColor;
            row.style.borderTopLeftRadius = 12;
            row.style.borderTopRightRadius = 12;
            row.style.borderBottomLeftRadius = 12;
            row.style.borderBottomRightRadius = 12;
            SetBorder(row, RowBorderColor, 1f);
            ApplyColumnWidth(row);

            bool needsArgs = RequiresArguments(command);

            var head = new Button { text = string.Empty };
            head.style.flexDirection = FlexDirection.Column;
            head.style.alignItems = Align.FlexStart;
            head.style.justifyContent = Justify.Center;
            head.style.backgroundColor = Color.clear;
            head.style.minHeight = 104; // comfortable thumb target
            head.style.marginTop = 0;
            head.style.marginBottom = 0;
            head.style.marginLeft = 0;
            head.style.marginRight = 0;
            head.style.paddingTop = 18;
            head.style.paddingBottom = 18;
            head.style.paddingLeft = 22;
            head.style.paddingRight = 22;
            SetBorder(head, Color.clear, 0f);

            var title = new Label(needsArgs ? command.Name + "  ›" : command.Name);
            title.enableRichText = false;
            title.pickingMode = PickingMode.Ignore;
            title.style.color = NameColor;
            title.style.fontSize = 30;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            head.Add(title);

            string subtitle = string.IsNullOrEmpty(command.Description) ? command.Usage : command.Description;
            if (!string.IsNullOrEmpty(subtitle))
            {
                var description = new Label(subtitle);
                description.enableRichText = false;
                description.pickingMode = PickingMode.Ignore;
                description.style.color = MutedColor;
                description.style.fontSize = 22;
                description.style.marginTop = 4;
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

        private void ApplyColumnWidth(VisualElement row)
        {
            if (_columns <= 1)
            {
                row.style.flexBasis = Length.Percent(100f);
                row.style.marginRight = 0;
                return;
            }

            // Leave room for the gap between columns so the row still fits on one line.
            // No flex-grow: a half-empty last line keeps the column width instead of stretching.
            row.style.flexBasis = Length.Percent(100f / _columns - 1.5f);
            row.style.flexGrow = 0f;
            row.style.marginRight = 10;
        }

        private VisualElement BuildArgumentEditor(ICommand command)
        {
            var container = new VisualElement();
            container.style.paddingLeft = 22;
            container.style.paddingRight = 22;
            container.style.paddingBottom = 18;

            if (!string.IsNullOrEmpty(command.Usage))
            {
                var usage = new Label(command.Usage);
                usage.enableRichText = false;
                usage.style.color = MutedColor;
                usage.style.fontSize = 20;
                usage.style.marginBottom = 6;
                usage.style.whiteSpace = WhiteSpace.Normal;
                container.Add(usage);
            }

            var editor = new VisualElement();
            editor.style.flexDirection = FlexDirection.Row;
            editor.style.alignItems = Align.Center;

            var field = new TextField { value = command.Name + " " };
            field.style.flexGrow = 1f;
            field.style.marginRight = 10;
            StyleTextField(field, null);

            var run = new Button { text = "RUN" };
            run.style.color = SuccessColor;
            run.style.backgroundColor = new Color(0.14f, 0.20f, 0.14f, 1f);
            run.style.fontSize = 26;
            run.style.minWidth = 130;
            run.style.minHeight = 76;
            run.style.paddingLeft = 22;
            run.style.paddingRight = 22;
            run.style.paddingTop = 14;
            run.style.paddingBottom = 14;
            run.style.marginLeft = 0;
            run.style.marginRight = 0;
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
            _root.style.fontSize = 22;
            _root.style.color = Color.white;

            _panel = new VisualElement();
            _panel.style.height = Length.Percent(100f);
            _panel.style.backgroundColor = PanelColor;
            _panel.style.borderRightColor = RowBorderColor;
            _root.Add(_panel);

            // Padded by the device safe area; the panel background itself stays edge to edge.
            _content = new VisualElement();
            _content.style.flexGrow = 1f;
            _panel.Add(_content);

            ApplyPanelMetrics();

            BuildHeader();
            BuildSearch();
            BuildList();
            BuildStatusBar();
        }

        private void ApplyPanelMetrics()
        {
            if (_panel == null) return;

            if (_fullScreen)
            {
                _panel.style.width = Length.Percent(100f);
                _panel.style.minWidth = 0;
                _panel.style.borderRightWidth = 0;
            }
            else
            {
                _panel.style.width = Length.Percent(Mathf.Clamp(_widthPercent, 20f, 100f));
                _panel.style.minWidth = 300;
                _panel.style.borderRightWidth = 1;
            }

            // Force the safe-area insets to be recomputed for the new shape.
            _appliedSafeArea = new Rect(-1f, -1f, -1f, -1f);
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.backgroundColor = HeaderColor;
            header.style.paddingTop = 14;
            header.style.paddingBottom = 14;
            header.style.paddingLeft = 20;
            header.style.paddingRight = 12;

            var title = new Label("CHEATS");
            title.style.color = AccentColor;
            title.style.fontSize = 32;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            _countLabel = new Label("0");
            _countLabel.style.color = MutedColor;
            _countLabel.style.fontSize = 22;
            _countLabel.style.marginLeft = 10;
            _countLabel.style.flexGrow = 1f;
            header.Add(_countLabel);

            var console = new Button { text = ">_" };
            StyleHeaderButton(console, NameColor);
            console.clicked += () => OnConsoleRequested?.Invoke();
            header.Add(console);

            var close = new Button { text = "X" };
            StyleHeaderButton(close, CloseColor);
            close.clicked += Close;
            header.Add(close);

            _content.Add(header);
        }

        private void BuildSearch()
        {
            var search = new TextField();
            search.style.marginTop = 12;
            search.style.marginLeft = 16;
            search.style.marginRight = 16;
            StyleTextField(search, "search cheats...");
            search.RegisterValueChangedCallback(evt =>
            {
                _filter = evt.newValue ?? string.Empty;
                MarkDirty();
            });
            _content.Add(search);
        }

        private void BuildList()
        {
            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.style.flexGrow = 1f;
            _list.style.paddingLeft = 16;
            _list.style.paddingRight = 16;
            _list.style.marginTop = 8;
            _list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _list.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _list.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;

            // Rows flow into as many columns as the screen can hold.
            var content = _list.contentContainer;
            content.style.flexDirection = FlexDirection.Row;
            content.style.flexWrap = Wrap.Wrap;
            content.style.alignItems = Align.FlexStart;

            _list.RegisterCallback<GeometryChangedEvent>(OnListResized);
            _content.Add(_list);
        }

        private void BuildStatusBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = HeaderColor;
            bar.style.paddingTop = 10;
            bar.style.paddingBottom = 10;
            bar.style.paddingLeft = 18;
            bar.style.paddingRight = 12;

            _statusLabel = new Label(string.Empty);
            _statusLabel.enableRichText = false;
            _statusLabel.style.color = SuccessColor;
            _statusLabel.style.fontSize = 21;
            _statusLabel.style.flexGrow = 1f;
            _statusLabel.style.flexShrink = 1f;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            bar.Add(_statusLabel);

            // A second, thumb-reachable way out: the header X is far away on a tall phone.
            var close = new Button { text = "CLOSE" };
            close.style.color = CloseColor;
            close.style.backgroundColor = new Color(0.18f, 0.12f, 0.14f, 1f);
            close.style.unityFontStyleAndWeight = FontStyle.Bold;
            close.style.fontSize = 24;
            close.style.minWidth = 160;
            close.style.minHeight = 80;
            close.style.marginLeft = 10;
            close.style.marginRight = 0;
            close.style.marginTop = 0;
            close.style.marginBottom = 0;
            SetBorder(close, RowBorderColor, 1f);
            close.clicked += Close;
            bar.Add(close);

            _content.Add(bar);
        }

        private static Label Muted(string text)
        {
            var label = new Label(text);
            label.enableRichText = false;
            label.style.color = MutedColor;
            label.style.marginTop = 14;
            label.style.flexBasis = Length.Percent(100f);
            return label;
        }

        private static void StyleHeaderButton(Button button, Color color)
        {
            button.style.color = color;
            button.style.backgroundColor = new Color(0.14f, 0.17f, 0.22f, 1f);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.fontSize = 28;
            button.style.width = 96;
            button.style.height = 84;
            button.style.marginLeft = 8;
            button.style.marginRight = 0;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            SetBorder(button, RowBorderColor, 1f);
        }

        private static void StyleTextField(TextField field, string placeholder)
        {
            field.style.fontSize = 26;

            // "unity-text-input" is the inner editable element of every TextField.
            var input = field.Q("unity-text-input");
            if (input != null)
            {
                input.style.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 1f);
                input.style.color = Color.white;
                input.style.minHeight = 72;
                input.style.paddingTop = 12;
                input.style.paddingBottom = 12;
                input.style.paddingLeft = 14;
                input.style.paddingRight = 14;
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
            hint.style.left = 16;
            hint.style.top = 0;
            hint.style.bottom = 0;
            hint.style.unityTextAlign = TextAnchor.MiddleLeft;
            hint.style.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            hint.style.unityFontStyleAndWeight = FontStyle.Italic;
            hint.style.fontSize = 22;
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
            element.style.borderTopLeftRadius = 8;
            element.style.borderTopRightRadius = 8;
            element.style.borderBottomLeftRadius = 8;
            element.style.borderBottomRightRadius = 8;
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
