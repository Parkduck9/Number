검증: CONFIRMED (A는 C05와 중복: BUG-BOSS-C05-wave3-skipped-by-announce-hack.md. B의 Esc 부분은 A01과 같아 REJECTED, Title 없음 가정은 Build Settings에 있어 추측)

# BUG-BOSS-A10: 3웨이브 예고/소환 중 해킹 시 적 소환이 즉시 중단되며, 정지 버튼 엔딩 완료/취소 처리의 상태 관리가 취약함

## 심각도
중간 (Medium)

## 재현 단계
1. **시나리오 A (3웨이브 소환 중단):** `STG05`에서 2웨이브 적을 모두 기절시킨 뒤 전광판(`x = 15`) 아래에 서서 기다린다. 1.5초 뒤 `"WAVE 3 - LUCKY 0"` 문구가 뜨는 2초간의 예고(`State.Announce`) 동안(또는 첫 적만 소환된 `State.Spawning` 도중) 즉시 `E` → `E`를 눌러 전광판을 해킹한다.
2. **시나리오 B (정지 버튼 취소/종료 상태):**
   - 정지 버튼이 해금되는 순간(또는 대화 종료와 같은 프레임) 정지 버튼 앞에서 `E`를 누르거나, 선택 창(`Phase.Choosing`)에서 `Esc`로 취소한다.
   - `Title` 씬이 Build Settings에 없거나 로드되지 않는 환경(예: `NUMBER/Build STG-05`만 실행한 직후)에서 엔딩 1을 끝까지 본 뒤 다시 정지 버튼에 다가간다.

## 기대 결과
- **시나리오 A:** 3웨이브가 시작된 뒤 전광판을 해킹해 정지 버튼이 열리더라도, 3웨이브의 적(안드로이드 2기 + 시민 3명)은 정해진 간격(`0.6초`)대로 끝까지 소환되어 정지 버튼으로 가는 길을 막아야 한다.
- **시나리오 B:**
  - `StopButton` 선택 창에서 `Esc`로 취소할 때 `GameFlow.EscConsumedFrame = Time.frameCount`를 설정해 일시정지 메뉴와의 충돌을 명시적으로 막아야 한다.
  - `StopButton`이 비활성 상태에서 활성화될 때 첫 프레임의 `lockedLastFrame`이 실제 `GameFlow.InputLocked` 상태를 반영해야 한다.
  - `StageFlow.LoadTitle()`이 실패해 현재 씬에 남게 될 경우(`if the title scene is missing we stay here, so give control back`), `phase`가 `Phase.Done`에 영구히 갇혀 두 번째 엔딩을 영영 선택할 수 없게 되지 않고 `Phase.Idle`로 복귀해야 한다.

## 실제 결과
- **시나리오 A:** `BossFight.Update()`(`Assets/Scripts/Boss/BossFight.cs:59`)는 `waveIndex == waves.Length - 1`이기만 하면 `State.Announce`(예고 2초)나 `State.Spawning` 중에도 `Unlock()`을 호출한다. `Unlock()`(`BossFight.cs:145-150`)은 `Enter(State.Unlocked)`를 호출해 상태 머신을 `State.Unlocked`로 바꿔 버리고, `switch (state)`(`BossFight.cs:61-89`)에는 `State.Unlocked` 케이스가 없으므로 **3웨이브 예고 2초 동안 해킹하면 3웨이브 적이 단 1마리도 소환되지 않은 빈 방에서 바로 클리어되며**, 소환 도중 해킹하면 나머지 적들의 소환이 영구히 중단된다.
- **시나리오 B:**
  - `StopButton.Update()`(`Assets/Scripts/Level/StopButton.cs:49`)에서 `Esc`로 선택을 취소할 때 `GameFlow.EscConsumedFrame = Time.frameCount`를 설정하지 않는다(`BroadcastHackUi.cs:125`와 불일치).
  - `StopButton` 오브젝트는 처음에 비활성(`SetActive(false)`) 상태였다가 해금 시 켜지므로(`BossFight.cs:148`), 켜진 첫 프레임에는 `lockedLastFrame`이 `false`(`StopButton.cs:29`)로 시작한다.
  - `StopButton.Finish()`(`StopButton.cs:80-85`)는 `Title` 씬이 없을 때 플레이어에게 조작권을 돌려주면서도(`GameFlow.InputLocked = false`) `phase = Phase.Done`으로 고정해 버려, 정지 버튼이 영구히 먹통이 되어 다른 엔딩을 다시 선택할 수 없다.

## 근거 (file:line)
- `Assets/Scripts/Boss/BossFight.cs:59, 68-78, 145-150` — `Unlock()`이 `state`를 `State.Unlocked`로 덮어씌워 진행 중이던 `State.Announce` / `State.Spawning`을 중단시킴
- `Assets/Scripts/Level/StopButton.cs:29, 37-56, 80-85` — `Esc` 취소 시 `GameFlow.EscConsumedFrame` 미설정, 활성화 첫 프레임 `lockedLastFrame` 초기화 누락, `LoadTitle()` 실패 시 `Phase.Done` 고착
