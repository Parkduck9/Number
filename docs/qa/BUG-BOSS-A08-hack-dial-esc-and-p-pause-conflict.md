검증: PLAUSIBLE (B는 A02와 중복·CONFIRMED. A: near!=dialBoard 프레임의 Esc가 EscConsumed를 못 남기는 것은 코드상 사실이나 전광판 y=3.5라 점프하면 오히려 가까워져 재현 경로가 약함, 낮음)

# BUG-BOSS-A08: 보스방에서 해킹 다이얼이 열린 채 점프/넉백/이동 중 `Esc`를 누르면 일시정지 메뉴가 열리고, `P`를 누르면 다이얼이 강제 취소됨

## 심각도
중간 (Medium)

## 재현 단계
1. `STG05` 보스전에서 `BossBoard`(`(15, 3.5)`) 근처로 가 `E` 키를 눌러 해킹 다이얼(`BroadcastHackUi`)을 연다.
2. **시나리오 A (`Esc` 입력 시 일시정지 메뉴 동시 오픈):** 해킹 다이얼이 열린 상태에서는 플레이어 이동·점프와 적의 공격·넉백이 그대로 작동한다. 전광판 가장자리(예: `x = 12.7` 부근)에서 적의 공격을 피하려고 점프(`W`)하거나 적에게 맞아 넉백되어 전광판 루트 `(15, 3.5)`와의 2D 거리가 `3u`를 막 벗어나는 프레임에 다이얼을 닫으려고 `Esc`를 누른다.
3. **시나리오 B (`P` 키 입력 시 다이얼 강제 취소):** 해킹 다이얼이 열린 상태에서 숫자를 고르던 중 일시정지 단축키 `P`를 누른다.

## 기대 결과
- **시나리오 A:** 해킹 다이얼이 열려 있던 프레임에 `Esc`를 눌렀다면 다이얼만 닫혀야 하며(`GameFlow.EscConsumedFrame = Time.frameCount`), 같은 프레임에 일시정지 메뉴(`PauseMenu`)가 열리지 않아야 한다. 또한 전광판 상호작용 거리 판정이 점프 시 화면(`y = 11`) 쪽으로 올라갔다는 이유로 끊기지 않아야 한다.
- **시나리오 B:** 해킹 다이얼이 열려 있을 때 `P`로 일시정지를 열면 일시정지 해제 후에도 다이얼이 유지되거나, 다이얼이 열린 동안에는 일시정지가 열리지 않도록 일관되게 처리되어야 한다.

## 실제 결과
- **시나리오 A:** `BroadcastHackUi.Update()`(`Assets/Scripts/Broadcast/BroadcastHackUi.cs:56-60`)는 `if (GameFlow.InputLocked || near != dialBoard) Close(); else UpdateDial();` 순서로 분기한다.
  - `Nearest(interactRange)`(`BroadcastHackUi.cs:69-81`)가 가로 거리가 아닌 루트 좌표 `(15, 3.5)`(`Assets/Editor/Stg05Builder.cs:65`)와의 2D 거리(`Vector2.Distance`)를 재기 때문에, 플레이어가 바닥(`y = 3`)에서 점프(최고점 `y ≈ 5.5`)하면 오히려 루트와의 거리가 멀어져 `near != dialBoard`가 되기 쉽다.
  - 점프·이동·피격 넉백으로 `near != dialBoard`가 된 프레임에 플레이어가 `Esc`를 누르면, 58번째 줄의 `Close()`만 호출되고 `UpdateDial()`(`125번째 줄`)은 건너뛰어져 **`GameFlow.EscConsumedFrame = Time.frameCount`가 설정되지 않는다.** 그 결과 같은 프레임의 `PauseMenu.Update()`(`Assets/Scripts/UI/PauseMenu.cs:63, 70`)가 `Esc` 입력을 소비되지 않은 것으로 판단해 일시정지 메뉴를 열어 버린다.
- **시나리오 B:** `PauseMenu.Update()`(`PauseMenu.cs:63, 68`)는 `P` 키 입력 시 `CanOpen`을 확인하는데, 해킹 다이얼은 `GameFlow.InputLocked`를 켜지 않으므로 `CanOpen`이 `true`가 되어 일시정지 메뉴가 열리고 `GameFlow.InputLocked = true`(`PauseMenu.cs:82`)가 설정된다. 그러면 다음 프레임에 `BroadcastHackUi.Update()`(`58번째 줄`: `if (GameFlow.InputLocked || near != dialBoard) Close();`)가 실행되어 **열려 있던 해킹 다이얼과 고르던 숫자가 강제로 닫히며 취소된다.**

## 근거 (file:line)
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:56-60` — `near != dialBoard`일 때 `UpdateDial()`을 거치지 않고 `Close()`만 호출하여 같은 프레임의 `Esc` 입력이 `GameFlow.EscConsumedFrame`에 기록되지 않음
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:69-81, 125` — `Vector2.Distance`로 `(15, 3.5)`와의 2D 거리를 계산해 점프 시 반경(`3f`)을 쉽게 벗어남
- `Assets/Scripts/UI/PauseMenu.cs:63, 68-70, 82` — 해킹 다이얼 오픈 여부를 확인하지 않고 `P` 키로 일시정지를 열어 `GameFlow.InputLocked = true`를 설정함으로써 다음 프레임에 해킹 다이얼을 강제 종료시킴
