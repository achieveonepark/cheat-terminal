---
sidebar_position: 5
title: 更新日志
---

# 更新日志

这里记录包的主要变更。本项目遵循 [Semantic Versioning](https://semver.org/)。

## Unreleased

## 1.0.0 - 2026-05-29

### Changed (breaking)

- 移除了内部协作接口 (`ICommandRegistry`, `ICommandParser`, `ICommandHistory`, `IAutoCompleteProvider`, `IAliasResolver`, `IArgumentConverter`, `ITerminalTrigger`, `ITerminal`) 和 `TerminalBuilder`。核心 `Terminal` 现在是一个拥有具体协作者的具体类。真正保留的扩展点只有 `ICommand`, `ICommandOutput`, `ITerminalView`, `ITerminalModule`。
- 不再使用命令 marker interface，`[Terminal]` 属性是唯一 marker。
- `Terminal.ScanStaticCommands()` 会扫描用户程序集并自动注册所有 static `[Terminal]` 方法。bootstrap 会在启动时通过 `TerminalBehaviour.AutoScanStaticCommands` 执行它。实例命令仍然使用 `Register(this)`。

### Added

- Runtime Logs 模块: 将 `Debug.Log` 输出收集到环形缓冲区，并通过 `logs [n | error | warning | info | <text> | find <text> | clear | export]` 操作。export 会写入 `Application.persistentDataPath`，方便从设备取回日志。
- 输入框支持用 Up/Down 方向键召回上一条/下一条命令历史。它通过 uGUI `IMoveHandler` 实现，因此兼容 legacy Input Manager 和 Input System package。
- `help <category>` 会列出某个分类中的命令及说明。`help` 现在会提示 `help <command>` / `help <category>`。

### Fixed

- 当 Input System package 启用时，EventSystem 会通过反射创建 `InputSystemUIInputModule`，避免每帧出现 `InvalidOperationException`。
- 右上角触发器现在是可见的 `>_` 手柄，并且单次点击即可打开。

## 0.1.0 - 2026-05-29

### Added

- 核心命令系统: `ICommandRegistry`, 支持引号的 `ICommandParser`, `ICommandHistory`, `IAutoCompleteProvider`, `IAliasResolver`, `IArgumentConverter`, 以及带默认实现的 `ICommandOutput`。
- `[Terminal("name {0}")]` 属性和基于反射的 `AttributeCommand` binding，包括位置 binding、optional 参数和 `CommandContext` 注入。
- 通过 `TerminalBuilder` 组装的 interface-driven `Terminal` 核心。
- uGUI overlay view (`UGuiTerminalView`) 在关闭时停用，实现零空闲成本。
- 右上角打开手势 (`TerminalCornerTrigger`，默认 double-tap)。
- 带 static convenience API 的 `TerminalBehaviour` bootstrap，在编辑器和开发构建中自动 bootstrap。
- 内置命令: `help`, `clear`, `history`, `echo`, `alias`。
- 模块系统 (`ITerminalModule`)，在 bootstrap 上提供 `InstallModule` / `GetModule<T>`。
- Scene Tools 模块: `scene list | load <name> [additive] | unload <name>`。
- Object Inspector 模块: `find`, `inspect`, `set <name>.<member> <value>`, `call`，并支持显式对象注册 (`ObjectInspectorModule.RegisterObject`)。
- Performance Monitor 模块: `perf` (FPS、frame ms、memory、GC，以及通过 `ProfilerRecorder` 获取的 render stats)。
- Basic Usage sample。
