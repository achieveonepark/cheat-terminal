using UnityEditor;
using UnityEngine;

namespace Achieve.CheatTerminal.Editor
{
    /// <summary>
    /// Editor entry points for the terminal. Multi-finger taps are unusable with a mouse, so
    /// play mode gets menu items next to the F1 / F10 / F9 keyboard shortcuts - useful exactly
    /// when those keys cannot reach the game, because another editor window has focus.
    /// Deliberately without menu hotkeys: they would fire on top of the runtime keys and cancel
    /// them out. Nothing here runs outside play mode; the terminal is a runtime object.
    /// </summary>
    internal static class CheatTerminalMenu
    {
        private const string Root = "Tools/Cheat Terminal/";
        private const string PauseItem = Root + "Pause Game While Open";

        [MenuItem(Root + "Toggle Console", false, 0)]
        private static void ToggleConsole() => TerminalBehaviour.Toggle();

        [MenuItem(Root + "Toggle Console", true, 0)]
        private static bool ToggleConsoleValidate() => Application.isPlaying;

        [MenuItem(Root + "Toggle Cheat HUD", false, 1)]
        private static void ToggleCheatHud() => TerminalBehaviour.ToggleCheatHud();

        [MenuItem(Root + "Toggle Cheat HUD", true, 1)]
        private static bool ToggleCheatHudValidate() => Application.isPlaying;

        [MenuItem(Root + "Toggle Corner Handle", false, 2)]
        private static void ToggleHandle() => TerminalBehaviour.ToggleHandle();

        [MenuItem(Root + "Toggle Corner Handle", true, 2)]
        private static bool ToggleHandleValidate() => Application.isPlaying;

        [MenuItem(PauseItem, false, 20)]
        private static void TogglePause()
            => TerminalBehaviour.PauseGameWhileOpen = !TerminalBehaviour.PauseGameWhileOpen;

        [MenuItem(PauseItem, true, 20)]
        private static bool TogglePauseValidate()
        {
            bool playing = Application.isPlaying;
            Menu.SetChecked(PauseItem, playing && TerminalBehaviour.PauseGameWhileOpen);
            return playing;
        }

        /// <summary>
        /// Release builds skip the automatic bootstrap; this is the same opt-in call, so the
        /// terminal can be brought up by hand while testing that path in the editor.
        /// </summary>
        [MenuItem(Root + "Bootstrap Now", false, 40)]
        private static void BootstrapNow() => TerminalBehaviour.Bootstrap();

        [MenuItem(Root + "Bootstrap Now", true, 40)]
        private static bool BootstrapNowValidate() => Application.isPlaying;
    }
}
