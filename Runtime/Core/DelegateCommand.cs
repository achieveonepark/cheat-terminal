using System;

namespace UniTerminal.Core
{
    /// <summary>A command backed by a delegate. Handy for built-ins and ad-hoc commands.</summary>
    public sealed class DelegateCommand : ICommand
    {
        private readonly Action<CommandContext> _action;

        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public string Usage { get; }

        public DelegateCommand(string name, Action<CommandContext> action,
            string description = null, string category = null, string usage = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            Description = description ?? string.Empty;
            Category = string.IsNullOrEmpty(category) ? "General" : category;
            Usage = usage ?? name;
        }

        public void Execute(CommandContext context) => _action(context);
    }
}
