using UniTerminal;
using UnityEngine;

namespace UniTerminalSamples
{
    /// <summary>
    /// Drop this on any GameObject. It registers its [Terminal] methods with the
    /// terminal, then you can open the console (double-tap the top-right corner)
    /// and run: gold 100000 / level 99 / god / pos 0 5 0 / heal
    /// </summary>
    public sealed class ExampleCheats : MonoBehaviour
    {
        private int _gold;
        private int _level = 1;
        private bool _godMode;

        private void Start()
        {
            // Scan this instance for [Terminal] methods and register them.
            TerminalBehaviour.Register(this);
        }

        [Terminal("gold {0}", Description = "Add gold", Category = "Cheats")]
        public void AddGold(int amount)
        {
            _gold += amount;
            Debug.Log($"Gold is now {_gold}");
        }

        [Terminal("level {0}", Description = "Set player level", Category = "Cheats")]
        public void SetLevel(int level, CommandContext ctx)
        {
            _level = level;
            ctx.Output.WriteLine($"Level set to {_level}", LogLevel.Success);
        }

        [Terminal("god", Description = "Toggle god mode", Category = "Cheats")]
        public void ToggleGod(CommandContext ctx)
        {
            _godMode = !_godMode;
            ctx.Output.WriteLine($"God mode: {(_godMode ? "ON" : "OFF")}",
                _godMode ? LogLevel.Success : LogLevel.Warning);
        }

        [Terminal("pos {0} {1} {2}", Description = "Teleport player", Category = "Cheats")]
        public void Teleport(float x, float y, float z)
        {
            transform.position = new Vector3(x, y, z);
        }

        // No placeholders + optional parameter: "heal" or "heal 50".
        [Terminal("heal", Description = "Heal (default 100)", Category = "Cheats")]
        public string Heal(int amount = 100)
        {
            return $"Healed {amount} HP";
        }
    }
}
