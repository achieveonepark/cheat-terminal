namespace UniTerminal
{
    /// <summary>Turns a raw input string into a command name and argument list.</summary>
    public interface ICommandParser
    {
        ParsedCommand Parse(string input);
    }
}
