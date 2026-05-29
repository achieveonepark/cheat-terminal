using System;
using System.Collections.Generic;
using UniTerminal.Core;
using UniTerminal.Modules;
using UniTerminal.UI;
using UnityEngine;

namespace UniTerminal
{
    /// <summary>
    /// Runtime entry point. Creates the concrete <see cref="UniTerminal.Terminal"/> core,
    /// attaches the uGUI view and the top-right corner trigger, sweeps the project for
    /// static [Terminal] commands, and survives scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TerminalBehaviour : MonoBehaviour
    {
        /// <summary>
        /// When true (default), the bootstrap scans user assemblies for static [Terminal]
        /// methods on startup. Set to false before bootstrapping to skip the scan.
        /// </summary>
        public static bool AutoScanStaticCommands = true;

        public static TerminalBehaviour Instance { get; private set; }

        public Terminal Terminal { get; private set; }
        public ITerminalView View { get; private set; }
        public TerminalCornerTrigger Trigger { get; private set; }

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
            var go = new GameObject("[UniTerminal]");
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

            InstallDefaultModules();

            if (AutoScanStaticCommands)
            {
                int count = Terminal.ScanStaticCommands();
                if (count > 0)
                    Terminal.Output.WriteLine($"Discovered {count} static command(s).", LogLevel.System);
            }

            Terminal.Output.WriteLine("UniTerminal ready. Type 'help' for commands.", LogLevel.System);
        }

        private void InstallDefaultModules()
        {
            InstallModule(new SceneToolsModule());
            InstallModule(new ObjectInspectorModule());
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
            if (Instance == this)
                Instance = null;
        }

        private void OnTriggered() => Terminal?.Toggle();

        // ---- Static convenience API -----------------------------------------

        public static Terminal Current => Bootstrap().Terminal;
        public static void Register(object target) => Current.Register(target);
        public static void RegisterStatic(Type type) => Current.RegisterStatic(type);
        public static void RegisterCommand(ICommand command) => Current.RegisterCommand(command);
        public static void Execute(string input) => Current.Execute(input);
        public static void Open() => Current.Open();
        public static void Close() => Current.Close();
        public static void Toggle() => Current.Toggle();
    }
}
