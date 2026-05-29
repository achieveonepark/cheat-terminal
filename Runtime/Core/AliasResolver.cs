using System;
using System.Collections.Generic;

namespace UniTerminal.Core
{
    /// <summary>
    /// Expands the first token of an input line if it matches a defined alias.
    /// e.g. alias "rich" -> "gold 999999" turns "rich" into "gold 999999".
    /// </summary>
    public sealed class AliasResolver : IAliasResolver
    {
        private readonly Dictionary<string, string> _aliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> All => _aliases;

        public void Set(string alias, string expansion)
        {
            if (string.IsNullOrWhiteSpace(alias)) return;
            _aliases[alias] = expansion ?? string.Empty;
        }

        public bool Remove(string alias)
            => !string.IsNullOrEmpty(alias) && _aliases.Remove(alias);

        public string Resolve(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            int space = input.IndexOf(' ');
            string head = space < 0 ? input : input.Substring(0, space);
            string tail = space < 0 ? string.Empty : input.Substring(space);

            if (_aliases.TryGetValue(head, out var expansion))
                return expansion + tail;

            return input;
        }
    }
}
