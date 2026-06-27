using System;
using System.Collections.Generic;
using Achieve.CheatTerminal.Core;

namespace Achieve.CheatTerminal
{
    /// <summary>
    /// The terminal core. Commands are registered explicitly so runtime behavior stays
    /// reflection-free and predictable under IL2CPP/AOT builds.
    /// </summary>
    public sealed class Terminal
    {
        public CommandRegistry Registry { get; }
        public CommandParser Parser { get; }
        public CommandHistory History { get; }
        public AliasResolver Alias { get; }
        public ArgumentConverter Converter { get; }
        public AutoCompleteProvider AutoComplete { get; }
        public DataTableRegistry DataTables { get; }
        public ICommandOutput Output { get; }
        public ITerminalView View { get; private set; }

        public Terminal(ICommandOutput output = null)
        {
            Registry = new CommandRegistry();
            Parser = new CommandParser();
            History = new CommandHistory();
            Alias = new AliasResolver();
            Converter = new ArgumentConverter();
            DataTables = new DataTableRegistry();
            AutoComplete = new AutoCompleteProvider(this);
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
            catch (Exception e)
            {
                Output.WriteLine($"Error: {e.Message}", LogLevel.Error);
            }
        }

        public void RegisterCommand(ICommand command) => Registry.Register(command);

        public void RegisterCommand(string name, Action<CommandContext> action,
            string description = null, string category = null, string usage = null)
            => RegisterCommand(new DelegateCommand(name, action, description, category, usage));

        public void RegisterCommand(string name, Action<CommandContext> action,
            string description, string category, string usage,
            Action<CommandCompletionContext, List<CompletionItem>> completion)
            => RegisterCommand(new DelegateCommand(name, action, description, category, usage,
                completion == null ? null : new CommandCompletionProvider(completion)));

        public void RegisterDataTable(string id, string name,
            Func<IEnumerable<DataTableRow>> rows, string description = null)
            => DataTables.Register(id, name, rows, description);

        public void Open() => View?.Open();
        public void Close() => View?.Close();
        public void Toggle() => View?.Toggle();

        private void OnViewSubmit(string input) => Execute(input);

        private void OnViewInputChanged(string input)
        {
            var suggestions = AutoComplete.Complete(input);
            View?.ShowSuggestions(suggestions);
        }
    }
}
