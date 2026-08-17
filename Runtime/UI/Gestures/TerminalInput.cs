using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Achieve.CheatTerminal.UI
{
    /// <summary>Keyboard shortcuts offered as a desktop/editor fallback for touch gestures.</summary>
    public enum TerminalShortcutKey
    {
        None = 0,
        F9,
        F10
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
            return Input.GetKeyDown(key == TerminalShortcutKey.F9 ? KeyCode.F9 : KeyCode.F10);
#elif ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var control = key == TerminalShortcutKey.F9 ? keyboard.f9Key : keyboard.f10Key;
            return control != null && control.wasPressedThisFrame;
#else
            return false;
#endif
        }
    }
}
