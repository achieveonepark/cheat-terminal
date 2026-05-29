using System;

namespace UniTerminal
{
    /// <summary>
    /// Decides when the terminal should be opened (e.g. tapping the top-right corner).
    /// Swap this out to change the open gesture without touching the core.
    /// </summary>
    public interface ITerminalTrigger
    {
        bool Enabled { get; set; }
        event Action OnTriggered;
    }
}
