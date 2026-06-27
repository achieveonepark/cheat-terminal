namespace Achieve.CheatTerminal
{
    public enum CompletionKind
    {
        Command,
        Keyword,
        Argument,
        DataTable,
        DataRow,
        Alias
    }

    /// <summary>A cached completion candidate shown while editing command input.</summary>
    public readonly struct CompletionItem
    {
        public readonly string InsertText;
        public readonly string Label;
        public readonly string Detail;
        public readonly string Category;
        public readonly CompletionKind Kind;

        public CompletionItem(string insertText, string label = null, string detail = null,
            string category = null, CompletionKind kind = CompletionKind.Argument)
        {
            InsertText = insertText ?? string.Empty;
            Label = string.IsNullOrEmpty(label) ? InsertText : label;
            Detail = detail ?? string.Empty;
            Category = category ?? string.Empty;
            Kind = kind;
        }
    }
}
