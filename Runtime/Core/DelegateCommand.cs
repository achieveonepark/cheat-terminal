using System;
using System.Collections.Generic;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>A command backed by a delegate. Handy for built-ins and ad-hoc commands.</summary>
    public sealed class DelegateCommand : ICommand, ICommandCompletionProvider
    {
        private readonly Action<CommandContext> _action;
        private readonly ICommandCompletionProvider _completion;

        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public string Usage { get; }

        public DelegateCommand(string name, Action<CommandContext> action,
            string description = null, string category = null, string usage = null,
            ICommandCompletionProvider completion = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _completion = completion;
            Description = description ?? string.Empty;
            Category = string.IsNullOrEmpty(category) ? "General" : category;
            Usage = usage ?? name;
        }

        public void Execute(CommandContext context) => _action(context);

        public void Complete(CommandCompletionContext context, List<CompletionItem> results)
            => _completion?.Complete(context, results);
    }
}
