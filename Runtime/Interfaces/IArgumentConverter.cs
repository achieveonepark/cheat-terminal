using System;

namespace UniTerminal
{
    /// <summary>Converts raw string arguments into strongly typed method parameters.</summary>
    public interface IArgumentConverter
    {
        bool CanConvert(Type type);
        object Convert(string raw, Type type);
    }
}
