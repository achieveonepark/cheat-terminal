namespace Achieve.CheatTerminal
{
    /// <summary>
    /// A pluggable feature bundle that registers commands (and any runtime helpers)
    /// against the terminal core. Install what you need; nothing is forced on you.
    /// </summary>
    public interface ITerminalModule
    {
        string Name { get; }
        void Install(Terminal terminal);
    }
}
