---
sidebar_position: 4
title: 명령 레퍼런스
---

# 명령 레퍼런스

## 내장 명령

| 명령 | 설명 |
| --- | --- |
| `help [명령\|카테고리]` | 명령 목록 / 상세 |
| `clear` | 출력 지우기 |
| `history` | 입력 기록 |
| `echo <text>` | 텍스트 출력 |
| `alias <name> <expansion>` | 별칭 등록 (`alias remove <name>`, `alias` 로 목록) |

## 모듈

기본 부트스트랩에서 아래 모듈이 자동 설치됩니다.

### Scene

```bash
scene list                 # 로드된 씬 + 빌드 세팅 씬
scene load Lobby           # 단일 로드
scene load InGame additive # 가산 로드
scene unload InGame
```

### Inspector

리플렉션 기반 런타임 객체 탐색.

```bash
find Player                 # 이름으로 GameObject 검색
inspect Player              # 컴포넌트 필드/프로퍼티 트리
set Player.HP 99999         # 멤버 값 변경
call Player.Respawn         # 메서드 호출
```

GameObject가 아닌 객체(서비스 등)를 이름으로 노출하려면:

```csharp
TerminalBehaviour.Instance.GetModule<ObjectInspectorModule>()
    .RegisterObject("Player", playerService);
```

### Performance

```bash
perf   # FPS / 프레임 ms / 메모리 / Mono / GC / 드로우콜·배치·삼각형 (가능 시)
```

### Logs

`Debug.Log` 출력을 캡 링 버퍼에 수집합니다.

```bash
logs                 # 최근 30개
logs 100             # 최근 100개
logs error           # 레벨 필터 (error / warning / info)
logs network         # 텍스트 부분일치 필터
logs find <text>     # 명시적 검색
logs clear           # 비우기
logs export          # 파일로 저장 (persistentDataPath, 실기기 회수 가능)
```

## 별칭 & 매크로

자주 쓰는 조합은 별칭으로:

```bash
alias rich gold 999999
rich            # → gold 999999
```
