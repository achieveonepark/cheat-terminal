using System.Collections.Generic;

namespace UniTerminal
{
    /// <summary>Stores and resolves registered commands by name (case-insensitive).</summary>
    public interface ICommandRegistry
    {
        void Register(ICommand command);
        bool Unregister(string name);
        bool TryGet(string name, out ICommand command);
        bool Contains(string name);
        IReadOnlyCollection<ICommand> All { get; }
    }
}
