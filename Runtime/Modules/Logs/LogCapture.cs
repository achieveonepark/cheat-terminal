using System.Collections.Generic;
using UnityEngine;

namespace Achieve.CheatTerminal.Modules
{
    /// <summary>
    /// Captures Unity console output (<see cref="Application.logMessageReceived"/>) into a
    /// capped ring buffer so the terminal can display, filter and export it - including on
    /// device, where the editor console is not available.
    /// </summary>
    public sealed class LogCapture : MonoBehaviour
    {
        public readonly struct Entry
        {
            public readonly string Message;
            public readonly string StackTrace;
            public readonly LogType Type;
            public readonly float Time;

            public Entry(string message, string stackTrace, LogType type, float time)
            {
                Message = message;
                StackTrace = stackTrace;
                Type = type;
                Time = time;
            }
        }

        private readonly Queue<Entry> _entries = new Queue<Entry>();
        private int _capacity = 1000;

        public IReadOnlyCollection<Entry> Entries => _entries;

        public int Capacity
        {
            get => _capacity;
            set => _capacity = Mathf.Max(1, value);
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            _entries.Enqueue(new Entry(condition, stackTrace, type, Time.unscaledTime));
            while (_entries.Count > _capacity)
                _entries.Dequeue();
        }

        public void Clear() => _entries.Clear();
    }
}
