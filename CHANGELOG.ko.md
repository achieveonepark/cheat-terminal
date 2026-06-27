# 변경 내역

> 🌐 [English](CHANGELOG.md)

이 패키지의 주요 변경 사항을 모두 여기에 기록합니다.
이 프로젝트는 [유의적 버전(Semantic Versioning)](https://semver.org/)을 따릅니다.

## [Unreleased]

## [1.1.0] - 2026-06-27

### 변경 (호환성 깨짐)
- 리플렉션 기반 `[Terminal]` 메서드 탐색 및 호출을 제거했습니다. 이제 명령은
  `RegisterCommand(...)`로 명시적으로 등록합니다.
- 기본 런타임에서 리플렉션 기반 Object Inspector 모듈을 제거했습니다.
- EventSystem 폴백이 더 이상 리플렉션으로 선택적 Input System 타입을 탐색하지 않습니다.
  Input System 전용 입력을 사용하는 프로젝트는 `InputSystemUIInputModule`이 포함된
  EventSystem을 제공해야 합니다.

### 수정
- 터미널 입력이 포커스 해제가 아니라 명시적으로 제출(submit)할 때만 실행됩니다.
- 터미널 출력이 줄을 색상 태그로 감싸기 전에 리치 텍스트에 민감한 문자를 이스케이프합니다.
- Unity 컴포넌트 명령이 이제 불변 문화권(invariant-culture) 숫자 파싱, 엄격한 bool 파싱,
  그리고 필요한 경우 비활성 객체를 포함하는 GameObject 조회를 사용합니다.
- Logs 및 Performance 모듈이 생성한 런타임 헬퍼 GameObject가 터미널 부트스트랩과 함께
  정리됩니다.

### 추가
- 명령 이름, 사용법 키워드, 명령별 제공자에 대한 자동완성 항목 캐싱.
- 씬, 로그, GameObject, 컴포넌트 대상, 불리언 값 등을 포함한 내장 명령용 문맥 인식 자동완성.
- 리플렉션 없이 데이터를 조회하는 `data [table] [id|text]` 명령과 `RegisterDataTable(...)`.
- Console, 등록된 Commands, 등록된 Data 테이블을 위한 터미널 UI 탭.

## [1.0.1] - 2026-05-30

### 변경
- 네임스페이스 `UniTerminal` → `Achieve.CheatTerminal` 로 변경.

### 추가
- Unity 내장 컴포넌트(`Transform`, `Rigidbody`, `Camera` 등)에 대한 터미널 명령 지원 추가.
- `ITerminalModule` 고급 사용법 문서 섹션 추가 (ko/en/ja/zh-CN).

## [1.0.0] - 2026-05-29

### 변경 (호환성 깨짐)
- 내부 협력 인터페이스(`ICommandRegistry`, `ICommandParser`, `ICommandHistory`,
  `IAutoCompleteProvider`, `IAliasResolver`, `IArgumentConverter`, `ITerminalTrigger`,
  `ITerminal`)와 `TerminalBuilder`를 제거했습니다. 핵심 `Terminal`은 이제 구체적인
  협력 객체를 소유하는 구체 클래스입니다. 실제 확장 지점으로는 `ICommand`,
  `ICommandOutput`, `ITerminalView`, `ITerminalModule`만 남습니다.
- 명령용 마커 인터페이스가 없으며 — `[Terminal]` 어트리뷰트가 유일한 마커입니다.
- `Terminal.ScanStaticCommands()`가 사용자 어셈블리를 훑어 모든 정적 `[Terminal]`
  메서드를 자동 등록합니다. 부트스트랩이 시작 시 이를 실행합니다
  (`TerminalBehaviour.AutoScanStaticCommands`로 토글). 인스턴스 명령은 여전히
  `Register(this)`를 사용합니다.

### 추가
- 런타임 Logs 모듈: `Debug.Log` 출력을 링 버퍼에 수집하며
  `logs [n | error | warning | info | <text> | find <text> | clear | export]`로 다룹니다.
  export는 `Application.persistentDataPath`에 기록하므로 기기에서 로그를 회수할 수 있습니다.
- 입력 필드에서 위/아래 화살표로 이전/다음 명령 기록을 불러옵니다
  (uGUI `IMoveHandler` 경유라 레거시 Input Manager와 Input System 패키지 양쪽에서 동작).
- `help <category>`가 카테고리 내 명령을 설명과 함께 나열합니다.
  이제 `help`는 `help <command>` / `help <category>`를 안내합니다.

### 수정
- Input System 패키지가 활성화되어 있으면 EventSystem이 (리플렉션으로 해석하여)
  `InputSystemUIInputModule`을 생성하므로 매 프레임 발생하던
  `InvalidOperationException`을 피합니다.
- 우측 상단 모서리 트리거가 이제 보이는 `>_` 핸들이 되었고 한 번 탭으로 열립니다.

## [0.1.0] - 2026-05-29

### 추가
- 핵심 명령 시스템: 기본 구현이 포함된 `ICommandRegistry`, `ICommandParser`(따옴표 인식),
  `ICommandHistory`, `IAutoCompleteProvider`, `IAliasResolver`, `IArgumentConverter`,
  `ICommandOutput`.
- `[Terminal("name {0}")]` 어트리뷰트와 리플렉션 기반 `AttributeCommand` 바인딩.
  위치 기반 바인딩, 선택적 매개변수, `CommandContext` 주입을 포함합니다.
- `TerminalBuilder`로 조립되는 인터페이스 기반 `Terminal` 코어.
- 닫혀 있을 때 유휴 비용이 0이 되도록 비활성화되는 uGUI 오버레이 뷰(`UGuiTerminalView`).
- 우측 상단 모서리 열기 제스처(`TerminalCornerTrigger`, 기본값은 더블 탭).
- 정적 편의 API를 갖춘 `TerminalBehaviour` 부트스트랩. 에디터와 개발 빌드에서 자동
  부트스트랩됩니다.
- 내장 명령: `help`, `clear`, `history`, `echo`, `alias`.
- 부트스트랩에서 `InstallModule` / `GetModule<T>`를 제공하는 모듈 시스템(`ITerminalModule`).
- Scene Tools 모듈: `scene list | load <name> [additive] | unload <name>`.
- Object Inspector 모듈: `find`, `inspect`, `set <name>.<member> <value>`, `call`.
  명시적 객체 등록(`ObjectInspectorModule.RegisterObject`)을 지원합니다.
- Performance Monitor 모듈: `perf` (FPS, 프레임 ms, 메모리, GC, `ProfilerRecorder`를 통한
  렌더 통계).
- Basic Usage 샘플.
