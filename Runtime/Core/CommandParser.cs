using System.Collections.Generic;
using System.Text;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>
    /// Whitespace-splitting parser that respects double-quoted segments, so
    /// <c>set Player.Name "John Doe"</c> yields two arguments.
    /// </summary>
    public sealed class CommandParser
    {
        public ParsedCommand Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ParsedCommand.Empty;

            var tokens = Tokenize(input);
            if (tokens.Count == 0)
                return ParsedCommand.Empty;

            var name = tokens[0];
            var args = tokens.Count > 1 ? tokens.GetRange(1, tokens.Count - 1) : new List<string>();
            return new ParsedCommand(name, args);
        }

        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                    continue;
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            return tokens;
        }
    }
}
