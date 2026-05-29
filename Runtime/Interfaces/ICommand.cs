namespace Achieve.CheatTerminal
{
    /// <summary>A single executable terminal command.</summary>
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        string Category { get; }
        string Usage { get; }
        void Execute(CommandContext context);
    }
}
