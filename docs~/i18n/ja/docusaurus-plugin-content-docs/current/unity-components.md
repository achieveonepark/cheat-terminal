---
sidebar_position: 5
title: Unity コンポーネントコマンド
---

# Unity コンポーネントコマンド

`UnityComponentsModule` は、Unity でよく使われる 7 種類の組み込みコンポーネントをターミナルから直接操作するコマンドを提供します。  
エディターと開発ビルドで自動的にインストールされるため、追加のセットアップは不要です。

---

## `transform` — Transform 操作

```bash
transform get   <name>                   # 位置・回転・スケールを出力
transform pos   <name> [x y z]           # 位置を取得または設定
transform rot   <name> [x y z]           # オイラー角を取得または設定
transform scale <name> [x y z]           # ローカルスケールを取得または設定
transform reset <name>                   # position/rotation/scale を初期化
```

### 例

```bash
transform get   Player
transform pos   Player 0 1 0
transform rot   Enemy  0 90 0
transform scale Boss   2 2 2
transform reset Cube
```

---

## `rb` — Rigidbody 制御

```bash
rb get       <name>           # Rigidbody の全プロパティを出力
rb velocity  <name> [x y z]  # 線速度を取得または設定
rb gravity   <name> [on|off] # useGravity を取得または設定
rb kinematic <name> [on|off] # isKinematic を取得または設定
rb mass      <name> [value]  # 質量を取得または設定
rb drag      <name> [value]  # 線形減衰 (drag) を取得または設定
```

---

## `cam` — Camera 設定

`Camera.main` に対して操作します。

```bash
cam list                          # シーン内の全カメラ一覧
cam fov  [value]                  # fieldOfView を取得または設定 (1–179°)
cam bg   [r g b [a]]              # backgroundColor を取得または設定 (0–1)
cam ortho [on|off]                # 遠近法/平行投影の切り替え
cam size  [value]                 # orthographicSize を取得または設定
cam clip  [near far]              # クリッピングプレーンを取得または設定
```

---

## `light` — Light 制御

```bash
light list                        # シーン内の全 Light 一覧
light intensity <name> [value]    # 明るさを取得または設定
light color     <name> [r g b]    # 色を取得または設定 (0–1)
light range     <name> [value]    # 範囲を取得または設定
light shadow    <name> [on|off]   # ソフトシャドウの on/off
```

---

## `audio` — AudioListener 制御

```bash
audio volume [value]   # マスターボリュームを取得または設定 (0–1)
audio mute   [on|off]  # ミュート on/off
audio pause            # 音声を一時停止
audio resume           # 音声を再開
```

---

## `time` — Time 制御

```bash
time get            # timeScale, fixedDeltaTime, time, frameCount を出力
time scale [value]  # Time.timeScale を取得または設定 (0 以上)
time fixed [value]  # Time.fixedDeltaTime を取得または設定
```

### 例

```bash
time scale 0.5     # スローモーション
time scale 0       # 一時停止
time scale 1       # 通常速度に戻す
```

---

## `go` — GameObject ユーティリティ

```bash
go list   [tag]             # GameObject 一覧 (最大 40 件、タグでフィルタ可)
go active <name> <on|off>   # SetActive を呼び出す
go tag    <name> [tag]      # タグを取得または設定
```

---

## カテゴリ

すべてのコマンドは `Components` カテゴリに属します。`help Components` で一覧を確認できます。
