using System;
using System.Collections.Generic;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>Delegate-backed completion provider for explicitly registered commands.</summary>
    public sealed class CommandCompletionProvider : ICommandCompletionProvider
    {
        private readonly Action<CommandCompletionContext, List<CompletionItem>> _complete;

        public CommandCompletionProvider(Action<CommandCompletionContext, List<CompletionItem>> complete)
        {
            _complete = complete ?? throw new ArgumentNullException(nameof(complete));
        }

        public void Complete(CommandCompletionContext context, List<CompletionItem> results)
            => _complete(context, results);
    }
}
