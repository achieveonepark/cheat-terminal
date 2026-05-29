using System;
using System.Reflection;
using UniTerminal.Core;

namespace UniTerminal
{
    /// <summary>
    /// The terminal core. Concrete by design: it owns its collaborators directly.
    /// Commands are discovered from [Terminal]-annotated methods - register an instance
    /// with <see cref="Register"/>, a type's statics with <see cref="RegisterStatic"/>,
    /// or sweep all user assemblies for static commands with <see cref="ScanStaticCommands"/>.
    /// </summary>
    public sealed class Terminal
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        // Assemblies we never bother scanning for static commands.
        private static readonly string[] SkippedAssemblyPrefixes =
        {
            "Unity", "System", "mscorlib", "netstandard", "Mono.", "Microsoft",
            "nunit", "JetBrains", "Bee.", "ExCSS", "ReportGenerator", "Newtonsoft",
            "log4net", "NiceIO", "PlayerBuildProgram"
        };

        public CommandRegistry Registry { get; }
        public CommandParser Parser { get; }
        public CommandHistory History { get; }
        public AliasResolver Alias { get; }
        public ArgumentConverter Converter { get; }
        public AutoCompleteProvider AutoComplete { get; }
        public ICommandOutput Output { get; }
        public ITerminalView View { get; private set; }

        public Terminal(ICommandOutput output = null)
        {
            Registry = new CommandRegistry();
            Parser = new CommandParser();
            History = new CommandHistory();
            Alias = new AliasResolver();
            Converter = new ArgumentConverter();
            AutoComplete = new AutoCompleteProvider(Registry);
            Output = output ?? new BufferedOutput();
        }

        public void AttachView(ITerminalView view)
        {
            if (View != null)
            {
                View.OnSubmit -= OnViewSubmit;
                View.OnInputChanged -= OnViewInputChanged;
            }

            View = view;
            if (view == null) return;

            view.Bind(this);
            view.OnSubmit += OnViewSubmit;
            view.OnInputChanged += OnViewInputChanged;

            if (Output is BufferedOutput buffered)
                buffered.AttachView(view);
        }

        public void Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            History.Add(input);

            var expanded = Alias.Resolve(input);
            var parsed = Parser.Parse(expanded);
            if (parsed.IsEmpty)
                return;

            if (!Registry.TryGet(parsed.Name, out var command))
            {
                Output.WriteLine($"Unknown command: {parsed.Name}", LogLevel.Error);
                return;
            }

            var context = new CommandContext(input, parsed.Name, parsed.Args, Output, this);
            try
            {
                command.Execute(context);
            }
            catch (TargetInvocationException tie)
            {
                Output.WriteLine($"Error: {tie.InnerException?.Message ?? tie.Message}", LogLevel.Error);
            }
            catch (Exception e)
            {
                Output.WriteLine($"Error: {e.Message}", LogLevel.Error);
            }
        }

        public void RegisterCommand(ICommand command) => Registry.Register(command);

        /// <summary>Scan a live instance for [Terminal] methods (instance + static) and register them.</summary>
        public void Register(object target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            ScanType(target.GetType(), target, InstanceFlags);
        }

        /// <summary>Scan a type for static [Terminal] methods and register them.</summary>
        public void RegisterStatic(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            ScanType(type, null, StaticFlags);
        }

        /// <summary>
        /// Sweep all user assemblies for static [Terminal] methods and register them.
        /// Engine/system assemblies are skipped to keep this cheap. Returns the count found.
        /// Instance methods are intentionally ignored here - they need a live object, so
        /// register those via <see cref="Register"/>.
        /// </summary>
        public int ScanStaticCommands()
        {
            int registered = 0;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int a = 0; a < assemblies.Length; a++)
            {
                var asm = assemblies[a];
                if (IsSkipped(asm)) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch { continue; }

                for (int t = 0; t < types.Length; t++)
                {
                    var type = types[t];
                    if (type == null || type.IsGenericTypeDefinition) continue;

                    MethodInfo[] methods;
                    try { methods = type.GetMethods(StaticFlags); }
                    catch { continue; }

                    for (int m = 0; m < methods.Length; m++)
                    {
                        var attr = methods[m].GetCustomAttribute<TerminalAttribute>(inherit: false);
                        if (attr == null) continue;
                        Registry.Register(new AttributeCommand(attr, methods[m], null, Converter));
                        registered++;
                    }
                }
            }
            return registered;
        }

        public void Open() => View?.Open();
        public void Close() => View?.Close();
        public void Toggle() => View?.Toggle();

        private void ScanType(Type type, object target, BindingFlags flags)
        {
            var methods = type.GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
            {
                var attribute = methods[i].GetCustomAttribute<TerminalAttribute>(inherit: true);
                if (attribute == null) continue;
                Registry.Register(new AttributeCommand(attribute, methods[i], target, Converter));
            }
        }

        private static bool IsSkipped(Assembly asm)
        {
            string name = asm.GetName().Name;
            for (int i = 0; i < SkippedAssemblyPrefixes.Length; i++)
                if (name.StartsWith(SkippedAssemblyPrefixes[i], StringComparison.Ordinal))
                    return true;
            return false;
        }

        private void OnViewSubmit(string input) => Execute(input);

        private void OnViewInputChanged(string input)
        {
            var suggestions = AutoComplete.Complete(input);
            View?.ShowSuggestions(suggestions);
        }
    }
}
