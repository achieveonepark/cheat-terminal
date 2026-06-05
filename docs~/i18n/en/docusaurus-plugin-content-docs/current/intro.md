---
slug: /
sidebar_position: 1
title: Introduction
---

# Cheat Terminal

Cheat Terminal is a Unity runtime developer console for cheats, debugging, and live object inspection.

Add the `[Terminal]` attribute to a method and it becomes a command. The console runs as a uGUI overlay with **zero idle cost** while closed.

```csharp
[Terminal("gold {0}", Description = "Add gold", Category = "Cheats")]
public void AddGold(int amount) => _gold += amount;
```

## Features

- **Attribute-based commands** - Register a command with one `[Terminal("name {0}")]` attribute. Static methods are collected automatically on startup.
- **Performance first** - The canvas is disabled while closed, output uses a capped ring buffer, and arrow-key history works through `IMoveHandler`.
- **Modular** - Scene, Inspector, Performance, and Logs modules are included by default.
- **Mobile friendly** - Open from the top-right handle and export device logs to a file.

## Requirements

- Unity 6 (6000.x)
- `com.unity.ugui` (included by default)

Next: [Getting Started](./getting-started.md)
