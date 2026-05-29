using System;
using System.Collections.Generic;
using UniTerminal.Core;
using UniTerminal.Modules;
using UniTerminal.UI;
using UnityEngine;

namespace UniTerminal
{
    /// <summary>
    /// Runtime entry point. Builds the terminal core with default implementations,
    /// attaches the uGUI view and the top-right corner trigger, and survives scene loads.
    /// Replace any collaborator by editing the <see cref="TerminalBuilder"/> usage here,
    /// or build your own and assign <see cref="Terminal"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TerminalBehaviour : MonoBehaviour
    {
        public static TerminalBehaviour Instance { get; private set; }

        public ITerminal Terminal { get; private set; }
        public ITerminalView View { get; private set; }
        public ITerminalTrigger Trigger { get; private set; }

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

            Terminal = new TerminalBuilder().Build();
            BuiltInCommands.RegisterAll(Terminal);

            View = gameObject.AddComponent<UGuiTerminalView>();
            Terminal.AttachView(View);

            var trigger = gameObject.AddComponent<TerminalCornerTrigger>();
            trigger.OnTriggered += OnTriggered;
            Trigger = trigger;

            InstallDefaultModules();

            Terminal.Output.WriteLine("UniTerminal ready. Type 'help' for commands.", LogLevel.System);
        }

        private void InstallDefaultModules()
        {
            InstallModule(new SceneToolsModule());
            InstallModule(new ObjectInspectorModule());
            InstallModule(new PerformanceModule());
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

        public static ITerminal Current => Bootstrap().Terminal;
        public static void Register(object target) => Current.Register(target);
        public static void RegisterStatic(Type type) => Current.RegisterStatic(type);
        public static void RegisterCommand(ICommand command) => Current.RegisterCommand(command);
        public static void Execute(string input) => Current.Execute(input);
        public static void Open() => Current.Open();
        public static void Close() => Current.Close();
        public static void Toggle() => Current.Toggle();
    }
}
