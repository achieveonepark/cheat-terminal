# Changelog

> 🌐 [한국어](CHANGELOG.ko.md)

All notable changes to this package are documented here.
This project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
