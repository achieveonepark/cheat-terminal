---
sidebar_position: 4
title: コマンドリファレンス
---

# コマンドリファレンス

## 組み込みコマンド

| コマンド | 説明 |
| --- | --- |
| `help [command\|category]` | コマンド一覧 / 詳細 |
| `clear` | 出力を消去 |
| `history` | 入力履歴 |
| `echo <text>` | テキストを出力 |
| `alias <name> <expansion>` | エイリアスを登録 (`alias remove <name>`, `alias` で一覧) |

## モジュール

デフォルトのブートストラップでは、次のモジュールが自動インストールされます。

### Scene

```bash
scene list                 # 読み込み済みシーン + Build Settings のシーン
scene load Lobby           # 単一ロード
scene load InGame additive # 加算ロード
scene unload InGame
```

### Inspector

リフレクションベースのランタイムオブジェクト探索です。

```bash
find Player                 # 名前で GameObject を検索
inspect Player              # コンポーネントのフィールド/プロパティツリー
set Player.HP 99999         # メンバー値を変更
call Player.Respawn         # メソッドを呼び出し
```

GameObject 以外のオブジェクト、たとえばサービスを名前で公開するには:

```csharp
TerminalBehaviour.Instance.GetModule<ObjectInspectorModule>()
    .RegisterObject("Player", playerService);
```

### Performance

```bash
perf   # FPS / フレーム ms / メモリ / Mono / GC / draw call、batch、triangle (可能な場合)
```

### Logs

`Debug.Log` 出力を上限付きリングバッファへ収集します。

```bash
logs                 # 最新 30 件
logs 100             # 最新 100 件
logs error           # レベルフィルター (error / warning / info)
logs network         # テキスト部分一致フィルター
logs find <text>     # 明示的な検索
logs clear           # 消去
logs export          # ファイルに保存 (persistentDataPath、実機から回収可能)
```

## エイリアスとマクロ

よく使う組み合わせはエイリアスにできます。

```bash
alias rich gold 999999
rich            # -> gold 999999
```
