namespace Achieve.CheatTerminal
{
    /// <summary>Sink for terminal text output. Implement to redirect output anywhere.</summary>
    public interface ICommandOutput
    {
        void Write(string message);
        void WriteLine(string message);
        void WriteLine(string message, LogLevel level);
        void Clear();
    }
}
