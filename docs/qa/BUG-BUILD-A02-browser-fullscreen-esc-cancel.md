검증: CONFIRMED (BroadcastHackUi.cs:125, StopButton.cs:49 Esc 전용). 심각도 높음은 과대, 중간~낮음이 적절 — PauseMenu는 P키 대체 있음. 중복: BUG-BUILD-C06

# BUG-BUILD-A02 — 브라우저 전체화면(Fullscreen)에서 `Esc` 키 가로채기로 해킹 다이얼 및 정지 버튼 선택 창을 닫을 수 없음

## 심각도
높음

## 관련 파일 및 근거 (`file:line`)
- `Assets/Scripts/UI/PauseMenu.cs:6, 68` (`// Pause menu (Esc or P; browsers eat Esc in fullscreen).` 주석과 함께 `kb.escapeKey` 외에 `kb.pKey` 대체 키를 지원함)
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:8, 125, 221-222` (`UpdateDial()`에서 취소 키로 `kb.escapeKey.wasPressedThisFrame`만 검사하며 대체 취소 키나 마우스 닫기 버튼이 없음)
- `Assets/Scripts/Level/StopButton.cs:8, 46-50, 122-123` (`Phase.Choosing` 상태에서 취소 키로 `kb.escapeKey.wasPressedThisFrame`만 검사하며 대체 키나 마우스 버튼이 없음. 또한 `Esc` 처리 시 `GameFlow.EscConsumedFrame = Time.frameCount;`를 기록하지 않음)

## 재현 단계
1. WebGL 빌드를 브라우저(itch.io)에서 실행하고 전체화면(Fullscreen) 모드로 전환한다.
2. **사례 A (전광판 해킹 다이얼)**: STG-03~05에서 전광판 근처에 가서 `E` 키를 눌러 해킹 다이얼(`BroadcastHackUi`)을 연 뒤, 해킹을 취소하기 위해 화면 안내(`Esc: cancel` / `Esc: 취소`)대로 `Esc` 키를 누른다.
3. **사례 B (AI 정지 버튼 선택 창)**: STG-05에서 AI 정지 버튼(`StopButton`) 앞에서 `E` 키를 눌러 엔딩 선택 창(`[1]`, `[2]`, `Esc: back` / `Esc: 돌아가기`)을 연 뒤, 돌아가기 위해 `Esc` 키를 누른다.

## 기대 결과
- 브라우저 전체화면 상태에서도 플레이어가 전체화면을 강제로 풀지 않고 UI(`BroadcastHackUi` 해킹 다이얼, `StopButton` 엔딩 선택 창)를 즉시 취소/닫을 수 있어야 한다(예: `PauseMenu.cs:68`처럼 브라우저가 가로채지 않는 대체 키 `Q`/`Backspace`/`P` 또는 마우스 우클릭/닫기 버튼 지원).

## 실제 결과
- 웹 브라우저는 전체화면 상태에서 `Esc` 키 입력을 브라우저의 전체화면 종료 명령으로 가로채어(consume) WebGL 캔버스로 `keydown` 이벤트를 전달하지 않는다.
- 그 결과 플레이어가 `Esc`를 누르면 게임 내 해킹 다이얼이나 정지 버튼 선택 창은 닫히지 않은 채 브라우저 전체화면만 해제되며, 특히 해킹 다이얼(`BroadcastHackUi.cs:7`)은 열려 있는 동안 게임이 일시정지되지 않아 적의 공격에 그대로 노출된다.
- 추가로 `StopButton.cs:49`는 `BroadcastHackUi.cs:125`와 달리 `GameFlow.EscConsumedFrame = Time.frameCount;`를 설정하지 않아 실행 순서가 바뀔 경우 일시정지 메뉴와 충돌할 여지가 있다.
