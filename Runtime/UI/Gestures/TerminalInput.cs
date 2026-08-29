using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Achieve.CheatTerminal.UI
{
    /// <summary>
    /// Keyboard shortcuts offered as a desktop/editor fallback for touch gestures.
    /// Existing values keep their numbers so serialized components stay valid.
    /// </summary>
    public enum TerminalShortcutKey
    {
        None = 0,
        F9 = 1,
        F10 = 2,
        F1 = 3,
        F2 = 4,
        F3 = 5,
        F4 = 6,
        F11 = 7,
        F12 = 8,
        BackQuote = 9,
        Escape = 10
    }

    /// <summary>
    /// Minimal, reflection-free input source for the terminal gestures. Reads the legacy
    /// Input Manager when it is enabled, and the Input System package otherwise (the
    /// package is an optional dependency resolved through the asmdef version define).
    /// </summary>
    internal static class TerminalInput
    {
        /// <summary>Number of fingers currently down (touches that ended this frame excluded).</summary>
        public static int ActiveTouchCount()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            int count = 0;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var phase = Input.GetTouch(i).phase;
                if (phase != TouchPhase.Ended && phase != TouchPhase.Canceled)
                    count++;
            }
            return count;
#elif ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM
            var screen = Touchscreen.current;
            if (screen == null) return 0;

            int count = 0;
            var touches = screen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var phase = touches[i].phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
                    phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                    count++;
            }
            return count;
#else
            return 0;
#endif
        }

        public static bool WasKeyPressedThisFrame(TerminalShortcutKey key)
        {
            if (key == TerminalShortcutKey.None)
                return false;

#if ENABLE_LEGACY_INPUT_MANAGER
            var code = ToKeyCode(key);
            return code != KeyCode.None && Input.GetKeyDown(code);
#elif ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var control = ToControl(keyboard, key);
            return control != null && control.wasPressedThisFrame;
#else
            return false;
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private static KeyCode ToKeyCode(TerminalShortcutKey key)
        {
            switch (key)
            {
                case TerminalShortcutKey.F1: return KeyCode.F1;
                case TerminalShortcutKey.F2: return KeyCode.F2;
                case TerminalShortcutKey.F3: return KeyCode.F3;
                case TerminalShortcutKey.F4: return KeyCode.F4;
                case TerminalShortcutKey.F9: return KeyCode.F9;
                case TerminalShortcutKey.F10: return KeyCode.F10;
                case TerminalShortcutKey.F11: return KeyCode.F11;
                case TerminalShortcutKey.F12: return KeyCode.F12;
                case TerminalShortcutKey.BackQuote: return KeyCode.BackQuote;
                case TerminalShortcutKey.Escape: return KeyCode.Escape;
                default: return KeyCode.None;
            }
        }
#elif ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM
        private static ButtonControl ToControl(Keyboard keyboard, TerminalShortcutKey key)
        {
            switch (key)
            {
                case TerminalShortcutKey.F1: return keyboard.f1Key;
                case TerminalShortcutKey.F2: return keyboard.f2Key;
                case TerminalShortcutKey.F3: return keyboard.f3Key;
                case TerminalShortcutKey.F4: return keyboard.f4Key;
                case TerminalShortcutKey.F9: return keyboard.f9Key;
                case TerminalShortcutKey.F10: return keyboard.f10Key;
                case TerminalShortcutKey.F11: return keyboard.f11Key;
                case TerminalShortcutKey.F12: return keyboard.f12Key;
                case TerminalShortcutKey.BackQuote: return keyboard.backquoteKey;
                case TerminalShortcutKey.Escape: return keyboard.escapeKey;
                default: return null;
            }
        }
#endif
    }
}
