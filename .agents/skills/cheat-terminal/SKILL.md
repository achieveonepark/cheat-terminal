---
name: cheat-terminal
description: Unity 프로젝트의 com.achieve.cheat-terminal에 게임 치트를 추가하고, 파라미터 폼·명령 자동완성·데이터 테이블을 연결한다. 골드 지급, 무적, 순간이동 등 런타임 치트 작성과 기존 명령 수정에 사용한다.
---

# Cheat Terminal 치트 작성

명령을 한 번 등록하면 Terminal, Cheat Library, help, 자동완성에서 함께 사용된다.
별도 HUD 버튼이나 reflection 기반 명령 검색을 만들지 않는다.

## 프로젝트 확인

- 패키지 저장소에서는 루트 `AGENT.md`, `Runtime/TerminalBehaviour.cs`,
  `Runtime/Core/CommandForm.cs`, `Samples~/BasicUsage/ExampleCheats.cs`를 참고한다.
- 사용하는 게임 프로젝트에서는 `Packages/manifest.json`에서
  `com.achieve.cheat-terminal`의 설치 위치와 버전을 확인한다. 게임 치트 코드는 기존 게임 코드에 추가한다.
- 값을 소유한 게임 서비스나 컴포넌트를 찾아 실제 상태 변경 API를 호출한다.
  UI 표시값만 바꾸거나 치트용으로 중복 상태를 만들지 않는다.
- 릴리즈 빌드는 자동 생성되지 않는다. 릴리즈에서 치트를 켜 달라는 요청이 없다면
  별도의 릴리즈 부트스트랩을 추가하지 않는다.

## 등록과 파라미터 폼

```csharp
TerminalBehaviour.RegisterCommand(
    "gold", AddGold, "골드 지급", "Cheats", "gold <amount>");
```

`name`은 공백 없는 고유 이름, `description`은 동작 설명, `category`는 게임의 기능별 분류를 사용한다.
내장 명령용 `System` 카테고리는 피한다. 사용 중인 명령 이름을 덮어쓰지 않는다.

| usage | Cheat Library |
| --- | --- |
| `god` | 입력란 없이 Run 버튼 |
| `gold <amount>` | 필수 amount 입력란과 Run 버튼 |
| `heal [amount]` | 생략 가능한 amount 입력란 |
| `teleport <x> <y> <z>` | 좌표별 입력란 3개 |
| `weather <sunny\|rain\|snow>` | 입력란과 선택값 버튼 |
| `echo <text...>` 또는 `[args]` | 공백으로 구분한 여러 인자 입력 |

명령어 이름을 입력란에 쓰게 하지 않는다. 폼이 명령을 조립한다.
단일 값의 공백·따옴표·경로는 자동으로 처리한다. `[args]`와 `<text...>`는 여러 토큰으로 처리되며,
그 안에서 공백을 포함한 하나의 값은 따옴표로 묶는다.
선택 인자는 뒤쪽에 두고, 앞의 선택값을 비운 채 뒤쪽 값을 입력하는 형식은 피한다.
필수값의 존재는 폼이 검사하지만 타입·범위·게임 상태 검사는 명령 자체에서 수행한다.

## 상태를 가진 치트 예시

기존 게임 서비스로 바꿔 쓸 수 있는 등록·해제·검증 예시다.

```csharp
using System.Globalization;
using Achieve.CheatTerminal;
using Achieve.CheatTerminal.Core;
using UnityEngine;

public sealed class PlayerCheats : MonoBehaviour
{
    private Terminal _terminal;
    private DelegateCommand _goldCommand;
    private int _gold; // 실제 게임에서는 지갑/경제 서비스 사용

    private void OnEnable()
    {
        _terminal = TerminalBehaviour.Current;
        _goldCommand = new DelegateCommand("gold", AddGold,
            "골드 지급", "Cheats", "gold <amount>");
        _terminal.RegisterCommand(_goldCommand);
    }

    private void OnDisable()
    {
        // 다른 인스턴스가 교체한 명령은 삭제하지 않는다. 해제 시 Bootstrap도 하지 않는다.
        if (_terminal != null && _terminal.Registry.TryGet("gold", out var registered)
            && ReferenceEquals(registered, _goldCommand))
            _terminal.Registry.Unregister("gold");
        _terminal = null;
    }

    private void AddGold(CommandContext ctx)
    {
        if (!ctx.Has(0) || !int.TryParse(ctx.Args[0], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int amount)
            || amount < 1 || amount > 1000000 || _gold > int.MaxValue - amount)
        {
            ctx.Output.WriteLine("amount는 1~1000000의 정수여야 합니다.", LogLevel.Error);
            return;
        }
        _gold += amount;
        ctx.Output.WriteLine($"Gold: {_gold}", LogLevel.Success);
    }
}
```

실제 의존성이 Start 이후 준비된다면 등록 시점을 그 초기화 흐름에 맞춘다.
씬 객체를 캡처한 delegate는 수명 종료 시 해제한다. `GetInt/GetFloat`의 fallback은 잘못된 입력도
기본값으로 처리하므로 잘못된 값을 거절해야 하는 명령은 위처럼 `TryParse`로 검사한다.

## 동적 ID 자동완성

```csharp
TerminalBehaviour.RegisterCommand("spawn", Spawn, "몬스터 소환", "Cheats",
    "spawn <id> [count]", (ctx, results) =>
    {
        if (ctx.ArgumentIndex != 0) return;
        foreach (string id in monsterIds)
            if (id.StartsWith(ctx.CurrentToken, System.StringComparison.OrdinalIgnoreCase))
                results.Add(new CompletionItem(id, id, "monster", "Cheats", CompletionKind.Argument));
    });
```

데이터를 탐색해야 하면 `RegisterDataTable(id, name, rowsProvider, description)`을 사용한다.
이 데이터는 `data` 명령과 Terminal의 Data 탭에서 보인다.
동적 자동완성은 Terminal용이다. Cheat Library의 선택값 버튼은 usage의 `a|b|c`에서 생성된다.

## 확인

- F1: Cheat Library. 파라미터만 입력하고 Run으로 상태가 바뀌는지 확인한다.
- F2 또는 백틱: Terminal. 같은 명령을 직접 입력해 동일한 결과를 확인한다.
- 기본 우상단 도크의 슬라이더/터미널 아이콘도 두 화면을 각각 연다. 그립이나 버튼을 드래그해 이동한다.
- 세 손가락 3연타는 Cheat Library, 네 손가락 3연타/F9는 도크 표시 전환이다.
- 정상 입력, 잘못된 타입/범위, 선택값 생략, 씬 재진입과 해제 후 호출을 확인한다.
- 피드백은 `ctx.Output.WriteLine(..., LogLevel.Success/Error)`를 사용한다.
  `Debug.Log`만 쓰면 카드 실행 결과에 표시되지 않는다.
- reflection, 추가 패키지, 매 프레임 등록을 도입하지 않는다. 새 Unity 파일에는 `.meta`를 포함한다.
- 사용한 Unity 검증과 남은 수동 확인을 구분해 보고한다.
