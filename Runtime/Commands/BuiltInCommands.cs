using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniTerminal.Core
{
    /// <summary>Registers the always-available commands: help, clear, history, alias, echo.</summary>
    public static class BuiltInCommands
    {
        public static void RegisterAll(ITerminal terminal)
        {
            terminal.RegisterCommand(new DelegateCommand(
                "help", ctx => Help(ctx),
                "List commands or show help for one", "System", "help [command]"));

            terminal.RegisterCommand(new DelegateCommand(
                "clear", ctx => ctx.Output.Clear(),
                "Clear the terminal output", "System", "clear"));

            terminal.RegisterCommand(new DelegateCommand(
                "history", ctx => History(ctx),
                "Show command history", "System", "history"));

            terminal.RegisterCommand(new DelegateCommand(
                "echo", ctx => ctx.Output.WriteLine(string.Join(" ", ctx.Args)),
                "Print text back", "System", "echo <text...>"));

            terminal.RegisterCommand(new DelegateCommand(
                "alias", ctx => Alias(ctx),
                "Manage aliases: alias <name> <expansion> | alias remove <name> | alias",
                "System", "alias [name] [expansion]"));
        }

        private static void Help(CommandContext ctx)
        {
            var registry = ctx.Terminal.Registry;

            if (ctx.Has(0))
            {
                if (registry.TryGet(ctx.Args[0], out var cmd))
                {
                    ctx.Output.WriteLine($"{cmd.Name} - {cmd.Description}", LogLevel.System);
                    ctx.Output.WriteLine($"  usage: {cmd.Usage}");
                    ctx.Output.WriteLine($"  category: {cmd.Category}");
                }
                else
                {
                    ctx.Output.WriteLine($"Unknown command: {ctx.Args[0]}", LogLevel.Error);
                }
                return;
            }

            var byCategory = registry.All
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Name)
                .GroupBy(c => c.Category);

            var sb = new StringBuilder();
            foreach (var group in byCategory)
            {
                sb.AppendLine($"[{group.Key}]");
                foreach (var cmd in group)
                {
                    sb.Append("  ").Append(cmd.Name);
                    if (!string.IsNullOrEmpty(cmd.Description))
                        sb.Append(" - ").Append(cmd.Description);
                    sb.AppendLine();
                }
            }
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void History(CommandContext ctx)
        {
            IReadOnlyList<string> entries = ctx.Terminal.History.Entries;
            if (entries.Count == 0)
            {
                ctx.Output.WriteLine("(history empty)");
                return;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
                sb.AppendLine($"{i + 1,3}  {entries[i]}");
            ctx.Output.WriteLine(sb.ToString().TrimEnd());
        }

        private static void Alias(CommandContext ctx)
        {
            var alias = ctx.Terminal.Alias;

            if (!ctx.Has(0))
            {
                if (alias.All.Count == 0)
                {
                    ctx.Output.WriteLine("(no aliases)");
                    return;
                }
                var sb = new StringBuilder();
                foreach (var kv in alias.All)
                    sb.AppendLine($"{kv.Key} = {kv.Value}");
                ctx.Output.WriteLine(sb.ToString().TrimEnd());
                return;
            }

            if (ctx.Args[0].Equals("remove", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!ctx.Has(1)) { ctx.Output.WriteLine("usage: alias remove <name>", LogLevel.Error); return; }
                bool removed = alias.Remove(ctx.Args[1]);
                ctx.Output.WriteLine(removed ? $"Removed alias '{ctx.Args[1]}'" : $"No alias '{ctx.Args[1]}'",
                    removed ? LogLevel.Success : LogLevel.Warning);
                return;
            }

            if (!ctx.Has(1))
            {
                ctx.Output.WriteLine("usage: alias <name> <expansion>", LogLevel.Error);
                return;
            }

            string name = ctx.Args[0];
            string expansion = string.Join(" ", ctx.Args.Skip(1));
            alias.Set(name, expansion);
            ctx.Output.WriteLine($"alias {name} = {expansion}", LogLevel.Success);
        }
    }
}
