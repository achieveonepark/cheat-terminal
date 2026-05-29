namespace UniTerminal.Core
{
    /// <summary>
    /// Fluent assembly of a <see cref="Terminal"/>. Every collaborator has a default
    /// implementation; call the With* methods to inject your own.
    /// </summary>
    public sealed class TerminalBuilder
    {
        private ICommandRegistry _registry;
        private ICommandParser _parser;
        private ICommandHistory _history;
        private IAliasResolver _alias;
        private IAutoCompleteProvider _autoComplete;
        private IArgumentConverter _converter;
        private ICommandOutput _output;

        public TerminalBuilder WithRegistry(ICommandRegistry registry) { _registry = registry; return this; }
        public TerminalBuilder WithParser(ICommandParser parser) { _parser = parser; return this; }
        public TerminalBuilder WithHistory(ICommandHistory history) { _history = history; return this; }
        public TerminalBuilder WithAlias(IAliasResolver alias) { _alias = alias; return this; }
        public TerminalBuilder WithAutoComplete(IAutoCompleteProvider autoComplete) { _autoComplete = autoComplete; return this; }
        public TerminalBuilder WithConverter(IArgumentConverter converter) { _converter = converter; return this; }
        public TerminalBuilder WithOutput(ICommandOutput output) { _output = output; return this; }

        public Terminal Build()
        {
            _registry ??= new CommandRegistry();
            _parser ??= new CommandParser();
            _history ??= new CommandHistory();
            _alias ??= new AliasResolver();
            _converter ??= new ArgumentConverter();
            _output ??= new BufferedOutput();
            _autoComplete ??= new AutoCompleteProvider(_registry);

            return new Terminal(_registry, _parser, _history, _alias,
                _autoComplete, _converter, _output);
        }
    }
}
