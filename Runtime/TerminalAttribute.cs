using System;

namespace Achieve.CheatTerminal
{
    /// <summary>
    /// Marks a method as a terminal command.
    /// The template's first token is the command name; {0}, {1}... bind supplied
    /// arguments to method parameters by index. With no placeholders, arguments
    /// bind positionally to the method's parameters (a <see cref="CommandContext"/>
    /// parameter is injected automatically and never consumes a user argument).
    /// </summary>
    /// <example>
    /// [Terminal("gold {0}", Description = "Add gold to the player")]
    /// public void AddGold(int amount) { ... }
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class TerminalAttribute : Attribute
    {
        /// <summary>The command template, e.g. "gold {0}".</summary>
        public string Template { get; }

        /// <summary>Human readable description shown by the help command.</summary>
        public string Description { get; set; }

        /// <summary>Optional grouping category, defaults to "General".</summary>
        public string Category { get; set; }

        public TerminalAttribute(string template)
        {
            Template = template;
        }
    }
}
