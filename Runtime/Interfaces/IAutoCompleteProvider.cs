using System.Collections.Generic;

namespace UniTerminal
{
    /// <summary>Produces completion candidates for the current input.</summary>
    public interface IAutoCompleteProvider
    {
        IReadOnlyList<string> Complete(string input);
    }
}
