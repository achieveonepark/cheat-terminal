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
            bool hasToken = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (inQuotes && c == '\\' && i + 1 < input.Length &&
                    (input[i + 1] == '"' || input[i + 1] == '\\'))
                {
                    sb.Append(input[++i]);
                    hasToken = true;
                    continue;
                }
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (hasToken)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                        hasToken = false;
                    }
                    continue;
                }

                sb.Append(c);
                hasToken = true;
            }

            if (hasToken)
                tokens.Add(sb.ToString());

            return tokens;
        }
    }
}
