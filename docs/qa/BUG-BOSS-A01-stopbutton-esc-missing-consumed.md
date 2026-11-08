검증: REJECTED (PauseMenu.CanOpen이 lockedAtFrameStart로 막아 같은 프레임에 열리지 않음: PauseMenu.cs:63,76. EscConsumedFrame 누락은 규약상 사소한 불일치일 뿐)

# BUG-BOSS-A01 — 정지 버튼(StopButton) 선택 창에서 Esc 취소 시 일시정지 메뉴(PauseMenu)가 동시에 열림

## 심각도
중간

## 재현 단계
1. STG-05 보스전의 3웨이브에서 전광판을 해킹하여 오른쪽(`x = 26`)의 정지 버튼(`StopButton`)을 활성화한다.
2. 정지 버튼 가까이에서 `E` 키를 눌러 엔딩 선택 창(`Phase.Choosing`, `[1]` / `[2]` / `Esc: back`)을 연다.
3. 엔딩 선택을 취소하기 위해 `Esc` 키를 누른다.

## 기대 결과
- 정지 버튼의 엔딩 선택 창만 닫히고(`phase = Phase.Idle`, `GameFlow.InputLocked = false`), 게임 플레이 상태로 돌아가야 한다. 일시정지 메뉴(`PauseMenu`)는 열리지 않아야 한다.

## 실제 결과
- 엔딩 선택 창이 닫히는 것과 **같은 프레임**에 일시정지 메뉴(`PauseMenu`)가 열린다.
- 원인:
  1. `PauseMenu`는 `[DefaultExecutionOrder(1000)]`으로 설정되어 일반 스크립트인 `StopButton`의 `Update()`보다 나중에 `Update()`가 실행된다 ([PauseMenu.cs:12](../../Assets/Scripts/UI/PauseMenu.cs#L12)).
  2. `StopButton.Update()`에서 `Phase.Choosing` 상태일 때 `Esc`를 누르면 `GameFlow.InputLocked = false;`만 설정하고 **`GameFlow.EscConsumedFrame = Time.frameCount;`를 설정하지 않는다** ([StopButton.cs:49](../../Assets/Scripts/Level/StopButton.cs#L49) — 반면 `BroadcastHackUi.UpdateDial()`은 [BroadcastHackUi.cs:125](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L125)에서 `GameFlow.EscConsumedFrame = Time.frameCount;`를 설정함).
  3. 또한 `PauseMenu.CanOpen`이 참조하는 `lockedAtFrameStart`는 이름과 달리 프레임 시작 시점이 아니라 `LateUpdate()`에서 갱신되는데 ([PauseMenu.cs:76](../../Assets/Scripts/UI/PauseMenu.cs#L76)), `OpenChoice()`가 호출된 프레임부터 `InputLocked == true`였던 동안에는 `PauseMenu.Update()`에서 `kb.escapeKey.wasPressedThisFrame`이 아니어도 `LateUpdate()`는 매 프레임 돌지만, 만약 같은 프레임이나 타이밍 이슈가 없더라도 `lockedAtFrameStart`에만 의존하고 `GameFlow.EscConsumedFrame`을 설정하지 않아 Esc 소비 규약(`GameFlow.EscConsumed`, [GameFlow.cs:8-10](../../Assets/Scripts/Core/GameFlow.cs#L8-L10))을 위반하고 있다. 특히 `OpenChoice()`가 열린 직후나 `Time.timeScale` / 실행 순서 변화 시 `EscConsumedFrame` 누락으로 인해 `PauseMenu` 방어가 불완전하다.

## 근거 파일 및 위치
- [StopButton.cs:49](../../Assets/Scripts/Level/StopButton.cs#L49) (`else if (kb.escapeKey.wasPressedThisFrame) { panel.SetActive(false); GameFlow.InputLocked = false; phase = Phase.Idle; }` — `GameFlow.EscConsumedFrame = Time.frameCount;` 누락)
- [BroadcastHackUi.cs:125](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L125) (`if (kb.escapeKey.wasPressedThisFrame) { GameFlow.EscConsumedFrame = Time.frameCount; Close(); return; }`)
- [GameFlow.cs:8-10](../../Assets/Scripts/Core/GameFlow.cs#L8-L10)
- [PauseMenu.cs:63-77](../../Assets/Scripts/UI/PauseMenu.cs#L63-L77)
