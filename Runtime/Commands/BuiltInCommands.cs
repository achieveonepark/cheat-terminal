using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>Registers the always-available commands: help, clear, history, alias, echo.</summary>
    public static class BuiltInCommands
    {
        public static void RegisterAll(Terminal terminal)
        {
            terminal.RegisterCommand(new DelegateCommand(
                "help", ctx => Help(ctx),
                "List commands or show help for one", "System", "help [command|category]",
                new CommandCompletionProvider(CompleteHelp)));

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
                "System", "alias [remove|name] [expansion]",
                new CommandCompletionProvider(CompleteAlias)));
        }

        private static void Help(CommandContext ctx)
        {
            var registry = ctx.Terminal.Registry;

            if (ctx.Has(0))
            {
                string arg = ctx.Args[0];

                // 1) Exact command name -> show command detail.
                if (registry.TryGet(arg, out var cmd))
                {
                    ctx.Output.WriteLine($"{cmd.Name} - {cmd.Description}", LogLevel.System);
                    ctx.Output.WriteLine($"  usage: {cmd.Usage}");
                    ctx.Output.WriteLine($"  category: {cmd.Category}");
                    return;
                }

                // 2) Category name -> list its commands with descriptions.
                var inCategory = registry.All
                    .Where(c => c.Category.Equals(arg, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.Name)
                    .ToList();

                if (inCategory.Count > 0)
                {
                    var catSb = new StringBuilder();
                    catSb.AppendLine($"[{inCategory[0].Category}]");
                    foreach (var c in inCategory)
                        AppendCommandLine(catSb, c);
                    ctx.Output.WriteLine(catSb.ToString().TrimEnd(), LogLevel.System);
                    return;
                }

                ctx.Output.WriteLine($"Unknown command or category: {arg}", LogLevel.Error);
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
                    AppendCommandLine(sb, cmd);
            }
            sb.AppendLine();
            sb.Append("Type 'help <command>' or 'help <category>' for details.");
            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void AppendCommandLine(StringBuilder sb, ICommand cmd)
        {
            sb.Append("  ").Append(cmd.Name);
            if (!string.IsNullOrEmpty(cmd.Description))
                sb.Append(" - ").Append(cmd.Description);
            sb.AppendLine();
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

        private static void CompleteHelp(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex != 0) return;

            foreach (var command in ctx.Terminal.Registry.All)
            {
                if (!StartsWith(command.Name, ctx.CurrentToken)) continue;
                results.Add(new CompletionItem(command.Name, command.Name, command.Description,
                    command.Category, CompletionKind.Command));
            }

            foreach (var category in ctx.Terminal.Registry.All.Select(c => c.Category).Distinct())
            {
                if (!StartsWith(category, ctx.CurrentToken)) continue;
                results.Add(new CompletionItem(category, category, "command category",
                    "Category", CompletionKind.Keyword));
            }
        }

        private static void CompleteAlias(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            if (ctx.ArgumentIndex == 0 && StartsWith("remove", ctx.CurrentToken))
                results.Add(new CompletionItem("remove", "remove", "remove an alias",
                    "Alias", CompletionKind.Keyword));

            if (ctx.ArgumentIndex == 1 && FirstArg(ctx.Input).Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var alias in ctx.Terminal.Alias.All)
                {
                    if (!StartsWith(alias.Key, ctx.CurrentToken)) continue;
                    results.Add(new CompletionItem(alias.Key, alias.Key,
                        "alias -> " + alias.Value, "Alias", CompletionKind.Alias));
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
    }
}
