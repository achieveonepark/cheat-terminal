using System;
using System.Collections.Generic;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>Stores and resolves registered commands by name (case-insensitive).</summary>
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, ICommand> _commands =
            new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        public event Action Changed;

        public IReadOnlyCollection<ICommand> All => _commands.Values;

        public void Register(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Command name cannot be empty.", nameof(command));
            _commands[command.Name] = command;
            Changed?.Invoke();
        }

        public bool Unregister(string name)
        {
            if (string.IsNullOrEmpty(name) || !_commands.Remove(name))
                return false;

            Changed?.Invoke();
            return true;
        }

        public bool TryGet(string name, out ICommand command)
        {
            if (!string.IsNullOrEmpty(name))
                return _commands.TryGetValue(name, out command);
            command = null;
            return false;
        }

        public bool Contains(string name)
            => !string.IsNullOrEmpty(name) && _commands.ContainsKey(name);
    }
}
