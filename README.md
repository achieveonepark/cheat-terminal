# Cheat Terminal

[한국어](README.ko.md)

A Unity runtime developer console for cheats, debugging, logs, and live game control.
Register commands explicitly with `RegisterCommand(...)`; no reflection or assembly scanning is required.
The uGUI console and UI Toolkit cheat library are built entirely in code for Unity 6.

## Installation

In Unity Package Manager, select **Add package from git URL**:

```text
https://github.com/achieveonepark/cheat-terminal.git
```

## Opening the terminal and cheat library

The terminal starts automatically in the editor and development builds. In release builds,
call `TerminalBehaviour.Bootstrap()` once to enable it.

The top-right developer dock is visible by default. The sliders icon opens Cheat Library;
the terminal icon opens Terminal. Drag the grip or either icon to move the dock within the safe area.

| Gesture | Action | Editor / desktop |
| --- | --- | --- |
| Tap with four fingers, three times | Show / hide the dock | `F9` |
| Tap with three fingers, three times | Open / close Cheat Library | `F1`, `F10` |
| Tap the dock's terminal icon | Open / close Terminal | `F2` |

Backquote (`` ` ``) opens Terminal. Escape closes the current surface.
Use **Tools > Cheat Terminal** when the editor Game view does not have focus.

- **Enter / Run:** execute a command.
- **Tab:** accept the first completion suggestion.
- **Up / Down:** navigate command history.
- **help:** list commands; `help <category>` and `help <command>` show details.
- **Console / Commands / Data:** browse output, registered commands, and data tables.

## Cheat Library

Every registered command appears automatically in the full-screen UI Toolkit cheat library.
No separate HUD registration is required.

- Browse by category or search by name, description, and category.
- Commands without arguments have a **Run** button.
- Commands with arguments show named parameter fields. Enter values and press **Run** or **Enter**;
  you do not need to type the command name.
- Required values use `<amount>`, optional values use `[amount]`, and `<on|off>` adds choice buttons.
- Input values survive filtering, resizing, and reopening.
- Touch controls respect `Screen.safeArea`; wide screens use two or three columns.
- Results and errors appear on the command card, in the status bar, and in terminal output.

```csharp
TerminalBehaviour.ToggleCheatHud();           // Toggle Cheat Library
TerminalBehaviour.HandleVisible = true;      // Show the developer dock
TerminalBehaviour.CheatHudFullScreen = false; // Use the narrow sliding panel
```

### Visibility and pausing

Subscribe to visibility changes to pause or slow gameplay while either surface is open:

```csharp
// true while either surface is open; false once both are closed
TerminalBehaviour.VisibilityChanged += open => Time.timeScale = open ? 0.2f : 1f;

// Alternatively, save the current time scale and restore it on close.
TerminalBehaviour.PauseGameWhileOpen = true;
TerminalBehaviour.PausedTimeScale = 0f;
```

## Adding commands

Register commands directly from the object that owns the game state.
Commands execute through a `CommandContext` delegate without reflection.

```csharp
using System.Globalization;
using Achieve.CheatTerminal;
using UnityEngine;

public class PlayerCheats : MonoBehaviour
{
    private int _gold;

    private void Start()
    {
        TerminalBehaviour.RegisterCommand(
            "gold",
            AddGold,
            "Add gold",
            "Cheats",
            "gold <amount>");

        TerminalBehaviour.RegisterCommand(
            "god",
            ctx => ctx.Output.WriteLine("god toggled", LogLevel.Success),
            "Toggle god mode",
            "Cheats",
            "god");
    }

    private void AddGold(CommandContext ctx)
    {
        if (!ctx.Has(0) ||
            !int.TryParse(ctx.Args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
        {
            ctx.Output.WriteLine("Usage: gold <amount>", LogLevel.Error);
            return;
        }

        _gold += amount;
        ctx.Output.WriteLine($"Gold is now {_gold}", LogLevel.Success);
    }
}
```

Run `gold 100000` or `god` in Terminal, or use their Run buttons in Cheat Library.
`help Cheats` lists the commands in that category with descriptions.

### Data tables

Registered game data is available through the `data` command, completion suggestions,
and the terminal's **Data** tab.

```csharp
TerminalBehaviour.RegisterDataTable("items", "Items", () => new[]
{
    new DataTableRow("sword_001", "Bronze Sword"),
    new DataTableRow("potion_hp", "Health Potion"),
});
```

- `data`: list table IDs, names, and row counts.
- `data items`: list row IDs and names in the table.
- `data items sword`: search IDs, names, and summaries, then display matching fields.

## Built-in commands and modules

- **Core:** `help`, `clear`, `history`, `echo`, `alias`.
- **Scene:** `scene list | load <name> [additive] | unload <name>`.
- **Data:** `data [table] [id|text]`.
- **Performance:** `perf` for FPS, memory, GC, and rendering statistics.
- **Logs:** `logs [n | error | warning | info | <text> | find <text> | clear | export]`.
- **Components:** control Unity's built-in components directly.

### Unity component commands

| Command | Purpose | Example |
| --- | --- | --- |
| `transform pos\|rot\|scale\|reset <name> [x y z]` | Edit transforms | `transform pos Player 0 5 0` |
| `rb velocity\|gravity\|kinematic\|mass\|drag <name> [args]` | Control rigidbodies | `rb gravity Player off` |
| `cam fov\|bg\|ortho\|size\|clip [args]` | Configure cameras | `cam fov 90` |
| `light intensity\|color\|range\|shadow <name> [args]` | Configure lights | `light intensity Sun 2` |
| `audio volume\|mute\|pause\|resume [args]` | Control audio | `audio volume 0.5` |
| `time scale\|fixed [value]` | Change time settings | `time scale 0` |
| `go list\|active\|tag [name] [args]` | Inspect and control GameObjects | `go active Enemy off` |

Run `help Components` for the full list, or see the [component command guide](https://docs.somiri.dev/cheat-terminal/en/usage/unity-components/).

## Documentation

- [Documentation site](https://docs.somiri.dev/cheat-terminal/en/): installation, usage, command reference, and Cheat Library.
- [Changelog](CHANGELOG.md): changes in each version.
- [Project history](https://docs.somiri.dev/cheat-terminal/en/history/): design changes and upgrade notes.
- [AGENT.md](AGENT.md): repository conventions for authoring cheats.

## License

[MIT License](LICENSE.md). Copyright (c) 2026 achieveonepark.
