using System.Collections.Generic;

namespace Achieve.CheatTerminal
{
    /// <summary>Optional extension for commands that can suggest argument values.</summary>
    public interface ICommandCompletionProvider
    {
        void Complete(CommandCompletionContext context, List<CompletionItem> results);
    }
}
