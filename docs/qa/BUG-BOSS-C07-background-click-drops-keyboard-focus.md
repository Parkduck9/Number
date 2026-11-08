검증: PLAUSIBLE (Unity 기본 EventSystem 동작에 기댄 주장, 코드로 확인 못 했고 에디터/WebGL 실측 필요)

# BUG-BOSS-C07 타이틀·일시정지 메뉴에서 버튼 밖을 한 번 클릭하면 키보드 선택이 사라져 방향키·Enter가 먹지 않는다

- 영역: UI (타이틀, 일시정지 메뉴) / WebGL 키보드 조작
- 확인 방법: 코드 읽기 + Input System 기본 동작 (Play 모드 미실행)

## 재현 단계
1. WebGL 빌드(itch.io)에서 게임을 연다. 키보드 입력을 받으려고 게임 화면의 빈 곳(버튼이 아닌 곳)을 클릭한다.
2. 방향키(위/아래)와 Enter로 타이틀 메뉴를 고르려 한다.
3. 스테이지에서 Esc/P로 일시정지한 뒤, 어두운 배경을 한 번 클릭하고 다시 방향키·Enter를 누른다.

## 기대 결과
- 시작할 때 "Start"(일시정지 때는 "Resume")가 선택돼 있고(`TitleScreen.cs:31`, `PauseMenu.cs:86`), 마우스를 한 번 눌러도 키보드로 계속 메뉴를 고를 수 있다.

## 실제 결과
- EventSystem은 모두 코드로 만든다 (`Assets/Scripts/UI/UiKit.cs:99-104`, 씬 파일에는 EventSystem이 없음). `InputSystemUIInputModule`의 기본값 `deselectOnBackgroundClick = true`라서, 선택할 수 없는 곳을 클릭하면 선택이 `null`이 된다.
  - 타이틀: 버튼 밖 빈 곳 클릭 → 선택 해제.
  - 일시정지: 화면 전체 `Dim` 이미지가 레이캐스트를 받는다 (`Assets/Scripts/UI/PauseMenu.cs:105-106`, `raycast = true`) → 버튼 밖 아무 곳이나 클릭하면 선택 해제.
- 선택된 버튼이 없으면 방향키(Navigate)·Enter(Submit)는 아무 일도 하지 않는다. 테두리 강조(`ButtonHighlight`)도 모두 꺼진다. 다시 키보드로 고르려면 마우스로 버튼을 눌러야 한다.
- WebGL에서는 첫 키 입력 전에 캔버스를 클릭해 포커스를 주는 경우가 많아, 키보드만 쓰려는 플레이어가 처음부터 겪기 쉽다.

## 심각도
낮음 — 마우스로 버튼을 누르면 진행할 수 있다. 키보드 위주 플레이어에게만 불편하다.

## 수정 제안 (코드 담당 참고)
- 둘 중 하나:
  - `UiKit.EnsureEventSystem`에서 만든 모듈에 `deselectOnBackgroundClick = false` 지정 (1줄).
  - 또는 `TitleScreen`/`PauseMenu`의 `Update`에서 `EventSystem.current.currentSelectedGameObject == null`이고 방향키가 눌리면 첫 버튼을 다시 선택.
