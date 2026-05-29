using System.Collections.Generic;

namespace UniTerminal
{
    /// <summary>Stores previously executed input and supports up/down navigation.</summary>
    public interface ICommandHistory
    {
        void Add(string command);
        string Previous();
        string Next();
        void ResetCursor();
        void Clear();
        IReadOnlyList<string> Entries { get; }
    }
}
