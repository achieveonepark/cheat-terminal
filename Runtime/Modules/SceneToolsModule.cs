using System.Collections.Generic;
using System.Text;
using Achieve.CheatTerminal.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Achieve.CheatTerminal.Modules
{
    /// <summary>scene list | scene load &lt;name&gt; [additive] | scene unload &lt;name&gt;</summary>
    public sealed class SceneToolsModule : ITerminalModule
    {
        public string Name => "Scene";

        public void Install(Terminal terminal)
        {
            terminal.RegisterCommand(new DelegateCommand(
                "scene", Run,
                "Scene tools: list | load <name> [additive] | unload <name>",
                "Scene", "scene <list|load|unload> [name] [additive]",
                new CommandCompletionProvider(Complete)));
        }

        private static void Run(CommandContext ctx)
        {
            string sub = ctx.GetString(0, "list").ToLowerInvariant();
            switch (sub)
            {
                case "list": List(ctx); break;
                case "load": Load(ctx); break;
                case "unload": Unload(ctx); break;
                default: ctx.Output.WriteLine($"Unknown scene subcommand: {sub}", LogLevel.Error); break;
            }
        }

        private static void List(CommandContext ctx)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Loaded:");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                bool active = s == SceneManager.GetActiveScene();
                sb.AppendLine($"  {(active ? "*" : " ")} {s.name} ({(s.isLoaded ? "loaded" : "loading")})");
            }

            sb.AppendLine("In build settings:");
            int count = SceneManager.sceneCountInBuildSettings;
            if (count == 0)
                sb.AppendLine("  (none)");
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                sb.AppendLine($"  {i,2}  {System.IO.Path.GetFileNameWithoutExtension(path)}");
            }

            ctx.Output.WriteLine(sb.ToString().TrimEnd(), LogLevel.System);
        }

        private static void Load(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine("usage: scene load <name> [additive]", LogLevel.Error); return; }

            string name = ctx.Args[1];
            if (!Application.CanStreamedLevelBeLoaded(name))
            {
                ctx.Output.WriteLine($"Scene '{name}' is not in build settings.", LogLevel.Error);
                return;
            }

            bool additive = ctx.GetString(2, "").Equals("additive", System.StringComparison.OrdinalIgnoreCase);
            SceneManager.LoadScene(name, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            ctx.Output.WriteLine($"Loading '{name}' ({(additive ? "additive" : "single")})", LogLevel.Success);
        }

        private static void Unload(CommandContext ctx)
        {
            if (!ctx.Has(1)) { ctx.Output.WriteLine("usage: scene unload <name>", LogLevel.Error); return; }

            string name = ctx.Args[1];
            var scene = SceneManager.GetSceneByName(name);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                ctx.Output.WriteLine($"Scene '{name}' is not loaded.", LogLevel.Error);
                return;
            }

            SceneManager.UnloadSceneAsync(scene);
            ctx.Output.WriteLine($"Unloading '{name}'", LogLevel.Success);
        }

        private static void Complete(CommandCompletionContext ctx, List<CompletionItem> results)
        {
            string sub = FirstArg(ctx.Input);

            if (ctx.ArgumentIndex == 1 && sub.Equals("load", System.StringComparison.OrdinalIgnoreCase))
            {
                int count = SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < count; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (!StartsWith(name, ctx.CurrentToken)) continue;
                    results.Add(new CompletionItem(name, name, "scene in build settings",
                        "Scene", CompletionKind.Argument));
                }
                return;
            }

            if (ctx.ArgumentIndex == 1 && sub.Equals("unload", System.StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!StartsWith(scene.name, ctx.CurrentToken)) continue;
                    results.Add(new CompletionItem(scene.name, scene.name, "loaded scene",
                        "Scene", CompletionKind.Argument));
                }
                return;
            }

            if (ctx.ArgumentIndex == 2 &&
                sub.Equals("load", System.StringComparison.OrdinalIgnoreCase) &&
                StartsWith("additive", ctx.CurrentToken))
            {
                results.Add(new CompletionItem("additive", "additive",
                    "load without unloading current scenes", "Scene", CompletionKind.Keyword));
            }
        }

        private static string FirstArg(string input)
        {
            var parts = (input ?? string.Empty)
                .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }

        private static bool StartsWith(string value, string prefix)
            => string.IsNullOrEmpty(prefix) ||
               (!string.IsNullOrEmpty(value) &&
                value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase));
    }
}
