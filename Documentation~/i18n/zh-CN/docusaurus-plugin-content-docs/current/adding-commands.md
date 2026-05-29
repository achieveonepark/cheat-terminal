---
sidebar_position: 3
title: 添加命令
---

# 添加命令

命令就是带有 `[Terminal]` 属性的方法。注册方式有两种。

## 1. 实例方法 - `Register(this)`

持有金币、等级等状态的类可以注册自身。

```csharp
using Achieve.CheatTerminal;
using UnityEngine;

public class PlayerCheats : MonoBehaviour
{
    private int _gold;

    void Start() => TerminalBehaviour.Register(this); // 自动收集 [Terminal] 方法

    [Terminal("gold {0}", Description = "添加金币", Category = "Cheats")]
    public void AddGold(int amount) => _gold += amount;

    [Terminal("god", Description = "切换无敌", Category = "Cheats")]
    public void God(CommandContext ctx) => ctx.Output.WriteLine("god toggled");
}
```

在控制台中执行 `gold 100000` 或 `god`。`help Cheats` 会显示该分类下的命令和说明。

## 2. static 方法 - 无需注册

static `[Terminal]` 方法会在启动时扫描用户程序集并 **自动收集**。不需要注册代码。

```csharp
public static class DebugCheats
{
    [Terminal("ping")]
    public static string Ping() => "pong";

    [Terminal("timescale {0}", Description = "设置 Time.timeScale")]
    public static void SetTimeScale(float scale) => Time.timeScale = scale;
}
```

:::note 关闭自动扫描
在 bootstrap 前设置 `TerminalBehaviour.AutoScanStaticCommands = false` 可以关闭自动扫描，然后使用 `Terminal.RegisterStatic(typeof(DebugCheats))` 手动注册。
:::

## 属性规则

- 模板的第一个 token 是命令名: `"gold {0}"` 会成为 `gold`。
- `{0} {1} ...` 会把输入参数映射到方法参数索引。没有 placeholder 时，参数按方法参数顺序传入。
- `CommandContext` 参数会自动注入，不会消耗用户输入。
- optional 参数会成为 optional 输入: `[Terminal("heal")] string Heal(int n = 100)` 可接受 `heal` 或 `heal 50`。
- 如果有返回值，会输出到控制台。

## 支持的参数类型

`string`, `bool`, `int/long/short/byte`, `float/double`, `enum`, `Vector2/3/4`, `Color`

```bash
pos 1 2 3        # Vector3
pos "1,2,3"      # 相同
god on           # bool: true/false/1/0/on/off
```

## 写入输出

可通过 `CommandContext.Output` 写入不同级别的输出。

```csharp
[Terminal("save")]
public void Save(CommandContext ctx)
{
    ctx.Output.WriteLine("已保存", LogLevel.Success); // Info/Success/Warning/Error/System
}
```

下一步: [命令参考](./commands.md)
