using System;
using System.Collections;
using System.IO;
using Achieve.CheatTerminal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Achieve.CheatTerminal.Tests
{
    public sealed class TerminalUiTests
    {
        private static void Click(UnityEngine.UIElements.Button button)
        {
            Assert.That(button.enabledInHierarchy, Is.True);
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        [UnityTest]
        public IEnumerator ParameterFormsDockAndTerminalWorkInPlayMode()
        {
            var backdrop = new GameObject("Test backdrop", typeof(Camera));
            var camera = backdrop.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .06f, .09f);
            yield return null;
            var host = TerminalBehaviour.Bootstrap();
            Assert.That(host.ConsoleKey, Is.EqualTo(TerminalShortcutKey.F2));
            Assert.That(host.CheatHudKey, Is.EqualTo(TerminalShortcutKey.F1));
            Assert.That(host.Trigger.Visible, Is.True);
            Assert.That(EventSystem.current, Is.Not.Null);
            int gold = 0;
            TerminalBehaviour.RegisterCommand("gold", ctx =>
            {
                gold += ctx.GetInt(0);
                ctx.Output.WriteLine("Gold: " + gold, LogLevel.Success);
            }, "Add gold to the player", "Cheats", "gold <amount>");
            TerminalBehaviour.RegisterCommand("teleport", _ => { }, "Move the player", "Cheats", "teleport <x> <y> <z>");
            TerminalBehaviour.RegisterCommand("heal", ctx => ctx.Output.WriteLine("Healed " + ctx.GetInt(0, 100)), "Restore health", "Cheats", "heal [amount]");
            TerminalBehaviour.RegisterCommand("fail", ctx =>
            {
                ctx.Output.WriteLine("Not enough energy", LogLevel.Error);
                ctx.Output.WriteLine("Finished");
            }, "Verify error feedback", "Cheats", "fail");
            TerminalBehaviour.OpenCheatHud();
            for (int i = 0; i < 8; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var graphic in host.Trigger.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                if (graphic is UnityEngine.UI.Text) continue;
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name);
                var mesh = graphic.canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null, graphic.name);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0), graphic.name);
            }
            var root = host.GetComponent<UIDocument>().rootVisualElement;
            var card = root.Q("command-gold");
            Assert.That(card, Is.Not.Null);
            var amountField = card.Q<TextField>("parameter-0");
            // Panel scaling rounds to device pixels, especially in the small batch Game view.
            Assert.That(amountField.layout.height, Is.GreaterThanOrEqualTo(100f));
            Assert.That(amountField.worldBound.yMax, Is.LessThanOrEqualTo(card.Q<UnityEngine.UIElements.Button>("run").worldBound.yMin));
            Assert.That(card.Q<UnityEngine.UIElements.Button>("run").enabledSelf, Is.False);
            card.Q<TextField>("parameter-0").value = "250";
            Click(card.Q<UnityEngine.UIElements.Button>("run"));
            Assert.That(gold, Is.EqualTo(250));
            Assert.That(card.Q<Label>("result").text, Is.EqualTo("Gold: 250"));
            Click(root.Q("command-heal").Q<UnityEngine.UIElements.Button>("run"));
            Assert.That(root.Q("command-heal").Q<Label>("result").text, Is.EqualTo("Healed 100"));
            Click(root.Q("command-fail").Q<UnityEngine.UIElements.Button>("run"));
            Assert.That(root.Q("command-fail").Q<Label>("result").text, Is.EqualTo("Not enough energy"));

            var search = root.Q<TextField>("cheat-search");
            search.value = "teleport";
            yield return null;
            Assert.That(root.Q("command-gold"), Is.Null);
            Assert.That(root.Q("command-teleport"), Is.Not.Null);
            search.value = "";
            yield return null;
            Assert.That(root.Q("command-gold").Q<TextField>("parameter-0").value, Is.EqualTo("250"));
            search.value = "Cheats";
            for (int i = 0; i < 4; i++) yield return null;
            yield return Capture("cheats.png");

            TerminalBehaviour.Open();
            for (int i = 0; i < 4; i++) yield return null;
            Assert.That(host.View.IsOpen, Is.True);
            Assert.That(host.CheatHud.IsOpen, Is.False);
            var field = host.GetComponentInChildren<UnityEngine.UI.InputField>();
            field.text = "gold 25";
            var buttons = host.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach (var button in buttons)
                if (button.name == "Run") button.onClick.Invoke();
            Assert.That(gold, Is.EqualTo(275));
            Assert.That(field.text, Is.Empty);
            yield return Capture("terminal.png");
            TerminalBehaviour.Close();

            // The drag handler is on the host, so it receives drag events from both dock buttons.
            var dock = host.transform.Find("Achieve.CheatTerminalTriggerCanvas/DeveloperDock").GetComponent<RectTransform>();
            Vector2 before = dock.anchoredPosition;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width - 30, Screen.height - 30),
                pointerPressRaycast = new RaycastResult { gameObject = dock.gameObject }
            };
            host.Trigger.OnBeginDrag(pointer);
            pointer.position -= new Vector2(180, 100);
            host.Trigger.OnDrag(pointer);
            host.Trigger.OnEndDrag(pointer);
            Assert.That(dock.anchoredPosition, Is.Not.EqualTo(before));
            Assert.That(host.View.IsOpen, Is.False);
            Assert.That(host.CheatHud.IsOpen, Is.False);
            Assert.That(pointer.eligibleForClick, Is.False);
            host.Trigger.Visible = false;
            host.Trigger.Visible = true;
            Assert.That(dock.anchoredPosition, Is.Not.EqualTo(before));
            yield return Capture("dock.png");

            TerminalBehaviour.PauseGameWhileOpen = true;
            TerminalBehaviour.OpenCheatHud();
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            TerminalBehaviour.Open();
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            TerminalBehaviour.Close();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            TerminalBehaviour.PauseGameWhileOpen = false;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var backdrop = GameObject.Find("Test backdrop");
            if (backdrop != null) UnityEngine.Object.Destroy(backdrop);
            if (TerminalBehaviour.Instance != null) UnityEngine.Object.Destroy(TerminalBehaviour.Instance.gameObject);
            if (EventSystem.current != null) UnityEngine.Object.Destroy(EventSystem.current.gameObject);
            yield return null;
        }

        private static IEnumerator Capture(string name)
        {
            string directory = Environment.GetEnvironmentVariable("CHEAT_TERMINAL_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) yield break;
            Directory.CreateDirectory(directory);
            // Wait for the unscaled open/close transition before capturing a settled frame.
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name));
            for (int i = 0; i < 5; i++) yield return null;
        }
    }
}
