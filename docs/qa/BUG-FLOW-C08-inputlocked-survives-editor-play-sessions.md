검증: CONFIRMED (EditorSettings 도메인 리로드 꺼짐, GameFlow.cs:7 정적 변수 리셋 없음; 에디터 한정)

# BUG-FLOW-C08 — (에디터 전용) 페이드·선택 도중 Play를 멈추면 다음 Play에서 STG-00 플레이어가 안 움직인다

## 재현 단계
1. 에디터에서 STG-01을 열고 Play.
2. SEG-A 대문(TR-A-DOOR)에 닿아 화면이 검게 되는 0.3초 사이에 Play를 멈춘다. (또는 STG-05/Test_Flow에서 정지 버튼 선택 창이 열린 상태로 멈춤)
3. STG-00 씬을 열고 Play.

## 기대 결과
매 Play는 깨끗한 상태에서 시작한다. STG-00에서 바로 움직일 수 있다.

## 실제 결과 (코드 근거)
- 프로젝트는 Play 진입 시 도메인 리로드를 끈다: `ProjectSettings/EditorSettings.asset:27-28` (`m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 1` = Disable Domain Reload). static 값이 Play 사이에 남는다.
- `GameFlow.InputLocked`에는 초기화가 없다: `Assets/Scripts/Core/GameFlow.cs:5-11` (`RespawnManager`, `PauseMenu`, `Shockwave`는 `[RuntimeInitializeOnLoadMethod]`로 초기화함: `RespawnManager.cs:20-21`, `PauseMenu.cs:24-25`).
- 잠금을 거는 쪽 중 `LevelTransition`(`LevelTransition.cs:33`)과 `StopButton`(`StopButton.cs:63`)은 파괴될 때 잠금을 풀지 않는다 (`DialoguePlayer.OnDestroy`·`PauseMenu.OnDestroy`는 푼다).
- 다음 Play에서 잠금을 풀어 주는 곳은 `RespawnManager.Start`(`RespawnManager.cs:39`), `TitleScreen.Start`(`TitleScreen.cs:14`), `StageFlow.Load`뿐이다. STG-00에는 RespawnManager가 없어서(`Assets/Editor/Stg00Builder.cs:64`) 처음부터 입력이 잠긴 채로 시작한다. 이 상태에서는 일시정지 메뉴도 안 열린다(`PauseMenu.cs:63`).
- 빌드(WebGL)에서는 매번 앱이 새로 시작하므로 생기지 않는다.

## 수정 제안 (참고)
`GameFlow`에 다른 클래스와 같은 초기화를 한 줄 넣는다:
`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { InputLocked = false; EscConsumedFrame = -1; }`

## 심각도
낮음 — 에디터에서만 생기고, 테스트하는 사람이 "STG-00이 고장 났다"고 오해하기 쉬운 정도.
