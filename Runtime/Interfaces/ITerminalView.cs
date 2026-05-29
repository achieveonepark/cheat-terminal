using System;
using System.Collections.Generic;

namespace UniTerminal
{
    /// <summary>
    /// The presentation layer for the terminal. The default is a uGUI overlay, but any
    /// implementation (native, UI Toolkit, headless) can be injected into the core.
    /// </summary>
    public interface ITerminalView
    {
        bool IsOpen { get; }

        void Open();
        void Close();
        void Toggle();

        void AppendLine(string text, LogLevel level);
        void Clear();

        void ShowSuggestions(IReadOnlyList<string> suggestions);

        /// <summary>Called once by the core when the view is attached.</summary>
        void Bind(ITerminal terminal);

        /// <summary>Raised when the user submits a line of input.</summary>
        event Action<string> OnSubmit;

        /// <summary>Raised as the user edits input (used for auto-complete).</summary>
        event Action<string> OnInputChanged;
    }
}
