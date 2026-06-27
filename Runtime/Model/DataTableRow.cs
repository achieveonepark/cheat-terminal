using System.Collections.Generic;

namespace Achieve.CheatTerminal
{
    /// <summary>A lightweight, reflection-free row description exposed to the terminal.</summary>
    public sealed class DataTableRow
    {
        public string Id { get; }
        public string Name { get; }
        public string Summary { get; }
        public IReadOnlyDictionary<string, string> Fields { get; }

        public DataTableRow(string id, string name = null, string summary = null,
            IReadOnlyDictionary<string, string> fields = null)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Summary = summary ?? string.Empty;
            Fields = fields ?? EmptyFields;
        }

        private static readonly IReadOnlyDictionary<string, string> EmptyFields =
            new Dictionary<string, string>();
    }
}
