using System.Collections.Generic;
using System.Globalization;

namespace Achieve.CheatTerminal
{
    /// <summary>Everything a command needs at execution time.</summary>
    public sealed class CommandContext
    {
        public string RawInput { get; }
        public string CommandName { get; }
        public IReadOnlyList<string> Args { get; }
        public ICommandOutput Output { get; }
        public Terminal Terminal { get; }

        public int ArgCount => Args.Count;

        public CommandContext(string rawInput, string commandName,
            IReadOnlyList<string> args, ICommandOutput output, Terminal terminal)
        {
            RawInput = rawInput;
            CommandName = commandName;
            Args = args;
            Output = output;
            Terminal = terminal;
        }

        public bool Has(int index) => index >= 0 && index < Args.Count;

        public string GetString(int index, string fallback = null)
            => Has(index) ? Args[index] : fallback;

        public int GetInt(int index, int fallback = 0)
            => Has(index) && int.TryParse(Args[index], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var v) ? v : fallback;

        public float GetFloat(int index, float fallback = 0f)
            => Has(index) && float.TryParse(Args[index], NumberStyles.Float,
                CultureInfo.InvariantCulture, out var v) ? v : fallback;

        public bool GetBool(int index, bool fallback = false)
        {
            if (!Has(index)) return fallback;
            var s = Args[index];
            if (s == "1" || s.Equals("true", System.StringComparison.OrdinalIgnoreCase)
                || s.Equals("on", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (s == "0" || s.Equals("false", System.StringComparison.OrdinalIgnoreCase)
                || s.Equals("off", System.StringComparison.OrdinalIgnoreCase)) return false;
            return fallback;
        }
    }
}
