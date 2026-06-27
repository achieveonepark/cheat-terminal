using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Achieve.CheatTerminal.Core;

namespace Achieve.CheatTerminal.Modules
{
    /// <summary>Lists registered game data tables and their rows.</summary>
    public sealed class DataTableModule : ITerminalModule
    {
        private const int MaxRows = 80;

        public string Name => "Data";

        public void Install(Terminal terminal)
        {
            terminal.RegisterCommand(
                "data",
                Run,
                "Browse registered data tables",
                "Data",
                "data [table] [id|text]",
                Complete);
        }

        private static void Run(CommandContext ctx)
        {
            if (!ctx.Has(0))
            {
                ListTables(ctx);
                return;
            }

            if (!ctx.Terminal.DataTables.TryGet(ctx.Args[0], out var table))
            {
                ctx.Output.WriteLine($"Unknown data table: {ctx.Args[0]}", LogLevel.Error);
                return;
            }

            var rows = table.GetRows();
            if (!ctx.Has(1))
            {
                ListRows(ctx, table, rows);
                return;
            }

            ShowRows(ctx, table, rows, ctx.Args[1]);
        }

        private static void ListTables(CommandContext ctx)
        {
            var tables = ctx.Terminal.DataTables.All.OrderBy(t => t.Id).ToList();
            if (tables.Count == 0)
            {
                ctx.Output.WriteLine("No data tables registered.", LogLevel.Warning);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Data tables:");
            foreach (var table in tables)
            {
                int count = table.GetRows().Count;
                sb.Append("  ").Append(table.Id)
                  .Append("  (").Append(count).Append(" row").Append(count == 1 ? "" : "s").Append(")");
                if (!string.IsNullOrEmpty(table.Name) && !table.Name.Equals(table.Id, StringComparison.OrdinalIgnoreCase))
                    sb.Append("  ").Append(table.Name);
                if (!string.IsNullOrEmpty(table.Description))
                    sb.Append(" - ").Append(table.Description);
                sb.AppendLine();
            }
            sb.Append("Use: data <table> [id|text]");
            ctx.Output.WriteLine(sb.ToString(), LogLevel.System);
        }

        private static void ListRows(CommandContext ctx, TerminalDataTable table,
            IReadOnlyList<DataTableRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{table.Id} - {table.Name} ({rows.Count} row{(rows.Count == 1 ? "" : "s")})");

            int shown = 0;
            foreach (var row in rows.OrderBy(r => r.Id))
            {
                if (shown++ >= MaxRows)
                {
                    sb.AppendLine("  ... (truncated)");
                    break;
                }

                AppendRowSummary(sb, row);
            }

            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void ShowRows(CommandContext ctx, TerminalDataTable table,
            IReadOnlyList<DataTableRow> rows, string query)
        {
            var matches = rows
                .Where(r => Matches(r, query))
                .OrderBy(r => r.Id)
                .Take(MaxRows)
                .ToList();

            if (matches.Count == 0)
            {
                ctx.Output.WriteLine($"No rows in '{table.Id}' matching '{query}'.", LogLevel.Warning);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{table.Id} matches for '{query}':");
            foreach (var row in matches)
            {
                AppendRowSummary(sb, row);
                foreach (var field in row.Fields)
                    sb.AppendLine($"      {field.Key}: {field.Value}");
            }

            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void Complete(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex == 0)
            {
                foreach (var table in ctx.Terminal.DataTables.All)
                {
                    if (!StartsWith(table.Id, ctx.CurrentToken)) continue;
                    results.Add(new CompletionItem(
                        table.Id,
                        table.Id,
                        $"{table.Name} ({table.GetRows().Count} rows)",
                        "Data",
                        CompletionKind.DataTable));
                }
                return;
            }

            if (ctx.ArgumentIndex == 1 &&
                ctx.Terminal.DataTables.TryGet(FirstArg(ctx.Input), out var selected))
            {
                foreach (var row in selected.GetRows())
                {
                    if (!StartsWith(row.Id, ctx.CurrentToken) &&
                        !StartsWith(row.Name, ctx.CurrentToken))
                        continue;

                    results.Add(new CompletionItem(
                        row.Id,
                        string.IsNullOrEmpty(row.Name) ? row.Id : $"{row.Id}  {row.Name}",
                        row.Summary,
                        selected.Id,
                        CompletionKind.DataRow));
                }
            }
        }

        private static string FirstArg(string input)
        {
            var parts = (input ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }

        private static bool StartsWith(string value, string prefix)
            => string.IsNullOrEmpty(prefix) ||
               (!string.IsNullOrEmpty(value) &&
                value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        private static bool Matches(DataTableRow row, string query)
            => row.Id.Equals(query, StringComparison.OrdinalIgnoreCase) ||
               (!string.IsNullOrEmpty(row.Name) &&
                row.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
               (!string.IsNullOrEmpty(row.Summary) &&
                row.Summary.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

        private static void AppendRowSummary(StringBuilder sb, DataTableRow row)
        {
            sb.Append("  ").Append(row.Id);
            if (!string.IsNullOrEmpty(row.Name))
                sb.Append("  ").Append(row.Name);
            if (!string.IsNullOrEmpty(row.Summary))
                sb.Append(" - ").Append(row.Summary);
            sb.AppendLine();
        }
    }
}
