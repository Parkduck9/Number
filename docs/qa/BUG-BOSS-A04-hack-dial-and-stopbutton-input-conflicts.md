검증: REJECTED (다이얼 안내와 확정 키는 E. Enter 공격은 의도된 바인딩. 정지 버튼은 x=26, 전광판 x=15로 범위가 겹치지 않음. 대화 클릭 부분은 추측)

# BUG-BOSS-A04 — 정지 버튼(StopButton)과 해킹 다이얼(BroadcastHackUi) 모두 키보드/게임패드 조작 시 좌클릭·Enter·화살표 등 플레이어 행동이 동시에 발동됨

## 심각도
중간

## 재현 단계
1. **해킹 다이얼(`BroadcastHackUi`) 사례:**
   - STG-05 전광판(`x = 15`) 아래에서 `E` 키를 눌러 해킹 다이얼을 연다.
   - 다이얼 안내에 따라 위/아래 화살표 키나 숫자 키를 누르거나, 실수로 `Enter` 키를 눌러 확정하려 한다.
2. **정지 버튼(`StopButton`) 엔딩 대화 사례:**
   - 정지 버튼에서 `1` 또는 `2`를 눌러 엔딩 대화(`DialoguePlayer` talk 모드)를 시작한다.
   - 마지막 대사 줄에서 **마우스 좌클릭**으로 대사를 닫는다 ([DialoguePlayer.cs:103-104](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L103-L104)).

## 기대 결과
- 해킹 다이얼 조작 중이거나 대화창을 마우스 좌클릭으로 닫을 때 불필요한 플레이어 공격이나 UI 오동작이 발생하지 않아야 한다.

## 실제 결과
1. `BroadcastHackUi`가 열려 있을 때 플레이어 입력이 잠기지 않는데(`GameFlow.InputLocked == false`), `InputSystem_Actions.inputactions`에서 **`<Keyboard>/enter`가 `Player/Attack`에 바인딩**되어 있어 ([InputSystem_Actions.inputactions:332](../../Assets/Settings/InputSystem_Actions.inputactions#L332)), 사용자가 다이얼에서 숫자를 고른 뒤 직관적으로 `Enter`를 누르면 다이얼은 확정되지 않고 캐릭터가 허공에 공격을 휘두른다.
2. 또한 `BroadcastHackUi`가 열린 상태에서 `StopButton`도 가까이 있다면(예: 커스텀 배치 또는 범위 내), 두 스크립트 모두 `Keyboard.current.eKey.wasPressedThisFrame`을 독립적으로 읽고 `BroadcastHackUi`는 `InputLocked`를 걸지 않기 때문에 `E` 키 입력이 두 컴포넌트에서 동시에 처리될 수 있다 ([BroadcastHackUi.cs:63](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L63), [BroadcastHackUi.cs:126](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L126), [StopButton.cs:44](../../Assets/Scripts/Level/StopButton.cs#L44)).
3. `DialoguePlayer`의 talk 모드 마지막 줄에서 좌클릭으로 대화를 닫으면 `DialoguePlayer.Update()`에서 `GameFlow.InputLocked = false;`가 된다 ([DialoguePlayer.cs:118](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L118)). 만약 `StopButton`이 아닌 일반 대화(STG-00, STG-03 등)에서 좌클릭으로 마지막 줄을 넘기면, 같은 프레임 또는 공격 버퍼를 통해 플레이어가 즉시 공격을 시전할 수 있다 (`PlayerInputReader`는 `[DefaultExecutionOrder(-100)]`이라 해당 프레임은 막히지만, `StopButton.Play()`에서 대화 시작 프레임에 `TalkInputGuard = 0.25f` 이전에 연타 시 주의 필요).

## 근거 파일 및 위치
- [InputSystem_Actions.inputactions:330-339](../../Assets/Settings/InputSystem_Actions.inputactions#L330-L339) (`<Keyboard>/enter` -> `Attack`)
- [BroadcastHackUi.cs:107-131](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L107-L131)
- [StopButton.cs:44-56](../../Assets/Scripts/Level/StopButton.cs#L44-L56)
- [DialoguePlayer.cs:103-119](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L103-L119)
