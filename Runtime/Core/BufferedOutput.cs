using System.Collections.Generic;

namespace UniTerminal.Core
{
    /// <summary>
    /// Default output sink. Keeps a capped ring of recent lines and forwards to the
    /// attached view. Buffered lines are replayed when a view attaches, so output
    /// produced before the UI exists is not lost.
    /// </summary>
    public sealed class BufferedOutput : ICommandOutput
    {
        public readonly struct Line
        {
            public readonly string Text;
            public readonly LogLevel Level;
            public Line(string text, LogLevel level) { Text = text; Level = level; }
        }

        private readonly Queue<Line> _buffer = new Queue<Line>();
        private readonly int _capacity;
        private ITerminalView _view;

        public IReadOnlyCollection<Line> Buffer => _buffer;

        public BufferedOutput(int capacity = 500)
        {
            _capacity = capacity < 1 ? 1 : capacity;
        }

        public void AttachView(ITerminalView view)
        {
            _view = view;
            if (_view == null) return;
            foreach (var line in _buffer)
                _view.AppendLine(line.Text, line.Level);
        }

        public void Write(string message) => WriteLine(message, LogLevel.Info);

        public void WriteLine(string message) => WriteLine(message, LogLevel.Info);

        public void WriteLine(string message, LogLevel level)
        {
            message ??= string.Empty;
            _buffer.Enqueue(new Line(message, level));
            while (_buffer.Count > _capacity)
                _buffer.Dequeue();

            _view?.AppendLine(message, level);
        }

        public void Clear()
        {
            _buffer.Clear();
            _view?.Clear();
        }
    }
}
