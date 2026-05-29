---
sidebar_position: 4
title: Command Reference
---

# Command Reference

## Built-in commands

| Command | Description |
| --- | --- |
| `help [command\|category]` | Command list / details |
| `clear` | Clear output |
| `history` | Input history |
| `echo <text>` | Print text |
| `alias <name> <expansion>` | Register an alias (`alias remove <name>`, `alias` to list) |

## Modules

The default bootstrap automatically installs these modules.

### Scene

```bash
scene list                 # Loaded scenes + build setting scenes
scene load Lobby           # Single load
scene load InGame additive # Additive load
scene unload InGame
```

### Inspector

Reflection-based runtime object exploration.

```bash
find Player                 # Search GameObjects by name
inspect Player              # Component field/property tree
set Player.HP 99999         # Change a member value
call Player.Respawn         # Invoke a method
```

To expose non-GameObject values, such as services, by name:

```csharp
TerminalBehaviour.Instance.GetModule<ObjectInspectorModule>()
    .RegisterObject("Player", playerService);
```

### Performance

```bash
perf   # FPS / frame ms / memory / Mono / GC / draw calls, batches, triangles when available
```

### Logs

Captures `Debug.Log` output into a capped ring buffer.

```bash
logs                 # Latest 30
logs 100             # Latest 100
logs error           # Level filter (error / warning / info)
logs network         # Text contains filter
logs find <text>     # Explicit search
logs clear           # Clear
logs export          # Save to file (persistentDataPath, retrievable from device)
```

## Aliases and macros

Create aliases for frequent command combinations:

```bash
alias rich gold 999999
rich            # -> gold 999999
```
