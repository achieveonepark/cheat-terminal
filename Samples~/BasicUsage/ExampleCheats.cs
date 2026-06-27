using System.Collections.Generic;
using System.Globalization;
using Achieve.CheatTerminal;
using UnityEngine;

namespace Achieve.CheatTerminalSamples
{
    /// <summary>
    /// Drop this on any GameObject. It explicitly registers commands with the
    /// terminal, then you can open the console (tap the top-right corner)
    /// and run: gold 100000 / level 99 / god / pos 0 5 0 / heal
    /// </summary>
    public sealed class ExampleCheats : MonoBehaviour
    {
        private int _gold;
        private int _level = 1;
        private bool _godMode;

        private void Start()
        {
            var terminal = TerminalBehaviour.Current;

            terminal.RegisterCommand("gold", AddGold,
                "Add gold", "Cheats", "gold <amount>");
            terminal.RegisterCommand("level", SetLevel,
                "Set player level", "Cheats", "level <level>");
            terminal.RegisterCommand("god", ToggleGod,
                "Toggle god mode", "Cheats", "god");
            terminal.RegisterCommand("pos", Teleport,
                "Teleport player", "Cheats", "pos <x> <y> <z>");
            terminal.RegisterCommand("heal", Heal,
                "Heal (default 100)", "Cheats", "heal [amount]");

            terminal.RegisterCommand("ping",
                ctx => ctx.Output.WriteLine("pong"),
                "Print pong", "Debug", "ping");
            terminal.RegisterCommand("timescale", SetTimeScale,
                "Set Time.timeScale", "Debug", "timescale <scale>");

            terminal.RegisterDataTable("items", "Sample Items", GetSampleItems,
                "Example data table exposed to the terminal");
        }

        private void AddGold(CommandContext ctx)
        {
            if (!TryGetInt(ctx, 0, "gold <amount>", out int amount))
                return;

            _gold += amount;
            Debug.Log($"Gold is now {_gold}");
        }

        private void SetLevel(CommandContext ctx)
        {
            if (!TryGetInt(ctx, 0, "level <level>", out int level))
                return;

            _level = level;
            ctx.Output.WriteLine($"Level set to {_level}", LogLevel.Success);
        }

        private void ToggleGod(CommandContext ctx)
        {
            _godMode = !_godMode;
            ctx.Output.WriteLine($"God mode: {(_godMode ? "ON" : "OFF")}",
                _godMode ? LogLevel.Success : LogLevel.Warning);
        }

        private void Teleport(CommandContext ctx)
        {
            if (!TryGetFloat(ctx, 0, "pos <x> <y> <z>", out float x) ||
                !TryGetFloat(ctx, 1, "pos <x> <y> <z>", out float y) ||
                !TryGetFloat(ctx, 2, "pos <x> <y> <z>", out float z))
                return;

            transform.position = new Vector3(x, y, z);
        }

        private void Heal(CommandContext ctx)
        {
            int amount = 100;
            if (ctx.Has(0) && !TryGetInt(ctx, 0, "heal [amount]", out amount))
                return;

            ctx.Output.WriteLine($"Healed {amount} HP", LogLevel.Success);
        }

        private static void SetTimeScale(CommandContext ctx)
        {
            if (!TryGetFloat(ctx, 0, "timescale <scale>", out float scale))
                return;

            Time.timeScale = scale;
            ctx.Output.WriteLine($"Time.timeScale = {Time.timeScale:0.###}", LogLevel.Success);
        }

        private static bool TryGetInt(CommandContext ctx, int index, string usage, out int value)
        {
            if (ctx.Has(index) &&
                int.TryParse(ctx.Args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;

            value = default;
            ctx.Output.WriteLine($"Usage: {usage}", LogLevel.Error);
            return false;
        }

        private static bool TryGetFloat(CommandContext ctx, int index, string usage, out float value)
        {
            if (ctx.Has(index) &&
                float.TryParse(ctx.Args[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            value = default;
            ctx.Output.WriteLine($"Usage: {usage}", LogLevel.Error);
            return false;
        }

        private static IEnumerable<DataTableRow> GetSampleItems()
        {
            yield return new DataTableRow("sword_001", "Bronze Sword", "Starter weapon",
                new Dictionary<string, string>
                {
                    ["type"] = "weapon",
                    ["attack"] = "5"
                });
            yield return new DataTableRow("potion_hp", "Health Potion", "Restores HP",
                new Dictionary<string, string>
                {
                    ["type"] = "consumable",
                    ["heal"] = "50"
                });
        }
    }
}
