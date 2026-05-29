using System.Text;
using Achieve.CheatTerminal.Core;
using UnityEngine;

namespace Achieve.CheatTerminal.Modules
{
    /// <summary>perf — print a snapshot of FPS, memory and render statistics.</summary>
    public sealed class PerformanceModule : ITerminalModule
    {
        private PerformanceSampler _sampler;

        public string Name => "Performance";

        public void Install(Terminal terminal)
        {
            var go = new GameObject("[Achieve.CheatTerminal.PerfSampler]");
            Object.DontDestroyOnLoad(go);
            _sampler = go.AddComponent<PerformanceSampler>();

            terminal.RegisterCommand(new DelegateCommand("perf", Run,
                "Show a performance snapshot", "Performance", "perf"));
        }

        private void Run(CommandContext ctx)
        {
            var s = _sampler.Get();
            var sb = new StringBuilder();
            sb.AppendLine($"FPS         : {s.Fps:0.0}  ({s.FrameMs:0.0} ms)");
            sb.AppendLine($"Memory      : {Mb(s.TotalAllocatedBytes)} MB total");
            sb.AppendLine($"Mono        : {Mb(s.MonoBytes)} MB");
            sb.AppendLine($"Managed     : {Mb(s.ManagedBytes)} MB");
            sb.AppendLine($"GC Count    : {s.GcCount}");
            if (s.RenderStatsValid)
            {
                sb.AppendLine($"Draw Calls  : {s.DrawCalls}");
                sb.AppendLine($"SetPass     : {s.SetPassCalls}");
                sb.AppendLine($"Batches     : {s.Batches}");
                sb.AppendLine($"Triangles   : {s.Triangles}");
                sb.AppendLine($"Vertices    : {s.Vertices}");
            }
            else
            {
                sb.AppendLine("Render stats: unavailable on this platform/build");
            }

            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static string Mb(long bytes) => (bytes / (1024f * 1024f)).ToString("0.0");
    }
}
