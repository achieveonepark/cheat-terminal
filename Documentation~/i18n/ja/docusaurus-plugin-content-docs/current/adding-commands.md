---
sidebar_position: 3
title: コマンドを追加する
---

# コマンドを追加する

コマンドは `[Terminal]` 属性が付いたメソッドです。登録方法は 2 つあります。

## 1. インスタンスメソッド - `Register(this)`

ゴールドやレベルなどの状態を持つクラスは、自分自身を登録します。

```csharp
using UniTerminal;
using UnityEngine;

public class PlayerCheats : MonoBehaviour
{
    private int _gold;

    void Start() => TerminalBehaviour.Register(this); // [Terminal] メソッドを収集

    [Terminal("gold {0}", Description = "ゴールドを追加", Category = "Cheats")]
    public void AddGold(int amount) => _gold += amount;

    [Terminal("god", Description = "無敵を切り替え", Category = "Cheats")]
    public void God(CommandContext ctx) => ctx.Output.WriteLine("god toggled");
}
```

コンソールで `gold 100000` または `god` を実行します。`help Cheats` は、そのカテゴリのコマンドを説明付きで表示します。

## 2. static メソッド - 登録不要

static な `[Terminal]` メソッドは、起動時にユーザーアセンブリを走査して **自動収集**されます。登録コードは不要です。

```csharp
public static class DebugCheats
{
    [Terminal("ping")]
    public static string Ping() => "pong";

    [Terminal("timescale {0}", Description = "Time.timeScale を設定")]
    public static void SetTimeScale(float scale) => Time.timeScale = scale;
}
```

:::note 自動スキャンを無効にする
ブートストラップ前に `TerminalBehaviour.AutoScanStaticCommands = false` を設定すると自動スキャンを無効にできます。その場合は `Terminal.RegisterStatic(typeof(DebugCheats))` で手動登録します。
:::

## 属性ルール

- テンプレートの最初のトークンがコマンド名です: `"gold {0}"` は `gold` になります。
- `{0} {1} ...` は入力引数をメソッドパラメーターのインデックスへ対応付けます。placeholder がなければ、引数はパラメーター順に渡されます。
- `CommandContext` パラメーターは自動注入され、ユーザー入力を消費しません。
- optional パラメーターは optional 引数になります: `[Terminal("heal")] string Heal(int n = 100)` は `heal` または `heal 50` を受け取ります。
- 戻り値がある場合はコンソールへ出力されます。

## 対応する引数型

`string`, `bool`, `int/long/short/byte`, `float/double`, `enum`, `Vector2/3/4`, `Color`

```bash
pos 1 2 3        # Vector3
pos "1,2,3"      # 同じ
god on           # bool: true/false/1/0/on/off
```

## 出力を書く

`CommandContext.Output` でレベル付き出力を書けます。

```csharp
[Terminal("save")]
public void Save(CommandContext ctx)
{
    ctx.Output.WriteLine("保存しました", LogLevel.Success); // Info/Success/Warning/Error/System
}
```

次へ: [コマンドリファレンス](./commands.md)
