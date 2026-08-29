using System;
using System.Collections.Generic;
using Achieve.CheatTerminal.Core;
using Achieve.CheatTerminal.Modules;
using Achieve.CheatTerminal.UI;
using UnityEngine;

namespace Achieve.CheatTerminal
{
    /// <summary>
    /// Runtime entry point. Creates the concrete <see cref="Achieve.CheatTerminal.Terminal"/> core,
    /// attaches the uGUI view, the (gesture-revealed) corner trigger and the UI Toolkit cheat HUD,
    /// installs built-in modules, and survives scene loads.
    ///
    /// Gestures (unscaled time, anywhere on screen):
    /// four-finger triple tap toggles the corner handle, three-finger triple tap toggles the cheat HUD.
    /// Keyboard (editor and desktop): F9 handle, F10 cheat HUD, F1 or ` for the console.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TerminalBehaviour : MonoBehaviour
    {
        private const int TriggerGestureFingers = 4;
        private const int CheatHudGestureFingers = 3;
        private const int GestureTaps = 3;

        public static TerminalBehaviour Instance { get; private set; }

        public Terminal Terminal { get; private set; }
        public ITerminalView View { get; private set; }
        public TerminalCornerTrigger Trigger { get; private set; }
        public CheatHudView CheatHud { get; private set; }

        [SerializeField] private TerminalShortcutKey _consoleKey = TerminalShortcutKey.F1;
        [SerializeField] private TerminalShortcutKey _consoleOpenKey = TerminalShortcutKey.BackQuote;
        [SerializeField] private bool _pauseWhileOpen;
        [SerializeField] private float _openTimeScale;

        private MultiFingerTapGesture _triggerGesture;
        private MultiFingerTapGesture _cheatHudGesture;

        private bool _consoleOpen;
        private bool _cheatHudOpen;
        private bool _anyOpen;
        private bool _timeScaleOverridden;
        private float _savedTimeScale = 1f;

        /// <summary>Raised whenever the console is shown or hidden, with the new state.</summary>
        public event Action<bool> OnConsoleVisibilityChanged;

        /// <summary>Raised whenever the cheat HUD is shown or hidden, with the new state.</summary>
        public event Action<bool> OnCheatHudVisibilityChanged;

        /// <summary>
        /// Raised when the terminal surfaces as a whole appear or disappear: true when the
        /// console or the cheat HUD opens, false once both are closed. The natural place to
        /// pause gameplay, mute audio or drop <see cref="Time.timeScale"/>.
        /// </summary>
        public event Action<bool> OnVisibilityChanged;

        /// <summary>True while the console or the cheat HUD is on screen.</summary>
        public bool AnyOpen => _anyOpen;

        /// <summary>Keyboard key that toggles the console (editor/desktop). Defaults to F1.</summary>
        public TerminalShortcutKey ConsoleKey
        {
            get => _consoleKey;
            set => _consoleKey = value;
        }

        /// <summary>
        /// Extra key that only opens the console, never closes it: it stays out of the way
        /// while you are typing a command. Defaults to the backquote (`) key.
        /// </summary>
        public TerminalShortcutKey ConsoleOpenKey
        {
            get => _consoleOpenKey;
            set => _consoleOpenKey = value;
        }

        /// <summary>
        /// Opt-in convenience built on <see cref="OnVisibilityChanged"/>: while the console or
        /// the cheat HUD is open, <see cref="Time.timeScale"/> is forced to
        /// <see cref="OpenTimeScale"/> and restored to its previous value on close.
        /// </summary>
        public bool PauseWhileOpen
        {
            get => _pauseWhileOpen;
            set
            {
                if (_pauseWhileOpen == value) return;
                _pauseWhileOpen = value;
                ApplyTimeScale(_anyOpen);
            }
        }

        /// <summary>Time scale applied while open when <see cref="PauseWhileOpen"/> is on (default 0).</summary>
        public float OpenTimeScale
        {
            get => _openTimeScale;
            set
            {
                _openTimeScale = Mathf.Max(0f, value);
                if (_timeScaleOverridden)
                    Time.timeScale = _openTimeScale;
            }
        }

        private readonly List<ITerminalModule> _modules = new List<ITerminalModule>();
        public IReadOnlyList<ITerminalModule> Modules => _modules;

        /// <summary>
        /// Auto-creates the terminal in the editor and development builds only.
        /// In release builds call <see cref="Bootstrap"/> explicitly to opt in.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Application.isEditor || Debug.isDebugBuild)
                Bootstrap();
        }

        public static TerminalBehaviour Bootstrap()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[Achieve.CheatTerminal]");
            Instance = go.AddComponent<TerminalBehaviour>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Terminal = new Terminal();
            BuiltInCommands.RegisterAll(Terminal);

            View = gameObject.AddComponent<UGuiTerminalView>();
            Terminal.AttachView(View);

            var trigger = gameObject.AddComponent<TerminalCornerTrigger>();
            trigger.OnTriggered += OnTriggered;
            Trigger = trigger;

            CheatHud = gameObject.AddComponent<CheatHudView>();
            CheatHud.Bind(Terminal);
            CheatHud.OnConsoleRequested += OnConsoleRequested;

            // The corner handle is off by default; both gestures also work in reverse to hide.
            _triggerGesture = MultiFingerTapGesture.Attach(
                gameObject, TriggerGestureFingers, GestureTaps, TerminalShortcutKey.F9);
            _triggerGesture.Performed += OnTriggerGesture;

            _cheatHudGesture = MultiFingerTapGesture.Attach(
                gameObject, CheatHudGestureFingers, GestureTaps,
                TerminalShortcutKey.F10, TerminalShortcutKey.F2);
            _cheatHudGesture.Performed += OnCheatHudGesture;

            InstallDefaultModules();

            Terminal.Output.WriteLine("Achieve.CheatTerminal ready. Type 'help' for commands.", LogLevel.System);
            Terminal.Output.WriteLine(
                $"Tap {TriggerGestureFingers} fingers x{GestureTaps} for the corner handle, " +
                $"{CheatHudGestureFingers} fingers x{GestureTaps} for the cheat HUD.", LogLevel.System);
            Terminal.Output.WriteLine(
                "Keyboard: F1 or ` console, F10 cheat HUD, F9 corner handle.", LogLevel.System);
        }

        /// <summary>
        /// Desktop and editor entry point: touch gestures are useless with a mouse, so the
        /// console has its own keys. F1 toggles; the backquote only opens, so it never eats a
        /// keystroke while a command is being typed.
        /// </summary>
        private void Update()
        {
            if (Terminal == null) return;

            if (TerminalInput.WasKeyPressedThisFrame(_consoleKey))
                Terminal.Toggle();
            else if (!_consoleOpen && TerminalInput.WasKeyPressedThisFrame(_consoleOpenKey))
                Terminal.Open();
        }

        /// <summary>
        /// Views can be opened from a gesture, a button or code, so visibility is read back
        /// from them (two bool comparisons) instead of being tracked at every call site.
        /// </summary>
        private void LateUpdate()
        {
            bool consoleOpen = View != null && View.IsOpen;
            bool cheatHudOpen = CheatHud != null && CheatHud.IsOpen;

            if (consoleOpen != _consoleOpen)
            {
                _consoleOpen = consoleOpen;
                OnConsoleVisibilityChanged?.Invoke(consoleOpen);
            }

            if (cheatHudOpen != _cheatHudOpen)
            {
                _cheatHudOpen = cheatHudOpen;
                OnCheatHudVisibilityChanged?.Invoke(cheatHudOpen);
            }

            bool anyOpen = consoleOpen || cheatHudOpen;
            if (anyOpen == _anyOpen)
                return;

            _anyOpen = anyOpen;
            ApplyTimeScale(anyOpen);   // before the callbacks, so a handler can still override it
            OnVisibilityChanged?.Invoke(anyOpen);
        }

        private void ApplyTimeScale(bool anyOpen)
        {
            if (_pauseWhileOpen && anyOpen)
            {
                if (_timeScaleOverridden) return;
                _savedTimeScale = Time.timeScale;
                _timeScaleOverridden = true;
                Time.timeScale = Mathf.Max(0f, _openTimeScale);
                return;
            }

            if (!_timeScaleOverridden) return;
            _timeScaleOverridden = false;
            Time.timeScale = _savedTimeScale;
        }

        private void InstallDefaultModules()
        {
            InstallModule(new SceneToolsModule());
            InstallModule(new DataTableModule());
            InstallModule(new PerformanceModule());
            InstallModule(new RuntimeLogsModule());
            InstallModule(new UnityComponentsModule());
        }

        public void InstallModule(ITerminalModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            module.Install(Terminal);
            _modules.Add(module);
        }

        public T GetModule<T>() where T : class, ITerminalModule
        {
            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i] is T typed)
                    return typed;
            return null;
        }

        private void OnDestroy()
        {
            ApplyTimeScale(false);

            if (Trigger != null)
                Trigger.OnTriggered -= OnTriggered;

            if (CheatHud != null)
                CheatHud.OnConsoleRequested -= OnConsoleRequested;

            if (_triggerGesture != null)
                _triggerGesture.Performed -= OnTriggerGesture;

            if (_cheatHudGesture != null)
                _cheatHudGesture.Performed -= OnCheatHudGesture;

            for (int i = _modules.Count - 1; i >= 0; i--)
                if (_modules[i] is IDisposable disposable)
                    disposable.Dispose();
            _modules.Clear();

            if (Instance == this)
                Instance = null;
        }

        private void OnTriggered() => Terminal?.Toggle();

        private void OnConsoleRequested() => Terminal?.Open();

        /// <summary>Four-finger triple tap: show or hide the top-right handle.</summary>
        private void OnTriggerGesture()
        {
            if (Trigger != null)
                Trigger.Toggle();
        }

        /// <summary>Three-finger triple tap: slide the cheat HUD in or out.</summary>
        private void OnCheatHudGesture()
        {
            if (CheatHud != null)
                CheatHud.Toggle();
        }

        // ---- Static convenience API -----------------------------------------

        public static Terminal Current => Bootstrap().Terminal;
        public static void RegisterCommand(ICommand command) => Current.RegisterCommand(command);
        public static void RegisterCommand(string name, Action<CommandContext> action,
            string description = null, string category = null, string usage = null)
            => Current.RegisterCommand(name, action, description, category, usage);
        public static void RegisterCommand(string name, Action<CommandContext> action,
            string description, string category, string usage,
            Action<CommandCompletionContext, List<CompletionItem>> completion)
            => Current.RegisterCommand(name, action, description, category, usage, completion);
        public static void RegisterDataTable(string id, string name,
            Func<IEnumerable<DataTableRow>> rows, string description = null)
            => Current.RegisterDataTable(id, name, rows, description);
        public static void Execute(string input) => Current.Execute(input);
        public static void Open() => Current.Open();
        public static void Close() => Current.Close();
        public static void Toggle() => Current.Toggle();

        /// <summary>Corner handle visibility, normally driven by the four-finger triple tap.</summary>
        public static bool HandleVisible
        {
            get => Bootstrap().Trigger?.Visible ?? false;
            set
            {
                var trigger = Bootstrap().Trigger;
                if (trigger != null) trigger.Visible = value;
            }
        }

        /// <summary>Static shortcut for <see cref="OnConsoleVisibilityChanged"/>.</summary>
        public static event Action<bool> ConsoleVisibilityChanged
        {
            add => Bootstrap().OnConsoleVisibilityChanged += value;
            remove { if (Instance != null) Instance.OnConsoleVisibilityChanged -= value; }
        }

        /// <summary>Static shortcut for <see cref="OnCheatHudVisibilityChanged"/>.</summary>
        public static event Action<bool> CheatHudVisibilityChanged
        {
            add => Bootstrap().OnCheatHudVisibilityChanged += value;
            remove { if (Instance != null) Instance.OnCheatHudVisibilityChanged -= value; }
        }

        /// <summary>
        /// Static shortcut for <see cref="OnVisibilityChanged"/>: true when the console or the
        /// cheat HUD opens, false once both are closed.
        /// </summary>
        public static event Action<bool> VisibilityChanged
        {
            add => Bootstrap().OnVisibilityChanged += value;
            remove { if (Instance != null) Instance.OnVisibilityChanged -= value; }
        }

        /// <summary>True while the console or the cheat HUD is on screen.</summary>
        public static bool IsAnyOpen => Instance != null && Instance.AnyOpen;

        /// <summary>Force <see cref="Time.timeScale"/> to <see cref="OpenTimeScale"/> while open.</summary>
        public static bool PauseGameWhileOpen
        {
            get => Bootstrap().PauseWhileOpen;
            set => Bootstrap().PauseWhileOpen = value;
        }

        /// <summary>Time scale used while open when <see cref="PauseGameWhileOpen"/> is on (default 0).</summary>
        public static float PausedTimeScale
        {
            get => Bootstrap().OpenTimeScale;
            set => Bootstrap().OpenTimeScale = value;
        }

        /// <summary>Cheat HUD layout: true (default) covers the screen, false slides in from the left.</summary>
        public static bool CheatHudFullScreen
        {
            get => Bootstrap().CheatHud?.FullScreen ?? true;
            set
            {
                var hud = Bootstrap().CheatHud;
                if (hud != null) hud.FullScreen = value;
            }
        }

        public static void OpenCheatHud() => Bootstrap().CheatHud?.Open();
        public static void CloseCheatHud() => Bootstrap().CheatHud?.Close();
        public static void ToggleCheatHud() => Bootstrap().CheatHud?.Toggle();
        public static void ToggleHandle() => HandleVisible = !HandleVisible;
    }
}
