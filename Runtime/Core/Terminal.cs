using System;
using System.Reflection;

namespace UniTerminal.Core
{
    /// <summary>
    /// The terminal core. All collaborators are injected, so any part can be replaced.
    /// Use <see cref="TerminalBuilder"/> for a configured instance with sensible defaults.
    /// </summary>
    public sealed class Terminal : ITerminal
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly ICommandParser _parser;

        public ICommandRegistry Registry { get; }
        public ICommandOutput Output { get; }
        public ICommandHistory History { get; }
        public IAliasResolver Alias { get; }
        public IAutoCompleteProvider AutoComplete { get; }
        public IArgumentConverter Converter { get; }
        public ITerminalView View { get; private set; }

        public Terminal(
            ICommandRegistry registry,
            ICommandParser parser,
            ICommandHistory history,
            IAliasResolver alias,
            IAutoCompleteProvider autoComplete,
            IArgumentConverter converter,
            ICommandOutput output)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            History = history ?? throw new ArgumentNullException(nameof(history));
            Alias = alias ?? throw new ArgumentNullException(nameof(alias));
            AutoComplete = autoComplete ?? throw new ArgumentNullException(nameof(autoComplete));
            Converter = converter ?? throw new ArgumentNullException(nameof(converter));
            Output = output ?? throw new ArgumentNullException(nameof(output));
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
            var parsed = _parser.Parse(expanded);
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

        public void Register(object target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            ScanType(target.GetType(), target, InstanceFlags);
        }

        public void RegisterStatic(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            ScanType(type, null, StaticFlags);
        }

        public void RegisterCommand(ICommand command) => Registry.Register(command);

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

        private void OnViewSubmit(string input) => Execute(input);

        private void OnViewInputChanged(string input)
        {
            var suggestions = AutoComplete.Complete(input);
            View?.ShowSuggestions(suggestions);
        }
    }
}
