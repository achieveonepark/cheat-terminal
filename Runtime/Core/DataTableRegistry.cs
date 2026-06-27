using System;
using System.Collections.Generic;
using System.Linq;

namespace Achieve.CheatTerminal.Core
{
    public sealed class TerminalDataTable
    {
        private readonly Func<IEnumerable<DataTableRow>> _rows;

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }

        public TerminalDataTable(string id, string name, Func<IEnumerable<DataTableRow>> rows,
            string description = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Table id cannot be empty.", nameof(id));

            Id = id;
            Name = string.IsNullOrEmpty(name) ? id : name;
            Description = description ?? string.Empty;
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
        }

        public IReadOnlyList<DataTableRow> GetRows()
            => _rows()?.Where(r => r != null).ToList() ?? EmptyRows;

        private static readonly IReadOnlyList<DataTableRow> EmptyRows = new List<DataTableRow>();
    }

    /// <summary>Registry for game data tables exposed to the terminal.</summary>
    public sealed class DataTableRegistry
    {
        private readonly Dictionary<string, TerminalDataTable> _tables =
            new Dictionary<string, TerminalDataTable>(StringComparer.OrdinalIgnoreCase);

        public event Action Changed;

        public IReadOnlyCollection<TerminalDataTable> All => _tables.Values;

        public void Register(TerminalDataTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            _tables[table.Id] = table;
            Changed?.Invoke();
        }

        public void Register(string id, string name, Func<IEnumerable<DataTableRow>> rows,
            string description = null)
            => Register(new TerminalDataTable(id, name, rows, description));

        public bool TryGet(string id, out TerminalDataTable table)
        {
            if (!string.IsNullOrEmpty(id))
                return _tables.TryGetValue(id, out table);
            table = null;
            return false;
        }
    }
}
