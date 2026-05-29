---
sidebar_position: 5
title: Unity Component Commands
---

# Unity Component Commands

`UnityComponentsModule` provides seven commands covering the most frequently used Unity built-in components, all controllable from the terminal without touching any code.  
The module is installed automatically in editor and development builds — no extra setup required.

---

## `transform` — Transform manipulation

```bash
transform get   <name>                   # Print position, rotation, and scale
transform pos   <name> [x y z]           # Get or set world position
transform rot   <name> [x y z]           # Get or set euler rotation
transform scale <name> [x y z]           # Get or set local scale
transform reset <name>                   # Reset position, rotation, and scale to identity
```

### Examples

```bash
transform get   Player
transform pos   Player 0 1 0
transform rot   Enemy  0 90 0
transform scale Boss   2 2 2
transform reset Cube
```

:::info
Omitting the `x y z` arguments prints the current value without modifying it.
:::

---

## `rb` — Rigidbody control

```bash
rb get       <name>           # Print all Rigidbody properties
rb velocity  <name> [x y z]  # Get or set linear velocity
rb gravity   <name> [on|off] # Get or set useGravity
rb kinematic <name> [on|off] # Get or set isKinematic
rb mass      <name> [value]  # Get or set mass
rb drag      <name> [value]  # Get or set linear damping (drag)
```

### Examples

```bash
rb get       Player
rb velocity  Player 0 10 0
rb gravity   Player off
rb kinematic Player on
rb mass      Boulder 50
rb drag      Ship 0.1
```

:::note
An error is shown when the target GameObject has no `Rigidbody` component.
:::

---

## `cam` — Camera settings

Always targets `Camera.main` unless otherwise noted.

```bash
cam list                          # List all cameras in the scene
cam fov  [value]                  # Get or set fieldOfView (1–179°)
cam bg   [r g b [a]]              # Get or set backgroundColor (0–1 range)
cam ortho [on|off]                # Toggle perspective / orthographic
cam size  [value]                 # Get or set orthographicSize
cam clip  [near far]              # Get or set near and far clip planes
```

### Examples

```bash
cam list
cam fov 75
cam bg 0.1 0.1 0.2
cam ortho on
cam size 5
cam clip 0.01 1000
```

---

## `light` — Light control

```bash
light list                        # List all Lights in the scene
light intensity <name> [value]    # Get or set intensity
light color     <name> [r g b]    # Get or set colour (0–1 range)
light range     <name> [value]    # Get or set range
light shadow    <name> [on|off]   # Toggle soft shadows on or off
```

### Examples

```bash
light list
light intensity Sun 1.5
light color     Sun 1 0.9 0.8
light range     PointLight 10
light shadow    Sun off
```

---

## `audio` — AudioListener control

Affects the global `AudioListener`.

```bash
audio volume [value]   # Get or set master volume (0–1)
audio mute   [on|off]  # Mute or unmute audio
audio pause            # Pause all audio
audio resume           # Resume all audio
```

### Examples

```bash
audio volume 0.5
audio mute on
audio pause
audio resume
```

---

## `time` — Time control

```bash
time get            # Print timeScale, fixedDeltaTime, time, frameCount
time scale [value]  # Get or set Time.timeScale (≥ 0)
time fixed [value]  # Get or set Time.fixedDeltaTime
```

### Examples

```bash
time get
time scale 0.5     # Slow motion
time scale 0       # Pause
time scale 1       # Resume normal speed
time fixed 0.02    # 50 Hz physics updates
```

:::tip
Pause with `time scale 0`, inspect state via the terminal, then resume with `time scale 1`.
:::

---

## `go` — GameObject utilities

```bash
go list   [tag]             # List GameObjects (up to 40, optional tag filter)
go active <name> <on|off>   # Call SetActive
go tag    <name> [tag]      # Get or set the tag
```

### Examples

```bash
go list
go list Player              # Only objects tagged "Player"
go active Enemy on
go active Canvas off
go tag Player Untagged
```

:::note
`go list` shows at most 40 objects from `FindObjectsByType`.
:::

---

## Value format reference

| Type | Accepted formats |
| --- | --- |
| float | `1`, `1.5`, `-0.3` |
| bool | `on` / `off` / `true` / `false` / `1` / `0` |
| Vector3 (x y z) | Space-separated: `0 1 0` |
| Color (r g b) | Float 0–1: `1 0.5 0` |

---

## Category

All commands belong to the `Components` category. Run `help Components` to list them.
