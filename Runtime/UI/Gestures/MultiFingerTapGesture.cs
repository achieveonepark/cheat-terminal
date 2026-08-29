using System;
using UnityEngine;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>
    /// Detects "tap with N fingers, M times in a row" without touching the UI event system,
    /// so the gesture works anywhere on screen and never steals input from the game.
    /// A tap counts only when the peak finger count for that press matches
    /// <see cref="Fingers"/> exactly, which keeps the 3-finger and 4-finger gestures apart.
    /// </summary>
    [AddComponentMenu("Achieve.CheatTerminal/Multi Finger Tap Gesture")]
    public sealed class MultiFingerTapGesture : MonoBehaviour
    {
        [SerializeField] private int _fingers = 3;
        [SerializeField] private int _taps = 3;
        [SerializeField] private float _tapWindow = 0.8f;
        [SerializeField] private float _maxTapDuration = 0.7f;
        [SerializeField] private TerminalShortcutKey _fallbackKey = TerminalShortcutKey.None;
        [SerializeField] private TerminalShortcutKey _alternateKey = TerminalShortcutKey.None;

        private bool _pressing;
        private int _peakFingers;
        private float _pressStartTime;
        private int _tapCount;
        private float _lastTapTime;

        /// <summary>Fingers that must be down simultaneously for one tap.</summary>
        public int Fingers
        {
            get => _fingers;
            set => _fingers = Mathf.Max(1, value);
        }

        /// <summary>How many taps in a row complete the gesture.</summary>
        public int Taps
        {
            get => _taps;
            set => _taps = Mathf.Max(1, value);
        }

        /// <summary>Maximum gap between two taps of the same sequence, in unscaled seconds.</summary>
        public float TapWindow
        {
            get => _tapWindow;
            set => _tapWindow = Mathf.Max(0.05f, value);
        }

        /// <summary>A press held longer than this is a drag, not a tap.</summary>
        public float MaxTapDuration
        {
            get => _maxTapDuration;
            set => _maxTapDuration = Mathf.Max(0.05f, value);
        }

        /// <summary>Optional keyboard shortcut that fires the gesture on desktop and in the editor.</summary>
        public TerminalShortcutKey FallbackKey
        {
            get => _fallbackKey;
            set => _fallbackKey = value;
        }

        /// <summary>Second keyboard shortcut for the same gesture, for people who prefer another key.</summary>
        public TerminalShortcutKey AlternateKey
        {
            get => _alternateKey;
            set => _alternateKey = value;
        }

        public event Action Performed;

        public static MultiFingerTapGesture Attach(GameObject host, int fingers, int taps,
            TerminalShortcutKey fallbackKey = TerminalShortcutKey.None,
            TerminalShortcutKey alternateKey = TerminalShortcutKey.None)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            var gesture = host.AddComponent<MultiFingerTapGesture>();
            gesture.Fingers = fingers;
            gesture.Taps = taps;
            gesture.FallbackKey = fallbackKey;
            gesture.AlternateKey = alternateKey;
            return gesture;
        }

        private void Update()
        {
            if (TerminalInput.WasKeyPressedThisFrame(_fallbackKey) ||
                TerminalInput.WasKeyPressedThisFrame(_alternateKey))
            {
                Fire();
                return;
            }

            float now = Time.unscaledTime;
            int touchCount = TerminalInput.ActiveTouchCount();

            if (touchCount > 0)
            {
                if (!_pressing)
                {
                    _pressing = true;
                    _peakFingers = 0;
                    _pressStartTime = now;
                }

                if (touchCount > _peakFingers)
                    _peakFingers = touchCount;
                return;
            }

            if (!_pressing)
            {
                // Idle: let a half-finished sequence expire.
                if (_tapCount > 0 && now - _lastTapTime > _tapWindow)
                    _tapCount = 0;
                return;
            }

            // All fingers lifted: judge the press that just ended.
            _pressing = false;
            bool isMatchingTap = _peakFingers == _fingers && now - _pressStartTime <= _maxTapDuration;
            _peakFingers = 0;
            if (!isMatchingTap)
                return;

            if (_tapCount > 0 && now - _lastTapTime > _tapWindow)
                _tapCount = 0;

            _tapCount++;
            _lastTapTime = now;

            if (_tapCount < Mathf.Max(1, _taps))
                return;

            _tapCount = 0;
            Fire();
        }

        private void Fire()
        {
            _tapCount = 0;
            _pressing = false;
            _peakFingers = 0;
            Performed?.Invoke();
        }
    }
}
