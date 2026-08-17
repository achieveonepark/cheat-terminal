# Cheat Terminal

Unity 런타임 개발자 콘솔. 치트 / 디버깅 / 로그 확인 / 런타임 상태 조작용 터미널입니다.
명령은 reflection 없이 `RegisterCommand(...)`로 명시 등록합니다. 닫혀 있을 땐 비용이 0인
uGUI 오버레이로 동작합니다. (Unity 6 / `com.unity.ugui`)

## 설치

Unity Package Manager → *Add package from git URL*:

```bash
https://github.com/achieveonepark/cheat-terminal.git
```

## 열기 (터치 제스처)

에디터와 개발 빌드에서는 자동으로 켜집니다. (릴리즈 빌드에서는 `TerminalBehaviour.Bootstrap()` 한 번 호출)
화면 어디서나 아래 제스처를 쓰면 됩니다. 평소엔 화면에 아무것도 표시되지 않습니다.

| 제스처 | 동작 | 에디터/데스크톱 |
| --- | --- | --- |
| **네 손가락 동시 탭 3회** | 우상단 `>_` 핸들 표시 / 숨김 (다시 하면 숨김) | `F9` |
| **세 손가락 동시 탭 3회** | 좌측 치트 HUD 열기 / 닫기 | `F10` |

핸들이 뜬 상태에서 `>_` 를 탭하면 콘솔이 열립니다.

- 위/아래 화살표: 이전/다음 명령 기록
- `help`: 전체 명령, `help <카테고리>` / `help <명령>`: 상세
- 입력 중 command/keyword/data row 후보가 설명과 함께 표시됩니다.
- `Console / Commands / Data` 탭에서 출력, 등록 명령, 등록 데이터 테이블을 볼 수 있습니다.

## 치트 HUD

세 손가락 3연타로 좌측에서 슬라이드되는 UI Toolkit HUD 입니다. 터미널에 등록한 **모든 명령이
자동으로 등록·표시**되므로 HUD 를 위한 추가 작업은 없습니다.

- 카테고리별로 묶여서 표시되고, 상단 검색창으로 이름/설명/카테고리를 필터링합니다.
- 인자가 없는 치트(`usage` 에 `<`, `[` 없음)는 **탭 한 번으로 즉시 실행**됩니다.
- 인자가 있는 치트는 행을 탭하면 인라인 입력창이 펼쳐지고 `RUN` 또는 Enter 로 실행합니다.
- 실행 결과는 콘솔 출력과 HUD 하단 상태 줄에 함께 남습니다.

```csharp
TerminalBehaviour.ToggleCheatHud();   // 코드로 HUD 토글
TerminalBehaviour.HandleVisible = true; // 우상단 핸들 강제 표시
```

## 명령 추가하기

상태를 가진 객체에서 직접 명령을 등록합니다. 자동 스캔이나 reflection 호출은 사용하지 않습니다.

```csharp
using System.Globalization;
using Achieve.CheatTerminal;
using UnityEngine;

public class PlayerCheats : MonoBehaviour
{
    private int _gold;

    private void Start()
    {
        TerminalBehaviour.RegisterCommand(
            "gold",
            AddGold,
            "골드 추가",
            "Cheats",
            "gold <amount>");

        TerminalBehaviour.RegisterCommand(
            "god",
            ctx => ctx.Output.WriteLine("god toggled", LogLevel.Success),
            "무적 토글",
            "Cheats",
            "god");
    }

    private void AddGold(CommandContext ctx)
    {
        if (!ctx.Has(0) ||
            !int.TryParse(ctx.Args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
        {
            ctx.Output.WriteLine("Usage: gold <amount>", LogLevel.Error);
            return;
        }

        _gold += amount;
        ctx.Output.WriteLine($"Gold is now {_gold}", LogLevel.Success);
    }
}
```

콘솔에서 `gold 100000`, `god` 실행. `help Cheats` 치면 설명과 함께 목록이 뜹니다.

### 데이터 테이블 노출

게임 데이터 테이블을 등록하면 `data` 명령과 자동완성, `Data` 탭에 같이 표시됩니다.

```csharp
TerminalBehaviour.RegisterDataTable("items", "Items", () => new[]
{
    new DataTableRow("sword_001", "Bronze Sword"),
    new DataTableRow("potion_hp", "Health Potion"),
});
```

- `data`: 등록된 테이블 ID / 이름 / row 수
- `data items`: 해당 테이블의 row ID / 이름
- `data items sword`: ID/이름/요약 검색 및 필드 출력

## 내장 명령 / 모듈

- 기본: `help` `clear` `history` `echo` `alias`
- **Scene**: `scene list | load <name> [additive] | unload <name>`
- **Data**: `data [table] [id|text]`
- **Performance**: `perf` (FPS / 메모리 / GC / 렌더 통계)
- **Logs**: `logs [n | error | warning | info | <text> | find <text> | clear | export]`
- **Components**: 유니티 내장 컴포넌트 직접 제어

### Unity 컴포넌트 명령

| 명령 | 설명 | 예시 |
| --- | --- | --- |
| `transform pos\|rot\|scale\|reset <name> [x y z]` | Transform 조작 | `transform pos Player 0 5 0` |
| `rb velocity\|gravity\|kinematic\|mass\|drag <name> [args]` | Rigidbody 제어 | `rb gravity Player off` |
| `cam fov\|bg\|ortho\|size\|clip [args]` | Camera 설정 | `cam fov 90` |
| `light intensity\|color\|range\|shadow <name> [args]` | Light 제어 | `light intensity Sun 2` |
| `audio volume\|mute\|pause\|resume [args]` | 오디오 제어 | `audio volume 0.5` |
| `time scale\|fixed [value]` | 타임스케일 제어 | `time scale 0` |
| `go list\|active\|tag [name] [args]` | GameObject 유틸 | `go active Enemy off` |

`help Components` 로 전체 목록 확인. 자세한 사용법은 [문서](https://somiri.dev/cheat-terminal/unity-components/)를 참조하세요.

## 문서 / 변경 내역

- [문서 사이트](https://somiri.dev/cheat-terminal/) — 시작하기 / 사용법 / 명령 레퍼런스 / 치트 HUD
- [변경 내역](CHANGELOG.ko.md) ([English](CHANGELOG.md)) — 버전별 변경 사항
- [개발 이력](https://somiri.dev/cheat-terminal/history) — 커밋 기록 기준 설계 변화와 업그레이드 요약
- [AGENT.md](AGENT.md) — 이 패키지에서 치트를 작성할 때의 규칙
