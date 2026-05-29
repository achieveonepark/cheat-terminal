# Changelog

All notable changes to this package are documented here.
This project adheres to [Semantic Versioning](https://semver.org/).

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
