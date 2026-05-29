---
sidebar_position: 5
title: Unity 组件命令
---

# Unity 组件命令

`UnityComponentsModule` 提供 7 个命令，覆盖 Unity 中最常用的内置组件，无需修改代码即可从终端直接控制。  
在编辑器和开发构建中自动安装，无需额外配置。

---

## `transform` — Transform 操作

```bash
transform get   <name>                   # 输出位置、旋转和缩放
transform pos   <name> [x y z]           # 获取或设置世界坐标位置
transform rot   <name> [x y z]           # 获取或设置欧拉角旋转
transform scale <name> [x y z]           # 获取或设置本地缩放
transform reset <name>                   # 重置 position/rotation/scale 为默认值
```

### 示例

```bash
transform get   Player
transform pos   Player 0 1 0
transform rot   Enemy  0 90 0
transform scale Boss   2 2 2
transform reset Cube
```

---

## `rb` — Rigidbody 控制

```bash
rb get       <name>           # 输出所有 Rigidbody 属性
rb velocity  <name> [x y z]  # 获取或设置线速度
rb gravity   <name> [on|off] # 获取或设置 useGravity
rb kinematic <name> [on|off] # 获取或设置 isKinematic
rb mass      <name> [value]  # 获取或设置质量
rb drag      <name> [value]  # 获取或设置线性阻尼 (drag)
```

---

## `cam` — Camera 设置

默认操作 `Camera.main`。

```bash
cam list                          # 列出场景中所有摄像机
cam fov  [value]                  # 获取或设置 fieldOfView (1–179°)
cam bg   [r g b [a]]              # 获取或设置 backgroundColor (0–1)
cam ortho [on|off]                # 切换透视/正交模式
cam size  [value]                 # 获取或设置 orthographicSize
cam clip  [near far]              # 获取或设置近/远裁剪面
```

---

## `light` — Light 控制

```bash
light list                        # 列出场景中所有 Light
light intensity <name> [value]    # 获取或设置亮度
light color     <name> [r g b]    # 获取或设置颜色 (0–1)
light range     <name> [value]    # 获取或设置范围
light shadow    <name> [on|off]   # 开关软阴影
```

---

## `audio` — AudioListener 控制

```bash
audio volume [value]   # 获取或设置主音量 (0–1)
audio mute   [on|off]  # 静音开关
audio pause            # 暂停所有音频
audio resume           # 恢复所有音频
```

---

## `time` — Time 控制

```bash
time get            # 输出 timeScale, fixedDeltaTime, time, frameCount
time scale [value]  # 获取或设置 Time.timeScale (≥ 0)
time fixed [value]  # 获取或设置 Time.fixedDeltaTime
```

### 示例

```bash
time scale 0.5     # 慢动作
time scale 0       # 暂停
time scale 1       # 恢复正常速度
```

---

## `go` — GameObject 工具

```bash
go list   [tag]             # 列出 GameObject（最多 40 个，可按标签过滤）
go active <name> <on|off>   # 调用 SetActive
go tag    <name> [tag]      # 获取或设置标签
```

---

## 分类

所有命令属于 `Components` 类别。运行 `help Components` 可查看完整列表。
