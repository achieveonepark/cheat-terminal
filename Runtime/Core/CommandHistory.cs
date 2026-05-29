using System.Collections.Generic;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>Stores executed input and supports up/down navigation.</summary>
    public sealed class CommandHistory
    {
        private readonly List<string> _entries = new List<string>();
        private readonly int _capacity;
        private int _cursor;

        public IReadOnlyList<string> Entries => _entries;

        public CommandHistory(int capacity = 100)
        {
            _capacity = capacity < 1 ? 1 : capacity;
            _cursor = 0;
        }

        public void Add(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            // Skip consecutive duplicates.
            if (_entries.Count > 0 && _entries[_entries.Count - 1] == command)
            {
                _cursor = _entries.Count;
                return;
            }

            _entries.Add(command);
            if (_entries.Count > _capacity)
                _entries.RemoveAt(0);

            _cursor = _entries.Count;
        }

        public string Previous()
        {
            if (_entries.Count == 0) return null;
            _cursor--;
            if (_cursor < 0) _cursor = 0;
            return _entries[_cursor];
        }

        public string Next()
        {
            if (_entries.Count == 0) return null;
            _cursor++;
            if (_cursor >= _entries.Count)
            {
                _cursor = _entries.Count;
                return string.Empty;
            }
            return _entries[_cursor];
        }

        public void ResetCursor() => _cursor = _entries.Count;

        public void Clear()
        {
            _entries.Clear();
            _cursor = 0;
        }
    }
}
