# Cheat Terminal

Unity 런타임 개발자 콘솔. 치트 / 디버깅 / 런타임 객체 조회용 터미널입니다.
메서드에 `[Terminal]` 어트리뷰트만 붙이면 명령이 됩니다. 닫혀 있을 땐 비용이 0인
uGUI 오버레이로 동작합니다. (Unity 6 / `com.unity.ugui`)

## 설치

Unity Package Manager → *Add package from git URL*:

```
https://github.com/achieveonepark/cheat-terminal.git
```

## 열기

에디터와 개발 빌드에서는 자동으로 켜집니다. 화면 **우측 상단의 `>_` 핸들을 탭**하면
콘솔이 열립니다. (릴리즈 빌드에서는 `TerminalBehaviour.Bootstrap()` 한 번 호출)

- 위/아래 화살표: 이전/다음 명령 기록
- `help` : 전체 명령, `help <카테고리>` / `help <명령>` : 상세

## 명령 추가하기

### 1) 인스턴스 메서드 — `Register(this)`

상태(골드, 레벨 등)를 들고 있는 클래스는 자기 자신을 등록합니다.

```csharp
using UniTerminal;
using UnityEngine;

public class PlayerCheats : MonoBehaviour
{
    private int _gold;

    void Start() => TerminalBehaviour.Register(this); // [Terminal] 메서드 자동 수집

    [Terminal("gold {0}", Description = "골드 추가", Category = "Cheats")]
    public void AddGold(int amount) => _gold += amount;

    [Terminal("god", Description = "무적 토글", Category = "Cheats")]
    public void God(CommandContext ctx) => ctx.Output.WriteLine("god toggled");
}
```

→ 콘솔에서 `gold 100000`, `god` 실행. `help Cheats` 치면 설명과 함께 목록이 뜹니다.

### 2) static 메서드 — 등록 불필요

static `[Terminal]` 메서드는 시작할 때 **자동으로 수집**됩니다. 등록 코드가 필요 없어요.

```csharp
public static class DebugCheats
{
    [Terminal("ping")]
    public static string Ping() => "pong";

    [Terminal("timescale {0}", Description = "Time.timeScale 설정")]
    public static void SetTimeScale(float scale) => Time.timeScale = scale;
}
```

### 어트리뷰트 규칙

- 템플릿 첫 토큰이 명령 이름: `"gold {0}"` → 명령 `gold`
- `{0} {1}` 은 입력 인자를 메서드 파라미터 인덱스에 매핑. placeholder 없으면 순서대로.
- `CommandContext` 파라미터는 자동 주입되고 인자를 소비하지 않음.
- optional 파라미터는 optional 인자: `[Terminal("heal")] string Heal(int n = 100)`
- 지원 타입: `string` `bool` `int/long/short/byte` `float/double` `enum`
  `Vector2/3/4` `Color` (예: `pos 1 2 3` 또는 `pos "1,2,3"`)

## 내장 명령 / 모듈

- 기본: `help` `clear` `history` `echo` `alias`
- **Scene**: `scene list | load <name> [additive] | unload <name>`
- **Inspector**: `find <name>` · `inspect <name>` · `set <name>.<member> <value>` · `call <name>.<method> [args]`
- **Performance**: `perf` (FPS / 메모리 / GC / 드로우콜)
- **Logs**: `logs [n | error | warning | info | <text> | find <text> | clear | export]`

GameObject가 아닌 객체를 이름으로 조회하려면:

```csharp
TerminalBehaviour.Instance.GetModule<ObjectInspectorModule>()
    .RegisterObject("Player", playerService);
```
