# Cheat Terminal

Unity 런타임 개발자 콘솔. 치트 / 디버깅 / 로그 확인 / 런타임 상태 조작용 터미널입니다.
명령은 reflection 없이 `RegisterCommand(...)`로 명시 등록합니다. 닫혀 있을 땐 비용이 0인
uGUI 오버레이로 동작합니다. (Unity 6 / `com.unity.ugui`)

## 설치

Unity Package Manager → *Add package from git URL*:

```bash
https://github.com/achieveonepark/cheat-terminal.git
```

## 열기

에디터와 개발 빌드에서는 자동으로 켜집니다. 화면 **우측 상단의 `>_` 핸들을 탭**하면
콘솔이 열립니다. (릴리즈 빌드에서는 `TerminalBehaviour.Bootstrap()` 한 번 호출)

- 위/아래 화살표: 이전/다음 명령 기록
- `help`: 전체 명령, `help <카테고리>` / `help <명령>`: 상세
- 입력 중 command/keyword/data row 후보가 설명과 함께 표시됩니다.
- `Console / Commands / Data` 탭에서 출력, 등록 명령, 등록 데이터 테이블을 볼 수 있습니다.

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
