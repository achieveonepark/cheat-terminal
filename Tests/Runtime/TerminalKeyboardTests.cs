#if ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using System.Collections;
using Achieve.CheatTerminal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Achieve.CheatTerminal.Tests
{
    public sealed class TerminalKeyboardTests
    {
        private Keyboard _keyboard;
        private InputSettings.BackgroundBehavior _backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInputBehavior;

        [SetUp]
        public void Setup()
        {
            // Synthetic keyboard events must reach the player even when the batch editor
            // has no focused Game view. Restore the user's settings after each test.
            _backgroundBehavior = InputSystem.settings.backgroundBehavior;
            _editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShortcutsCompletionHistoryAndEnterUseInputSystem()
        {
            var host = TerminalBehaviour.Bootstrap();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            int calls = 0;
            host.Terminal.RegisterCommand("gold", _ => calls++, usage: "gold <amount>");
            yield return null;
            yield return Press(Key.F1);
            Assert.That(host.CheatHud.IsOpen, Is.True);
            yield return Press(Key.F2);
            Assert.That(host.View.IsOpen, Is.True);
            Assert.That(host.CheatHud.IsOpen, Is.False);
            var input = host.GetComponentInChildren<InputField>();
            input.text = "gol";
            yield return Press(Key.Tab);
            Assert.That(input.text, Is.EqualTo("gold "));
            input.text = "gold 25";
            yield return Press(Key.Enter);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(input.text, Is.Empty);
            yield return Press(Key.UpArrow);
            Assert.That(input.text, Is.EqualTo("gold 25"));
            yield return Press(Key.DownArrow);
            Assert.That(input.text, Is.Empty);
            input.text = "draft";
            yield return Press(Key.F2);
            Assert.That(host.View.IsOpen, Is.False);
            yield return Press(Key.F2);
            Assert.That(input.text, Is.EqualTo("draft"));
            yield return Press(Key.Escape);
            Assert.That(host.View.IsOpen, Is.False);
            yield return Press(Key.F9);
            Assert.That(host.Trigger.Visible, Is.False);
            yield return Press(Key.F9);
            Assert.That(host.Trigger.Visible, Is.True);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings.backgroundBehavior = _backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInputBehavior;
            if (TerminalBehaviour.Instance != null) Object.Destroy(TerminalBehaviour.Instance.gameObject);
            if (UnityEngine.EventSystems.EventSystem.current != null) Object.Destroy(UnityEngine.EventSystems.EventSystem.current.gameObject);
            yield return null;
        }
    }
}
#endif
