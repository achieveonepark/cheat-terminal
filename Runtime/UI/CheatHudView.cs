using System;
using System.Collections.Generic;
using System.Linq;
using Achieve.CheatTerminal.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>
    /// UI Toolkit cheat HUD. By default it covers the whole screen so every row is a large,
    /// thumb-sized touch target; set <see cref="FullScreen"/> to false for the old panel that
    /// slides in from the left edge. Every command registered on the terminal shows up here
    /// automatically, grouped by category, with named parameter fields when needed and
    /// a dedicated Run button for every command.
    /// The panel is fully hidden (no rendering, no picking) while closed.
    /// </summary>
    [AddComponentMenu("Achieve.CheatTerminal/Cheat HUD")]
    public sealed class CheatHudView : MonoBehaviour
    {
        private const string SystemCategory = "System";

        /// <summary>List width (in panel units) one cheat column needs before a second one is added.</summary>
        private const float ColumnWidth = 700f;
        private const int MaxColumns = 3;

        private static readonly Color PanelColor = new Color(0.035f, 0.047f, 0.065f, 0.99f);
        private static readonly Color HeaderColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        private static readonly Color RowColor = new Color(0.10f, 0.12f, 0.16f, 0.9f);
        private static readonly Color RowBorderColor = new Color(0.20f, 0.24f, 0.30f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.89f, 0.76f, 1f);
        private static readonly Color NameColor = new Color(0.91f, 0.95f, 0.98f, 1f);
        private static readonly Color MutedColor = new Color(0.56f, 0.64f, 0.71f, 1f);
        private static readonly Color SuccessColor = new Color(0.40f, 0.89f, 0.76f, 1f);
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

        private readonly Dictionary<string, string[]> _values = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        private VisualElement _categories;
        private string _category;
        private Label _activeResult;
        private bool _receivedOutput;
        private bool _executionError;
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
            {
                _terminal.Registry.Changed -= MarkDirty;
                if (_terminal.Output is BufferedOutput previous) previous.LineWritten -= OnOutput;
            }

            _terminal = terminal;

            if (_terminal != null)
            {
                _terminal.Registry.Changed += MarkDirty;
                if (_terminal.Output is BufferedOutput output) output.LineWritten += OnOutput;
            }

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
            RebuildCategories();

            var commands = CollectCommands();
            _countLabel.text = commands.Count.ToString();

            if (commands.Count == 0)
            {
                _list.Add(Muted(_terminal == null
                    ? "Terminal is not bound."
                    : "No matching commands. Try another category or search."));
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

            if (!string.IsNullOrEmpty(_category))
                query = query.Where(c => string.Equals(c.Category, _category, StringComparison.OrdinalIgnoreCase));

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
            var row = new VisualElement { name = "command-" + command.Name };
            row.style.marginBottom = 16;
            row.style.paddingTop = 24;
            row.style.paddingLeft = 24;
            row.style.paddingRight = 24;
            row.style.paddingBottom = 24;
            row.style.backgroundColor = RowColor;
            row.style.flexShrink = 0;
            SetBorder(row, RowBorderColor, 1f);
            ApplyColumnWidth(row);

            var title = new Label(command.Name) { enableRichText = false };
            title.style.flexShrink = 0;
            title.style.color = NameColor;
            title.style.fontSize = 30;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(title);
            var description = Muted(command.Description);
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.fontSize = 22;
            description.style.marginTop = 6;
            description.style.marginBottom = 18;
            row.Add(description);
            row.Add(BuildArgumentEditor(command));
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
            var form = new CommandForm(command);
            if (!_values.TryGetValue(command.Name, out var values) || values.Length != form.Parameters.Count)
            {
                values = new string[form.Parameters.Count];
                _values[command.Name] = values;
            }

            var container = new VisualElement();
            container.style.flexShrink = 0;
            var fields = new VisualElement();
            fields.style.flexShrink = 0;
            fields.style.flexDirection = FlexDirection.Row;
            fields.style.flexWrap = Wrap.Wrap;
            container.Add(fields);
            var result = Muted(form.Parameters.Count == 0 ? "Ready to run" : "Fill the parameters, then run.");
            result.name = "result";
            result.style.whiteSpace = WhiteSpace.Normal;
            result.style.fontSize = 20;
            result.style.marginTop = 12;
            result.style.maxHeight = 110;
            result.style.overflow = Overflow.Hidden;
            var run = new Button { text = "Run " + command.Name, name = "run" };
            StyleHeaderButton(run, PanelColor);
            run.style.width = Length.Percent(100f);
            run.style.marginLeft = 0;
            run.style.marginTop = 16;
            run.style.backgroundColor = AccentColor;
            run.style.fontSize = 24;

            void Refresh()
            {
                bool valid = form.TryBuild(values, out _, out string error);
                run.SetEnabled(valid);
                run.style.opacity = valid ? 1f : .4f;
                result.text = valid ? "Ready to run" : error;
                result.style.color = MutedColor;
            }

            for (int i = 0; i < form.Parameters.Count; i++)
            {
                int index = i;
                var parameter = form.Parameters[i];
                var group = new VisualElement();
                group.style.flexBasis = Length.Percent(form.Parameters.Count <= 3 && parameter.Suggestions.Length == 0 ? 100f / form.Parameters.Count - 2f : 100f);
                group.style.flexGrow = 1;
                group.style.minWidth = 130;
                group.style.marginRight = 8;
                group.style.marginBottom = 12;
                var label = Muted((parameter.Suggestions.Length > 0 ? "Value" : parameter.Name.TrimEnd('.')) + (parameter.Optional ? " (optional)" : " *"));
                label.style.marginTop = 0;
                label.style.fontSize = 20;
                label.style.marginBottom = 6;
                group.Add(label);
                var field = new TextField { name = "parameter-" + i, value = values[i] ?? string.Empty };
                StyleTextField(field, parameter.Multiple ? "Values separated by spaces" : "Enter value");
                field.RegisterValueChangedCallback(evt => { values[index] = evt.newValue; Refresh(); });
                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
                    if (form.TryBuild(values, out string input, out _)) Execute(input, result);
                    evt.StopPropagation();
                });
                group.Add(field);
                if (parameter.Suggestions.Length > 0)
                {
                    var choices = new VisualElement();
                    choices.style.flexDirection = FlexDirection.Row;
                    choices.style.flexWrap = Wrap.Wrap;
                    foreach (string suggestion in parameter.Suggestions)
                    {
                        string value = suggestion;
                        var choice = new Button(() => field.value = value) { text = value };
                        StyleHeaderButton(choice, AccentColor);
                        choice.style.width = StyleKeyword.Auto;
                        choice.style.minWidth = 104;
                        choice.style.fontSize = 20;
                        choice.style.paddingLeft = 16;
                        choice.style.paddingRight = 16;
                        choice.style.marginLeft = 0;
                        choice.style.marginRight = 8;
                        choice.style.marginTop = 8;
                        choices.Add(choice);
                    }
                    group.Add(choices);
                }
                fields.Add(group);
            }

            run.clicked += () =>
            {
                if (form.TryBuild(values, out string input, out _)) Execute(input, result);
            };
            container.Add(run);
            container.Add(result);
            Refresh();
            return container;
        }

        private void Execute(string input, Label result)
        {
            if (_terminal == null || string.IsNullOrWhiteSpace(input)) return;
            _activeResult = result;
            _receivedOutput = false;
            _executionError = false;
            try
            {
                _terminal.Execute(input);
                if (!_receivedOutput)
                {
                    result.text = "Executed: " + input;
                    result.style.color = AccentColor;
                    _statusLabel.text = result.text;
                    _statusLabel.style.color = AccentColor;
                }
            }
            finally { _activeResult = null; }
        }

        private void OnOutput(string text, LogLevel level)
        {
            if (_activeResult == null) return;
            if (_executionError && level != LogLevel.Error) return;
            _receivedOutput = true;
            _executionError |= level == LogLevel.Error;
            Color color = level == LogLevel.Error ? CloseColor : level == LogLevel.Warning ? new Color(1f, .79f, .40f) : AccentColor;
            _activeResult.text = text;
            _activeResult.style.color = color;
            _statusLabel.text = text;
            _statusLabel.style.color = color;
        }

        private void RebuildCategories()
        {
            _categories.Clear();
            if (_terminal == null) return;
            var categories = _terminal.Registry.All.Where(c => _includeSystemCommands || !IsSystem(c))
                .Select(c => c.Category).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => string.Equals(c, SystemCategory, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
            if (_category != null && !categories.Contains(_category, StringComparer.OrdinalIgnoreCase)) _category = null;
            AddCategory(null, "All");
            foreach (string category in categories) AddCategory(category, category);
        }

        private void AddCategory(string category, string label)
        {
            bool selected = string.Equals(_category, category, StringComparison.OrdinalIgnoreCase);
            var button = new Button(() => { _category = category; MarkDirty(); }) { text = label };
            StyleHeaderButton(button, selected ? PanelColor : MutedColor);
            button.style.backgroundColor = selected ? AccentColor : HeaderColor;
            button.style.width = StyleKeyword.Auto;
            button.style.minWidth = 104;
            button.style.paddingLeft = 20;
            button.style.paddingRight = 20;
            button.style.fontSize = 22;
            button.style.marginLeft = 0;
            button.style.marginRight = 10;
            _categories.Add(button);
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
            _content.style.minHeight = 0;
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
            header.style.flexShrink = 0;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.backgroundColor = HeaderColor;
            header.style.paddingTop = 14;
            header.style.paddingBottom = 14;
            header.style.paddingLeft = 20;
            header.style.paddingRight = 12;

            var branding = new VisualElement();
            branding.style.flexGrow = 1;
            var eyebrow = Muted("ACHIEVE / DEVELOPER TOOLS");
            eyebrow.style.fontSize = 20;
            eyebrow.style.marginTop = 0;
            branding.Add(eyebrow);
            var title = new Label("Cheat Library");
            title.style.color = AccentColor;
            title.style.fontSize = 32;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            branding.Add(title);
            header.Add(branding);

            _countLabel = new Label("0");
            _countLabel.style.color = MutedColor;
            _countLabel.style.fontSize = 22;
            _countLabel.style.marginLeft = 10;
            _countLabel.style.marginRight = 20;
            header.Add(_countLabel);

            var console = new Button { text = "Terminal / F2" };
            StyleHeaderButton(console, NameColor);
            console.style.width = 220;
            console.style.fontSize = 22;
            console.clicked += () => OnConsoleRequested?.Invoke();
            header.Add(console);

            var close = new Button { text = "Close" };
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
            StyleTextField(search, "Search commands, descriptions or categories...");
            search.RegisterValueChangedCallback(evt =>
            {
                _filter = evt.newValue ?? string.Empty;
                MarkDirty();
            });
            search.name = "cheat-search";
            _content.Add(search);
            var categoryScroll = new ScrollView(ScrollViewMode.Horizontal);
            categoryScroll.style.flexShrink = 0;
            categoryScroll.style.height = 116;
            categoryScroll.style.marginLeft = 20;
            categoryScroll.style.marginRight = 20;
            categoryScroll.style.marginTop = 14;
            categoryScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            categoryScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _categories = categoryScroll.contentContainer;
            _categories.style.flexDirection = FlexDirection.Row;
            _content.Add(categoryScroll);
        }

        private void BuildList()
        {
            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.style.flexGrow = 1f;
            _list.style.minHeight = 0;
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
            bar.style.flexShrink = 0;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = HeaderColor;
            bar.style.paddingTop = 10;
            bar.style.paddingBottom = 10;
            bar.style.paddingLeft = 18;
            bar.style.paddingRight = 12;

            _statusLabel = new Label("F1  Cheat Library    /    F2  Terminal    /    Esc  Close");
            _statusLabel.enableRichText = false;
            _statusLabel.style.color = SuccessColor;
            _statusLabel.style.fontSize = 21;
            _statusLabel.style.flexGrow = 1f;
            _statusLabel.style.flexShrink = 1f;
            _statusLabel.style.minWidth = 0;
            _statusLabel.style.maxHeight = 110;
            _statusLabel.style.overflow = Overflow.Hidden;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            bar.Add(_statusLabel);

            // A second, thumb-reachable way out: the header X is far away on a tall phone.
            var close = new Button { text = "CLOSE" };
            close.style.color = CloseColor;
            close.style.backgroundColor = new Color(0.18f, 0.12f, 0.14f, 1f);
            close.style.unityFontStyleAndWeight = FontStyle.Bold;
            close.style.fontSize = 24;
            close.style.minWidth = 160;
            close.style.minHeight = 104;
            close.style.unityTextAlign = TextAnchor.MiddleCenter;
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
            label.style.flexShrink = 0;
            return label;
        }

        private static void StyleHeaderButton(Button button, Color color)
        {
            button.style.color = color;
            button.style.backgroundColor = new Color(0.14f, 0.17f, 0.22f, 1f);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.fontSize = 28;
            button.style.width = 120;
            button.style.minHeight = 104;
            button.style.flexShrink = 0;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.marginLeft = 8;
            button.style.marginRight = 0;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            SetBorder(button, RowBorderColor, 1f);
        }

        private static void StyleTextField(TextField field, string placeholder)
        {
            field.style.fontSize = 24;
            field.style.minHeight = 104;
            field.style.flexShrink = 0;
            field.style.marginLeft = 0;
            field.style.marginRight = 0;

            // "unity-text-input" is the inner editable element of every TextField.
            var input = field.Q("unity-text-input");
            if (input != null)
            {
                input.style.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 1f);
                input.style.color = Color.white;
                input.style.minHeight = 104;
                input.style.flexGrow = 1;
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
            {
                _terminal.Registry.Changed -= MarkDirty;
                if (_terminal.Output is BufferedOutput previous) previous.LineWritten -= OnOutput;
            }

            if (_panelSettings != null)
                Destroy(_panelSettings);
            if (_generatedTheme != null)
                Destroy(_generatedTheme);
        }
    }
}
