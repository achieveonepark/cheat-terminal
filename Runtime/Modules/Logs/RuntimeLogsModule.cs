using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniTerminal.Core;
using UnityEngine;

namespace UniTerminal.Modules
{
    /// <summary>
    /// Runtime log viewer backed by <see cref="LogCapture"/>.
    ///   logs                  last N entries
    ///   logs &lt;n&gt;            last n entries
    ///   logs error|warning|info   filter by level
    ///   logs &lt;text&gt;         filter by substring (e.g. "logs network")
    ///   logs find &lt;text&gt;    explicit substring search
    ///   logs clear            clear the buffer
    ///   logs export           write the buffer to a file (works on device)
    /// </summary>
    public sealed class RuntimeLogsModule : ITerminalModule
    {
        private const int DefaultShow = 30;

        private LogCapture _capture;

        public string Name => "Logs";

        public LogCapture Capture => _capture;

        public void Install(Terminal terminal)
        {
            var go = new GameObject("[UniTerminal.LogCapture]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _capture = go.AddComponent<LogCapture>();

            terminal.RegisterCommand(new DelegateCommand("logs", Run,
                "Runtime logs viewer / search / export",
                "Logs", "logs [n | error | warning | info | <text> | find <text> | clear | export]"));
        }

        private void Run(CommandContext ctx)
        {
            if (!ctx.Has(0))
            {
                Print(ctx, _capture.Entries, DefaultShow, $"last {DefaultShow} logs");
                return;
            }

            string sub = ctx.Args[0].ToLowerInvariant();
            switch (sub)
            {
                case "clear":
                    _capture.Clear();
                    ctx.Output.WriteLine("Logs cleared.", LogLevel.Success);
                    return;

                case "export":
                    Export(ctx);
                    return;

                case "find":
                case "search":
                    if (!ctx.Has(1)) { ctx.Output.WriteLine("usage: logs find <text>", LogLevel.Error); return; }
                    FilterText(ctx, ctx.Args[1]);
                    return;

                case "error":
                case "errors":
                    FilterLevel(ctx, "errors", LogType.Error, LogType.Exception, LogType.Assert);
                    return;

                case "warning":
                case "warnings":
                case "warn":
                    FilterLevel(ctx, "warnings", LogType.Warning);
                    return;

                case "info":
                case "log":
                    FilterLevel(ctx, "info", LogType.Log);
                    return;

                default:
                    if (int.TryParse(sub, out int n))
                        Print(ctx, _capture.Entries, Mathf.Max(1, n), $"last {n} logs");
                    else
                        FilterText(ctx, ctx.Args[0]); // e.g. "logs network"
                    return;
            }
        }

        private void FilterText(CommandContext ctx, string text)
        {
            var matches = _capture.Entries.Where(
                e => e.Message != null &&
                     e.Message.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
            Print(ctx, matches, DefaultShow, $"logs matching \"{text}\"");
        }

        private void FilterLevel(CommandContext ctx, string label, params LogType[] types)
        {
            var set = new HashSet<LogType>(types);
            var matches = _capture.Entries.Where(e => set.Contains(e.Type));
            Print(ctx, matches, DefaultShow, label);
        }

        private static void Print(CommandContext ctx, IEnumerable<LogCapture.Entry> source,
            int count, string header)
        {
            var list = source.ToList();
            int start = Mathf.Max(0, list.Count - count);

            ctx.Output.WriteLine($"== {header} ({list.Count - start}/{list.Count}) ==", LogLevel.System);
            if (list.Count == 0)
            {
                ctx.Output.WriteLine("(no logs)");
                return;
            }

            for (int i = start; i < list.Count; i++)
            {
                var e = list[i];
                ctx.Output.WriteLine($"[{e.Time:0.0}] {Tag(e.Type)} {e.Message}", ToLevel(e.Type));
            }
        }

        private void Export(CommandContext ctx)
        {
            var sb = new StringBuilder();
            foreach (var e in _capture.Entries)
            {
                sb.Append('[').Append(e.Time.ToString("0.0")).Append("] ")
                  .Append(e.Type).Append(": ").AppendLine(e.Message);

                if (!string.IsNullOrEmpty(e.StackTrace) &&
                    (e.Type == LogType.Error || e.Type == LogType.Exception || e.Type == LogType.Assert))
                    sb.AppendLine(e.StackTrace);
            }

            string path = Path.Combine(Application.persistentDataPath,
                $"cheatterminal-logs-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            try
            {
                File.WriteAllText(path, sb.ToString());
                ctx.Output.WriteLine($"Exported {_capture.Entries.Count} logs to:", LogLevel.Success);
                ctx.Output.WriteLine(path);
            }
            catch (Exception ex)
            {
                ctx.Output.WriteLine($"Export failed: {ex.Message}", LogLevel.Error);
            }
        }

        private static string Tag(LogType type) => type switch
        {
            LogType.Error => "[ERR]",
            LogType.Exception => "[EXC]",
            LogType.Assert => "[AST]",
            LogType.Warning => "[WRN]",
            _ => "[INF]"
        };

        private static LogLevel ToLevel(LogType type) => type switch
        {
            LogType.Error or LogType.Exception or LogType.Assert => LogLevel.Error,
            LogType.Warning => LogLevel.Warning,
            _ => LogLevel.Info
        };
    }
}
