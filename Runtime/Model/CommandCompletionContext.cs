namespace Achieve.CheatTerminal
{
    /// <summary>Context passed to command-specific completion providers.</summary>
    public readonly struct CommandCompletionContext
    {
        public readonly Terminal Terminal;
        public readonly string Input;
        public readonly string CommandName;
        public readonly string CurrentToken;
        public readonly int ArgumentIndex;
        public readonly bool IsNewToken;

        public CommandCompletionContext(Terminal terminal, string input, string commandName,
            string currentToken, int argumentIndex, bool isNewToken)
        {
            Terminal = terminal;
            Input = input ?? string.Empty;
            CommandName = commandName ?? string.Empty;
            CurrentToken = currentToken ?? string.Empty;
            ArgumentIndex = argumentIndex;
            IsNewToken = isNewToken;
        }
    }
}
