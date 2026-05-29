# UniTerminal

A runtime developer terminal for Unity — cheats, debugging and live inspection.
Built to be **interface-driven** (swap any part) and **attribute-based** (annotate a
method and it becomes a command). The default UI is a uGUI overlay that costs nothing
while closed.

> Status: **Core** is implemented (command system, attribute binding, interface DI,
> uGUI view, top-right corner trigger). Modules (Inspector, Perf, Scene, Logs, …) are
> on the roadmap below.

## Install

Unity Package Manager → *Add package from git URL*:

```
https://github.com/achieveonepark/cheat-terminal.git?path=/
```

Requires Unity 6 (6000.x) and `com.unity.ugui` (default).

## Quick start

In the editor / development builds the terminal auto-bootstraps. **Double-tap the
top-right corner** of the screen to open it. Then register your cheats:

```csharp
using UniTerminal;
using UnityEngine;

public class PlayerCheats : MonoBehaviour
{
    private int _gold;

    void Start() => TerminalBehaviour.Register(this); // scans [Terminal] methods

    [Terminal("gold {0}", Description = "Add gold", Category = "Cheats")]
    public void AddGold(int amount) => _gold += amount;

    [Terminal("god", Description = "Toggle god mode")]
    public void God(CommandContext ctx) => ctx.Output.WriteLine("god toggled");
}
```

Now `gold 100000`, `god`, `help`, `clear`, `history`, `alias` all work.

### Attribute template rules

- The first token is the command name: `"gold {0}"` → command `gold`.
- `{0} {1} …` bind supplied args to method parameters **by index**.
- With no placeholders, args bind **positionally** to the parameters.
- A `CommandContext` parameter is injected automatically and never consumes an arg.
- Optional parameters become optional args: `[Terminal("heal")] string Heal(int n = 100)`.

Supported argument types: `string`, `bool`, `int/long/short/byte`, `float/double`,
`enum`, `Vector2/3/4`, `Color` (e.g. `pos "1,2,3"` or `pos 1 2 3`).

## Architecture

Everything is an interface injected into the core `Terminal`:

| Interface | Default | Responsibility |
|---|---|---|
| `ICommandRegistry` | `CommandRegistry` | store / resolve commands |
| `ICommandParser` | `CommandParser` | tokenize input (quotes aware) |
| `ICommandHistory` | `CommandHistory` | up/down history |
| `IAutoCompleteProvider` | `AutoCompleteProvider` | name completion |
| `IAliasResolver` | `AliasResolver` | alias expansion |
| `IArgumentConverter` | `ArgumentConverter` | string → typed args |
| `ICommandOutput` | `BufferedOutput` | output sink (capped ring) |
| `ITerminalView` | `UGuiTerminalView` | presentation (uGUI overlay) |
| `ITerminalTrigger` | `TerminalCornerTrigger` | open gesture |

Build a custom terminal and plug in your own pieces:

```csharp
var terminal = new TerminalBuilder()
    .WithOutput(myOutput)          // any ICommandOutput
    .WithParser(myParser)          // any ICommandParser
    .Build();

// The view is a runtime component, so it is attached (not built):
terminal.AttachView(myView);       // e.g. a native iOS/Android view later
```

Performance notes: the view canvas is fully **deactivated** while closed (no draw
calls, no raycasts); output is buffered into a capped ring and the text mesh is only
rebuilt on a dirty frame while open; command registration uses targeted reflection on
the registered instance/type only — there is no whole-domain scan.

## Modules

Modules implement `ITerminalModule` and register their own commands. Install/replace
them on the bootstrap:

```csharp
TerminalBehaviour.Instance.InstallModule(new MyModule());
var inspector = TerminalBehaviour.Instance.GetModule<ObjectInspectorModule>();
inspector.RegisterObject("Player", playerService); // expose a non-GameObject by name
```

Shipped:

- **Scene Tools** — `scene list | load <name> [additive] | unload <name>`
- **Object Inspector** — `find <name>`, `inspect <name>`, `set <name>.<member> <value>`,
  `call <name>.<method> [args]`
- **Performance Monitor** — `perf` (FPS, frame ms, memory, GC, render stats)

### Roadmap

Runtime Logs · Event Viewer · Save Data Tools · Network Tools · DI Container Tools ·
Macro System · Script Runner · Remote Terminal · AI Assistant · Dashboard.

Each ships as an optional module injected into the core — nothing forces you to take
what you don't use.
