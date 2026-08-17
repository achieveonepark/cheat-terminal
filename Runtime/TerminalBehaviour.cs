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

        private MultiFingerTapGesture _triggerGesture;
        private MultiFingerTapGesture _cheatHudGesture;

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
                gameObject, CheatHudGestureFingers, GestureTaps, TerminalShortcutKey.F10);
            _cheatHudGesture.Performed += OnCheatHudGesture;

            InstallDefaultModules();

            Terminal.Output.WriteLine("Achieve.CheatTerminal ready. Type 'help' for commands.", LogLevel.System);
            Terminal.Output.WriteLine(
                $"Tap {TriggerGestureFingers} fingers x{GestureTaps} for the corner handle, " +
                $"{CheatHudGestureFingers} fingers x{GestureTaps} for the cheat HUD.", LogLevel.System);
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

        public static void OpenCheatHud() => Bootstrap().CheatHud?.Open();
        public static void CloseCheatHud() => Bootstrap().CheatHud?.Close();
        public static void ToggleCheatHud() => Bootstrap().CheatHud?.Toggle();
        public static void ToggleHandle() => HandleVisible = !HandleVisible;
    }
}
