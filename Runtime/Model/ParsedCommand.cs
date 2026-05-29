using System;
using System.Collections.Generic;

namespace Achieve.CheatTerminal
{
    /// <summary>Result of parsing a raw input line.</summary>
    public readonly struct ParsedCommand
    {
        public static readonly ParsedCommand Empty =
            new ParsedCommand(string.Empty, Array.Empty<string>());

        public string Name { get; }
        public IReadOnlyList<string> Args { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Name);

        public ParsedCommand(string name, IReadOnlyList<string> args)
        {
            Name = name;
            Args = args ?? Array.Empty<string>();
        }
    }
}
