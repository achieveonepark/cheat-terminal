---
slug: /
sidebar_position: 1
title: 소개
---

# Cheat Terminal

Unity 런타임 개발자 콘솔입니다. 치트 · 디버깅 · 런타임 객체 조회를 한 곳에서 처리합니다.

메서드에 `[Terminal]` 어트리뷰트만 붙이면 그게 곧 명령이 됩니다. 콘솔이 닫혀 있을
때는 비용이 **0** 인 uGUI 오버레이로 동작합니다.

```csharp
[Terminal("gold {0}", Description = "골드 추가", Category = "Cheats")]
public void AddGold(int amount) => _gold += amount;
```

## 특징

- **어트리뷰트 기반** — `[Terminal("name {0}")]` 한 줄로 명령 등록. static 메서드는
  시작 시 **자동 수집**.
- **성능 우선** — 닫혀 있으면 캔버스 비활성화(드로우콜·레이캐스트 0), 출력은 캡 링
  버퍼, 화살표 기록은 입력시스템 중립(`IMoveHandler`).
- **모듈식** — Scene / Inspector / Performance / Logs 모듈을 기본 제공, 필요한 것만 사용.
- **모바일 친화** — 우측 상단 핸들 탭으로 열기, 로그를 기기에서 파일로 export.

## 요구 사항

- Unity 6 (6000.x)
- `com.unity.ugui` (기본 포함)

다음: [시작하기](./getting-started.md)
