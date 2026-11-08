검증: CONFIRMED 일부 (LevelTransition.cs:22-28 사망 가드 없음 확인; 체크포인트 정적 누수는 PLAUSIBLE; 범위상 FLOW 영역과 겹침; 심각도 '중간' 권장)

# BUG-COMBAT-A13 — 사망 판정 후 넉백·대기 중 클리어/해킹 상호작용 허용 및 `RespawnManager` 무배치 씬 재시작 시 정적 체크포인트 오염

## 근거 파일 및 위치
- `Assets/Scripts/Player/PlayerController.cs:145-147, 276-281`
- `Assets/Scripts/Checkpoint/RespawnManager.cs:11-15, 31-35, 45-50, 62-68`
- `Assets/Scripts/UI/PauseMenu.cs:121-131`
- `Assets/Scripts/UI/TitleScreen.cs:10-15`
- `Assets/Scripts/Core/GameFlow.cs:5-11`
- `Assets/Scripts/Core/PrototypeDebug.cs:12-16`
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:55-64`
- `Assets/Scripts/Level/LevelTransition.cs:22-28`

## 재현 단계
1. **케이스 A (사망 넉백 및 `Dead` 대기 중 상호작용):**
   - 체력이 1 남은 상태에서 전광판(`BroadcastBoard`) 또는 구간/스테이지 출구(`LevelTransition`) 근처에서 적에게 맞아 체력이 0(`health.IsDead == true`)이 되게 한다.
   - 피격 넉백(`State.Hurt` 0.25초, 속도 `±5, 4`)으로 출구 트리거에 밀려 들어가거나, `State.Dead` 진입 후 리스폰 대기 시간(`respawnDelay = 1.5초`) 동안 `E` 키를 눌러 전광판 해킹 다이얼을 조작·확정한다 (특히 STG-05 보스전 3웨이브).
2. **케이스 B (`RespawnManager`가 없는 씬에서 일시정지 '체크포인트에서 재시작' 호출 시 정적 상태 누수):**
   - `STG01`에서 중간 체크포인트(`CP-B0`)에 도달한 뒤 일시정지 메뉴 → `타이틀로`(`ToTitle`) → 새 게임 시작(`STG00`)으로 진입한다.
   - `RespawnManager`가 없는 `STG00`(`Stg00Builder.cs:64`)이나 테스트 씬(`Prototype`)에서 일시정지 메뉴의 `체크포인트에서 재시작`(`RestartFromCheckpoint`)을 한 번 누른 뒤 `STG01`로 진입한다.
   - 또는 `Prototype` 씬에서 대화 중(`GameFlow.InputLocked == true`)일 때 `R` 키(`PrototypeDebug.cs:15`)로 씬을 재시작한다.

## 기대 결과
- 체력이 0(`health.IsDead == true`)이 된 순간부터는 넉백으로 출구(`LevelTransition`)나 대화 트리거에 닿아도 다음 스테이지로 넘어가거나 대화가 시작되지 않아야 하며, 사망 상태에서는 전광판 해킹(`BroadcastHackUi`)이 닫히고 입력이 차단되어야 한다.
- 타이틀 복귀나 새 게임 시작, 또는 `RespawnManager`가 없는 씬에서의 재시작 시 이전 플레이의 정적 체크포인트 플래그(`respawnPending`, `hasRecord`)나 `GameFlow.InputLocked`가 남아서 다음 씬의 시작 위치나 입력을 오염시키지 않아야 한다.

## 실제 결과
1. **사망 후 1.75초(Hurt 0.25초 + Dead 1.5초) 동안 트리거 및 해킹 가능:**
   - `Checkpoint.OnTriggerEnter2D`(`Checkpoint.cs:23`)에는 `if (hp.IsDead) return;` 가드가 있으나, `LevelTransition.OnTriggerEnter2D`(`LevelTransition.cs:22-28`)와 `LudditeMeeting.OnTriggerEnter2D`(`LudditeMeeting.cs:15-22`)에는 사망 가드가 없다. 치명타 넉백으로 Goal 출구에 닿으면 **죽은 상태로 다음 스테이지가 로드**된다.
   - `BroadcastHackUi.Update()`(`BroadcastHackUi.cs:55-64`)도 `health.IsDead`나 `player.CurrentState == State.Dead`를 확인하지 않으며 사망 중에는 `GameFlow.InputLocked`가 `false`이므로, 죽어 쓰러진 뒤 1.5초 동안 `E` 키로 해킹 다이얼을 열어 방송 숫자를 바꿀 수 있고 STG-05 3웨이브에서는 **사망 상태에서 보스 정지 버튼(`BossFight.Unlock`)을 해금**할 수 있다.
2. **정적 상태(`RespawnManager.respawnPending`, `GameFlow.InputLocked`) 누수:**
   - `PauseMenu.RestartFromCheckpoint()`(`PauseMenu.cs:121-125`)가 호출하는 `RespawnManager.RestartFromCheckpoint()`(`RespawnManager.cs:31-35`)는 무조건 `respawnPending = true`로 설정한다. 그러나 `STG00`이나 `Prototype`처럼 `RespawnManager`가 없는 씬에서는 `Start()`가 실행되지 않아 `respawnPending = true`가 정적 메모리에 그대로 남는다.
   - 게다가 `PauseMenu.ToTitle()`과 `TitleScreen.Start()`가 `RespawnManager`의 정적 기록(`hasRecord`, `recordScene`, `recordPos`, `respawnPending`)을 초기화하지 않으므로, 이후 `STG01`에 진입하면 새 게임인데도 이전 판의 중간 체크포인트(`CP-B0`)로 순간이동한다 (`RespawnManager.cs:45`).
   - 또한 `GameFlow`(`GameFlow.cs:5-11`)에는 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 초기화가 없고 `PrototypeDebug.Update()`(`PrototypeDebug.cs:14-15`)의 `R` 재시작도 `GameFlow.InputLocked`와 `Time.timeScale`을 초기화하지 않아, 대화/히트스톱 중 `R` 재시작 시 조작이 영구 잠긴다.

## 심각도
높음
