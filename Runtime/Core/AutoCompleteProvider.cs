using System;
using System.Collections.Generic;

namespace UniTerminal.Core
{
    /// <summary>Prefix-matches the first token against registered command names.</summary>
    public sealed class AutoCompleteProvider : IAutoCompleteProvider
    {
        private readonly ICommandRegistry _registry;
        private readonly List<string> _buffer = new List<string>();

        public AutoCompleteProvider(ICommandRegistry registry)
        {
            _registry = registry;
        }

        public IReadOnlyList<string> Complete(string input)
        {
            _buffer.Clear();
            if (string.IsNullOrWhiteSpace(input))
                return _buffer;

            // Only complete the command name (no space typed yet).
            if (input.IndexOf(' ') >= 0)
                return _buffer;

            foreach (var command in _registry.All)
            {
                if (command.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    _buffer.Add(command.Name);
            }

            _buffer.Sort(StringComparer.OrdinalIgnoreCase);
            return _buffer;
        }
    }
}
