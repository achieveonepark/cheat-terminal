# Changelog

> 🌐 [한국어](CHANGELOG.ko.md)

All notable changes to this package are documented here.
This project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.3.0] - 2026-08-29

### Changed
- The cheat HUD covers the whole screen instead of sliding in from the left edge. Rows are
  sized as touch targets (104px minimum, larger text), content is inset by `Screen.safeArea`
  so notches never cover a row, wide screens split the list into two or three columns, and the
  bottom status bar carries a large `CLOSE` button for tall phones. The old panel is still
  there: `CheatHudView.FullScreen = false` (or `TerminalBehaviour.CheatHudFullScreen`) with
  `CheatHudView.WidthPercent` for its width.

### Added
- Open/close listeners. `TerminalBehaviour.VisibilityChanged` fires with true when the console
  or the cheat HUD opens and false once both are closed; `ConsoleVisibilityChanged` and
  `CheatHudVisibilityChanged` report one surface each (instance events `OnVisibilityChanged`,
  `OnConsoleVisibilityChanged`, `OnCheatHudVisibilityChanged`). `CheatHudView` and
  `UGuiTerminalView` raise `OnOpened` / `OnClosed` / `OnVisibilityChanged` directly.
- `TerminalBehaviour.PauseGameWhileOpen` and `PausedTimeScale`: opt-in convenience over those
  listeners that holds `Time.timeScale` while a terminal surface is open and restores the
  previous value on close.
- Console keyboard shortcuts for the editor and desktop: `F1` toggles the console and the
  backquote (`` ` ``) opens it. The backquote never closes it, so it does not eat a keystroke
  while a command is being typed. Both are configurable through `TerminalBehaviour.ConsoleKey`
  and `ConsoleOpenKey`. The cheat HUD also answers to `F2` next to `F10`.
- `Tools > Cheat Terminal` editor menu (play mode): toggle the console, the cheat HUD or the
  corner handle, flip "Pause Game While Open", or bootstrap the terminal by hand. Useful
  exactly when the Game view has no focus and the keys cannot reach the game.
- `TerminalShortcutKey` gained `F1`-`F4`, `F11`, `F12`, `BackQuote` and `Escape`; existing
  values keep their numbers. `MultiFingerTapGesture` accepts a second `AlternateKey`.

### Fixed
- Input System-only projects had no `EventSystem` created for them, which left every button in
  the console and the cheat HUD dead. One is now created with `InputSystemUIInputModule`
  (referenced directly under the package version define, so this stays reflection-free).

### Documentation
- Added a Project History page reconstructed from the commit history: the three design
  turning points (reflection to explicit registration, interface zoo to concrete core,
  always-on UI to gesture-summoned UI), a per-release summary and an upgrade cheat sheet
  (ko / en / ja / zh).

## [1.2.0] - 2026-08-17

### Changed
- The top-right `>_` handle is hidden by default. Tapping with **four fingers three times**
  (unscaled time, anywhere on screen) shows it, and the same gesture hides it again.
  `TerminalCornerTrigger.Visible` / `TerminalBehaviour.HandleVisible` expose it from code
  (`TerminalCornerTrigger.Enabled` is kept as an alias).

### Added
- Cheat HUD (`CheatHudView`): a UI Toolkit panel that slides in from the left and lists every
  registered command, grouped by category. Toggled by tapping with **three fingers three times**
  (F10 on desktop), or through `TerminalBehaviour.Open/Close/ToggleCheatHud()`.
  Commands whose usage takes no arguments run on a single tap; commands with `<...>` or `[...]`
  arguments open an inline input pre-filled with the command name. Includes a search box, a
  status line for the last executed command, and a `>_` button that opens the full console.
  The list refreshes itself from `Terminal.Registry.Changed`.
- `MultiFingerTapGesture`: reusable "N fingers, M taps" detector that requires an exact finger
  count, so the three- and four-finger gestures never trigger each other. Optional keyboard
  fallback (F9 / F10) for the editor and desktop.
- `TerminalInput`: reflection-free input abstraction that reads the legacy Input Manager, or the
  Input System package when that is the active backend (optional dependency, resolved through
  the `com.unity.inputsystem` version define).
- `AGENT.md` with the conventions required to author cheats in this package.

## [1.1.0] - 2026-06-27

### Changed (breaking)
- Removed reflection-based `[Terminal]` method discovery and invocation. Commands are
  now registered explicitly with `RegisterCommand(...)`.
- Removed the reflection-based Object Inspector module from the default runtime.
- EventSystem fallback no longer probes optional Input System types via reflection; projects
  using Input System-only input should provide an EventSystem with `InputSystemUIInputModule`.

### Fixed
- Terminal input now executes only on an explicit submit, not on focus loss.
- Terminal output escapes rich text-sensitive characters before wrapping lines in color tags.
- Unity component commands now use invariant-culture numeric parsing, strict bool parsing,
  and inactive-inclusive GameObject lookup where appropriate.
- Runtime helper GameObjects created by Logs and Performance modules are disposed with the
  terminal bootstrap.

### Added
- Cached completion items for command names, usage keywords and command-specific providers.
- Context-aware completions for built-in commands, including scenes, logs, GameObjects,
  component targets and boolean values.
- `data [table] [id|text]` command plus `RegisterDataTable(...)` for reflection-free data lookup.
- Terminal UI tabs for Console, registered Commands and registered Data tables.

## [1.0.1] - 2026-05-30

### Changed
- 네임스페이스 `UniTerminal` → `Achieve.CheatTerminal` 로 변경.

### Added
- Unity 내장 컴포넌트(`Transform`, `Rigidbody`, `Camera` 등)에 대한 터미널 명령 지원 추가.
- `ITerminalModule` 고급 사용법 문서 섹션 추가 (ko/en/ja/zh-CN).

## [1.0.0] - 2026-05-29

### Changed (breaking)
- Removed the internal collaborator interfaces (`ICommandRegistry`, `ICommandParser`,
  `ICommandHistory`, `IAutoCompleteProvider`, `IAliasResolver`, `IArgumentConverter`,
  `ITerminalTrigger`, `ITerminal`) and `TerminalBuilder`. The core `Terminal` is now a
  concrete class that owns concrete collaborators. Only `ICommand`, `ICommandOutput`,
  `ITerminalView` and `ITerminalModule` remain as real extension points.
- No marker interface for commands — the `[Terminal]` attribute is the only marker.
- `Terminal.ScanStaticCommands()` sweeps user assemblies and auto-registers all static
  `[Terminal]` methods; the bootstrap runs it on startup
  (`TerminalBehaviour.AutoScanStaticCommands` to toggle). Instance commands still use
  `Register(this)`.

### Added
- Runtime Logs module: captures `Debug.Log` output into a ring buffer with
  `logs [n | error | warning | info | <text> | find <text> | clear | export]`.
  Export writes to `Application.persistentDataPath` so logs are retrievable on device.
- Up/Down arrow recalls previous/next command history in the input field
  (via uGUI `IMoveHandler`, so it works under both the legacy Input Manager and
  the Input System package).
- `help <category>` lists the commands in a category with descriptions;
  `help` now hints at `help <command>` / `help <category>`.

### Fixed
- EventSystem now creates `InputSystemUIInputModule` when the Input System package
  is active (resolved via reflection), avoiding the per-frame `InvalidOperationException`.
- Top-right corner trigger is now a visible `>_` handle and opens on a single tap.

## [0.1.0] - 2026-05-29

### Added
- Core command system: `ICommandRegistry`, `ICommandParser` (quote-aware),
  `ICommandHistory`, `IAutoCompleteProvider`, `IAliasResolver`, `IArgumentConverter`,
  `ICommandOutput` with default implementations.
- `[Terminal("name {0}")]` attribute and reflection-based `AttributeCommand` binding,
  including positional binding, optional parameters and `CommandContext` injection.
- Interface-driven `Terminal` core assembled via `TerminalBuilder`.
- uGUI overlay view (`UGuiTerminalView`) deactivated while closed for zero idle cost.
- Top-right corner open gesture (`TerminalCornerTrigger`, double-tap by default).
- `TerminalBehaviour` bootstrap with a static convenience API; auto-bootstraps in the
  editor and development builds.
- Built-in commands: `help`, `clear`, `history`, `echo`, `alias`.
- Module system (`ITerminalModule`) with `InstallModule` / `GetModule<T>` on the bootstrap.
- Scene Tools module: `scene list | load <name> [additive] | unload <name>`.
- Object Inspector module: `find`, `inspect`, `set <name>.<member> <value>`, `call`,
  with explicit object registration (`ObjectInspectorModule.RegisterObject`).
- Performance Monitor module: `perf` (FPS, frame ms, memory, GC, render stats via
  `ProfilerRecorder`).
- Basic Usage sample.
