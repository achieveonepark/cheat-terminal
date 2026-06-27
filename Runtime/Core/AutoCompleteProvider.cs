using System;
using System.Collections.Generic;
using System.Linq;

namespace Achieve.CheatTerminal.Core
{
    /// <summary>Builds cached command and argument suggestions for the current input.</summary>
    public sealed class AutoCompleteProvider
    {
        private readonly Terminal _terminal;
        private readonly List<CompletionItem> _buffer = new List<CompletionItem>();
        private readonly List<CompletionItem> _commandCache = new List<CompletionItem>();
        private readonly Dictionary<string, List<CompletionItem>> _usageCache =
            new Dictionary<string, List<CompletionItem>>(StringComparer.OrdinalIgnoreCase);

        public AutoCompleteProvider(Terminal terminal)
        {
            _terminal = terminal;
            _terminal.Registry.Changed += RebuildCache;
            RebuildCache();
        }

        public IReadOnlyList<CompletionItem> Complete(string input)
        {
            _buffer.Clear();

            var state = Tokenize(input ?? string.Empty);
            if (state.Tokens.Count == 0)
            {
                AddCommands(string.Empty);
                return _buffer;
            }

            if (state.Tokens.Count == 1 && !state.EndsWithSpace)
            {
                AddCommands(state.Tokens[0]);
                return _buffer;
            }

            string commandName = state.Tokens[0];
            if (!_terminal.Registry.TryGet(commandName, out var command))
                return _buffer;

            int argIndex = state.EndsWithSpace
                ? state.Tokens.Count - 1
                : state.Tokens.Count - 2;
            string current = state.EndsWithSpace ? string.Empty : state.Tokens[state.Tokens.Count - 1];

            AddUsageCandidates(command, argIndex, current);

            if (command is ICommandCompletionProvider provider)
            {
                var context = new CommandCompletionContext(
                    _terminal, input, commandName, current, argIndex, state.EndsWithSpace);
                provider.Complete(context, _buffer);
            }

            SortAndDedupe();
            return _buffer;
        }

        private void AddCommands(string prefix)
        {
            foreach (var item in _commandCache)
            {
                if (StartsWith(item.InsertText, prefix))
                    _buffer.Add(item);
            }

            foreach (var alias in _terminal.Alias.All)
            {
                if (!StartsWith(alias.Key, prefix)) continue;
                _buffer.Add(new CompletionItem(
                    alias.Key,
                    alias.Key,
                    "alias -> " + alias.Value,
                    "Alias",
                    CompletionKind.Alias));
            }

            SortAndDedupe();
        }

        private void AddUsageCandidates(ICommand command, int argIndex, string current)
        {
            if (!_usageCache.TryGetValue(command.Name + ":" + argIndex, out var candidates))
                return;

            foreach (var candidate in candidates)
            {
                if (StartsWith(candidate.InsertText, current))
                    _buffer.Add(candidate);
            }
        }

        private void RebuildCache()
        {
            _commandCache.Clear();
            _usageCache.Clear();

            foreach (var command in _terminal.Registry.All)
            {
                _commandCache.Add(new CompletionItem(
                    command.Name,
                    command.Name,
                    string.IsNullOrEmpty(command.Description) ? command.Usage : command.Description,
                    command.Category,
                    CompletionKind.Command));

                var usageTokens = Tokenize(command.Usage ?? command.Name).Tokens;
                for (int argIndex = 0; argIndex < usageTokens.Count - 1; argIndex++)
                {
                    var candidates = new List<CompletionItem>();
                    foreach (var option in ExtractOptions(usageTokens[argIndex + 1]))
                    {
                        candidates.Add(new CompletionItem(
                            option,
                            option,
                            command.Usage,
                            command.Category,
                            CompletionKind.Keyword));
                    }

                    if (candidates.Count > 0)
                        _usageCache[command.Name + ":" + argIndex] = candidates;
                }
            }
        }

        private static IEnumerable<string> ExtractOptions(string usageToken)
        {
            if (string.IsNullOrEmpty(usageToken))
                yield break;

            if (!usageToken.StartsWith("<", StringComparison.Ordinal) ||
                !usageToken.EndsWith(">", StringComparison.Ordinal))
                yield break;

            string token = usageToken.Trim('[', ']', '<', '>');
            var parts = token.Split('|');
            if (parts.Length <= 1)
                yield break;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (!string.IsNullOrEmpty(part) && part.IndexOf(' ') < 0)
                    yield return part;
            }
        }

        private void SortAndDedupe()
        {
            var deduped = _buffer
                .GroupBy(i => i.InsertText, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(i => i.Kind)
                .ThenBy(i => i.InsertText, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _buffer.Clear();
            _buffer.AddRange(deduped);
        }

        private static bool StartsWith(string value, string prefix)
            => string.IsNullOrEmpty(prefix) ||
               (!string.IsNullOrEmpty(value) &&
                value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        private static TokenState Tokenize(string input)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(input))
                return new TokenState(tokens, false);

            bool inQuotes = false;
            var current = string.Empty;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current);
                        current = string.Empty;
                    }
                    continue;
                }

                current += c;
            }

            if (current.Length > 0)
                tokens.Add(current);

            return new TokenState(tokens, char.IsWhiteSpace(input[input.Length - 1]));
        }

        private readonly struct TokenState
        {
            public readonly List<string> Tokens;
            public readonly bool EndsWithSpace;

            public TokenState(List<string> tokens, bool endsWithSpace)
            {
                Tokens = tokens;
                EndsWithSpace = endsWithSpace;
            }
        }
    }
}
