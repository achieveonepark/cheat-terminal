using System;
using System.Collections.Generic;
using System.Text;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>Builds named HUD inputs from the same usage displayed by terminal help.</summary>
    public sealed class CommandForm
    {
        public readonly struct Parameter
        {
            public string Name { get; }
            public bool Optional { get; }
            public bool Multiple { get; }
            public string[] Suggestions { get; }

            internal Parameter(string name, bool optional)
            {
                Name = name;
                Optional = optional;
                Multiple = name.EndsWith("...", StringComparison.Ordinal) || name == "args";
                Suggestions = name.IndexOf('|') >= 0 ? name.Split('|') : Array.Empty<string>();
            }
        }

        private readonly List<Parameter> _parameters = new List<Parameter>();
        public IReadOnlyList<Parameter> Parameters => _parameters;
        public string CommandName { get; }

        public CommandForm(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            CommandName = command.Name;
            string usage = command.Usage ?? command.Name;
            int start = usage.IndexOf(' ');
            if (start < 0) return;

            for (int i = start; i < usage.Length; i++)
            {
                char c = usage[i];
                if (char.IsWhiteSpace(c)) continue;
                bool optional = c == '[';
                if (optional || c == '<')
                {
                    int end = usage.IndexOf(optional ? ']' : '>', i + 1);
                    if (end < 0) break;
                    AddParameters(usage.Substring(i + 1, end - i - 1), optional);
                    i = end;
                }
                else
                {
                    // Literal subcommands and choices also need an input, never a command line.
                    int end = i;
                    while (end < usage.Length && !char.IsWhiteSpace(usage[end])) end++;
                    AddParameters(usage.Substring(i, end - i), false);
                    i = end - 1;
                }
            }
        }

        private void AddParameters(string group, bool optional)
        {
            foreach (string part in group.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                _parameters.Add(new Parameter(part, optional));
        }

        public bool TryBuild(IReadOnlyList<string> values, out string input, out string error)
        {
            var builder = new StringBuilder(CommandName);
            bool omitted = false;
            for (int i = 0; i < _parameters.Count; i++)
            {
                var parameter = _parameters[i];
                string value = values != null && i < values.Count ? values[i] : null;
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (!parameter.Optional)
                    {
                        input = null;
                        error = "Enter " + parameter.Name + ".";
                        return false;
                    }
                    omitted = true;
                    continue;
                }

                if (omitted)
                {
                    input = null;
                    error = "Fill earlier optional parameters first (" + parameter.Name + ").";
                    return false;
                }

                if (parameter.Multiple)
                {
                    // Rest arguments are intentionally several tokens, e.g. rb velocity args.
                    var parsed = new CommandParser().Parse("args " + value);
                    foreach (string token in parsed.Args) AppendToken(builder, token);
                }
                else AppendToken(builder, value);
            }

            input = builder.ToString();
            error = null;
            return true;
        }

        private static void AppendToken(StringBuilder builder, string value)
        {
            builder.Append(' ').Append('"');
            builder.Append(value.Replace("\\", "\\\\").Replace("\"", "\\\""));
            builder.Append('"');
        }
    }
}
