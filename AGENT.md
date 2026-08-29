# AGENT.md

이 저장소(`com.achieve.cheat-terminal`)에서 **치트를 만들거나 치트 관련 기능을 구현하는 에이전트가
먼저 읽어야 하는 문서**입니다. 구현 전에 아래 규칙을 지켜야 터미널 / 치트 HUD / 자동완성이
모두 정상 동작합니다.

## 0. 한 줄 요약

치트 = `ICommand` 하나. `RegisterCommand(name, action, description, category, usage)` 로 **명시 등록**하면
터미널 `Commands` 탭, `help`, 자동완성, **전체 화면 치트 HUD** 에 자동으로 나타납니다.
리플렉션 / 자동 스캔은 사용하지 않습니다.

## 1. 구조

```
Runtime/
  Core/        Terminal, CommandRegistry, Parser, History, Alias, AutoComplete, DataTableRegistry
  Commands/    BuiltInCommands (help, clear, history, echo, alias)
  Interfaces/  ICommand, ICommandOutput, ITerminalView, ICommandCompletionProvider
  Model/       CommandContext, CompletionItem, DataTableRow, ParsedCommand
  Modules/     ITerminalModule 구현체 (Scene, Data, Performance, Logs, UnityComponents)
  UI/          UGuiTerminalView(콘솔), TerminalCornerTrigger(우상단 핸들), CheatHudView(치트 HUD)
  UI/Gestures/ MultiFingerTapGesture(멀티터치 제스처), TerminalInput(입력 백엔드 추상화)
  TerminalBehaviour.cs  런타임 진입점 / 정적 편의 API
Editor/
  CheatTerminalMenu.cs  플레이 모드용 `Tools > Cheat Terminal` 메뉴 (에디터 전용 어셈블리)
```

- `TerminalBehaviour` 는 `RuntimeInitializeOnLoadMethod` 로 **에디터·개발 빌드에서만** 자동 생성됩니다.
  릴리즈 빌드는 `TerminalBehaviour.Bootstrap()` 를 직접 호출해야 합니다.
- 모든 UI 는 프리팹/에셋 없이 **코드로 생성**합니다. 이 패키지에는 `.prefab`, `.asset`, `.uss` 파일이 없습니다.
  새 UI 도 같은 방식(코드 생성)으로 만드세요.

## 2. 치트(명령) 등록 방법

```csharp
using Achieve.CheatTerminal;

TerminalBehaviour.RegisterCommand(
    "gold",              // name      : 공백 없는 소문자 한 단어 권장
    AddGold,             // action    : Action<CommandContext>
    "골드 추가",          // description: HUD / help 에 그대로 노출
    "Cheats",            // category  : HUD 섹션 헤더, help <category> 키
    "gold <amount>");    // usage     : 인자 표기 (아래 규칙 중요)
```

인스턴스 상태가 필요한 치트는 해당 MonoBehaviour 의 `Start()` 에서 등록하세요.
`TerminalBehaviour.Current` 로 `Terminal` 인스턴스를 직접 받을 수도 있습니다.

### usage 문자열 규칙 (치트 HUD 동작을 결정함)

| usage 예시 | HUD 동작 |
| --- | --- |
| `god` (`<`, `[` 없음) | 행을 탭하면 **즉시 실행** |
| `gold <amount>` | 행을 탭하면 인라인 입력창이 펼쳐지고 `RUN` 으로 실행 |
| `heal [amount]` | 위와 동일 (선택 인자도 입력창) |

즉 **필수 인자는 `<...>`, 선택 인자는 `[...]`, 인자가 없으면 아무 괄호도 쓰지 않는다**는 규칙을
반드시 지켜야 HUD 가 원터치 치트와 인자 치트를 올바르게 구분합니다.

### category 규칙

- 게임 치트는 `"Cheats"`, 디버그 도구는 `"Debug"` 처럼 의미 있는 이름을 씁니다.
- `"System"` 카테고리는 내장 명령용이며, HUD 에서 **항상 목록 맨 뒤로** 정렬됩니다.
- `category` 를 비우면 `"General"` 로 처리됩니다.

## 3. 인자 파싱 / 출력

```csharp
private void AddGold(CommandContext ctx)
{
    if (!ctx.Has(0))
    {
        ctx.Output.WriteLine("Usage: gold <amount>", LogLevel.Error);
        return;
    }

    int amount = ctx.GetInt(0);            // GetString / GetInt / GetFloat / GetBool 제공
    _gold += amount;
    ctx.Output.WriteLine($"Gold: {_gold}", LogLevel.Success);
}
```

- 숫자 파싱은 반드시 **`CultureInfo.InvariantCulture`** 로 (지역 설정에 따라 `0.5` 가 깨지지 않도록).
  `ctx.GetInt/GetFloat` 는 이미 invariant 입니다.
- 사용자 피드백은 `Debug.Log` 가 아니라 `ctx.Output.WriteLine(text, LogLevel)` 로 보냅니다.
- `LogLevel`: `Info`, `Success`, `Warning`, `Error`, `System`.
- 예외를 던져도 `Terminal.Execute` 가 잡아서 `Error` 로 출력하지만, 예측 가능한 실패는 직접 메시지를 쓰세요.

## 4. 자동완성 / 데이터 테이블

```csharp
TerminalBehaviour.RegisterCommand("spawn", Spawn, "몹 소환", "Cheats", "spawn <id> [count]",
    (ctx, results) =>
    {
        if (ctx.ArgumentIndex != 0) return;          // 첫 번째 인자에만 후보 제공
        foreach (var id in MonsterIds)
            results.Add(new CompletionItem(id, id, "monster", "Cheats", CompletionKind.Argument));
    });

TerminalBehaviour.RegisterDataTable("items", "Items", () => new[]
{
    new DataTableRow("sword_001", "Bronze Sword"),
});
```

데이터 테이블은 `data` 명령, 자동완성, 터미널 `Data` 탭에서 함께 사용됩니다.

## 5. UI / 입력 규칙

| 기능 | 진입 방법 |
| --- | --- |
| 우상단 `>_` 핸들 표시/숨김 | **네 손가락 동시 탭 3회** (에디터/데스크톱: `F9`) |
| 치트 HUD (전체 화면) | **세 손가락 동시 탭 3회** (에디터/데스크톱: `F10`, `F2`) |
| 콘솔 열기/닫기 | 핸들 탭, HUD 헤더의 `>_`, 에디터/데스크톱 `F1` 또는 `` ` ``, `TerminalBehaviour.Toggle()` |
| 에디터 (게임 뷰 비포커스) | `Tools > Cheat Terminal` 메뉴 |

- 제스처는 `MultiFingerTapGesture` 가 담당합니다. **손가락 수가 정확히 일치**할 때만 탭으로 인정하므로
  3-finger 와 4-finger 제스처가 서로 섞이지 않습니다. 새 제스처가 필요하면
  `MultiFingerTapGesture.Attach(go, fingers, taps, fallbackKey)` 를 쓰세요.
- 입력은 **레거시 Input Manager / Input System 양쪽**을 지원해야 합니다. 직접 `UnityEngine.Input` 이나
  `UnityEngine.InputSystem` 을 참조하지 말고 `TerminalInput` 에 API 를 추가하세요.
  Input System 코드는 `ACHIEVE_CHEAT_TERMINAL_INPUT_SYSTEM`(asmdef `versionDefines`) 으로 감쌉니다.
- 콘솔 키(`F1` / `` ` ``)는 `TerminalBehaviour.Update()` 에서 직접 처리합니다. 백틱은 **열기 전용**이라
  명령 입력 중에 키를 가로채지 않습니다. 에디터 메뉴에는 단축키를 달지 마세요. 런타임 키와 같이
  발동해서 서로 상쇄됩니다.
- 클릭·탭은 `EventSystem` 이 있어야 동작합니다. `UGuiTerminalView.EnsureEventSystem()` 이 없을 때 만들며,
  Input System 전용 프로젝트에는 `InputSystemUIInputModule` 을 붙입니다(버전 디파인으로 감싼 직접 참조).
- 치트 HUD 는 `Runtime/UI/CheatHudView.cs` 의 UI Toolkit 패널입니다. 스타일은 테마 에셋 없이도
  동일하게 보이도록 **전부 인라인 스타일**로 지정합니다. 닫혀 있을 때는 `display: none` 이라 비용이 0이며,
  루트는 `PickingMode.Ignore` 라 게임 입력을 가로채지 않습니다. HUD 목록은
  `Terminal.Registry.Changed` 를 구독해 자동 갱신되므로, 명령을 등록하기만 하면 HUD 작업은 따로 없습니다.
- HUD 는 기본이 **전체 화면**입니다. 새 요소를 추가할 때는 터치 목표를 최소 **104px**(참조 해상도
  1080x1920 기준)로 잡고, 폰트는 20px 이상으로 두세요. 안전 영역은 `_content` 패딩으로 처리되므로
  새 요소는 `_panel` 이 아니라 `_content` 아래에 붙입니다.
- 열림/닫힘은 `TerminalBehaviour` 가 `View.IsOpen` / `CheatHud.IsOpen` 을 읽어 `OnVisibilityChanged`
  (정적 별칭 `VisibilityChanged`) 로 알립니다. 새 UI 표면을 추가하면 `LateUpdate` 의 집계에 함께 넣으세요.
  `PauseWhileOpen` / `OpenTimeScale` 은 그 이벤트 위에 얹은 편의 기능입니다.

## 6. 코드 규칙 (반드시 지킬 것)

- **리플렉션 금지.** IL2CPP/AOT 에서 안전해야 합니다. 명령은 항상 명시 등록입니다.
- **외부 의존성 금지.** `com.unity.ugui` + `com.unity.modules.uielements` 외 패키지를 추가하지 마세요.
  Input System 은 선택적 의존성(versionDefines)으로만 다룹니다.
- 매 프레임 도는 코드(`Update`, `LateUpdate`)에서 **할당(LINQ/문자열 연결) 금지**. 목록 재구성처럼
  비싼 작업은 `dirty` 플래그로 모아서 처리합니다 (`UGuiTerminalView`, `CheatHudView` 참고).
- 리치 텍스트를 쓰는 uGUI 출력은 `EscapeRichText` 로 이스케이프합니다. UI Toolkit 라벨은
  `enableRichText = false` 로 둡니다.
- 새 파일마다 `.meta` 파일을 함께 커밋합니다(폴더 포함). 기존 파일 형식을 그대로 따르세요.
- `public` API 를 지우거나 시그니처를 바꾸면 **breaking change** 입니다. 알리아스를 남기고
  CHANGELOG 에 기록하세요.

## 7. 문서 / 릴리즈 규칙

- 변경 사항은 `CHANGELOG.md`(영문)와 `CHANGELOG.ko.md`(한국어) **양쪽 모두**에 적습니다.
- 사용자 문서는 `docs~/content/docs` 에 **ko(기본) / en / ja / zh 4개 언어**로 존재합니다.
  기능을 추가하면 4개 파일을 함께 수정하고, 새 페이지는 각 언어의 `meta*.json` `pages` 에 등록합니다.
- 버전은 `package.json` 의 `version` 을 SemVer 로 올립니다(기능 추가 = minor).
- 샘플은 `Samples~/BasicUsage` 에 있습니다. 사용법이 바뀌면 샘플 주석도 갱신하세요.
- 설계가 지금 형태가 된 배경(리플렉션 제거, 구체 코어 전환, 제스처 UI 등)은
  `docs~/content/docs/history.mdx` 의 개발 이력 문서를 참고하세요.
