using System.Collections.Generic;

namespace UniTerminal
{
    /// <summary>Expands user-defined aliases inside an input string.</summary>
    public interface IAliasResolver
    {
        void Set(string alias, string expansion);
        bool Remove(string alias);
        string Resolve(string input);
        IReadOnlyDictionary<string, string> All { get; }
    }
}
