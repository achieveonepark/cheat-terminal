using System;

namespace UniTerminal
{
    /// <summary>The terminal core facade: register commands, execute input, control the view.</summary>
    public interface ITerminal
    {
        ICommandRegistry Registry { get; }
        ICommandOutput Output { get; }
        ICommandHistory History { get; }
        IAliasResolver Alias { get; }
        IAutoCompleteProvider AutoComplete { get; }
        IArgumentConverter Converter { get; }
        ITerminalView View { get; }

        /// <summary>Parse and run a line of input.</summary>
        void Execute(string input);

        /// <summary>Scan an instance for [Terminal] methods and register them.</summary>
        void Register(object target);

        /// <summary>Scan a type for static [Terminal] methods and register them.</summary>
        void RegisterStatic(Type type);

        /// <summary>Register a pre-built command.</summary>
        void RegisterCommand(ICommand command);

        void AttachView(ITerminalView view);

        void Open();
        void Close();
        void Toggle();
    }
}
