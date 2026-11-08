# BUG-BOSS-04: 사망 판정 직후(`Hurt`/`Dead`) 정지 버튼 및 해킹 UI 조작이 가능하여 엔딩 진행 중 씬이 강제 재시작됨

## 심각도
**높음**

## 재현 단계
1. `STG05` 3웨이브에서 전광판을 해킹하여 오른쪽(`x = 26, y = 2.5`)의 정지 버튼(`StopButton`)을 활성화한다(3웨이브 적들은 살아 있는 상태).
2. 정지 버튼 근처(`interactRange = 2.5u` 이내)에서 적의 공격이나 안드로이드의 황금 탄에 맞아 마지막 체력(5번째 피격, `hp == 0`, `health.IsDead == true`)을 잃는다.
3. 피격 직후 `State.Hurt`(0.25초) 또는 쓰러진 `State.Dead`(씬 리로드 전 1.5초 대기) 상태에서 `E` 키를 눌러 정지 버튼 선택 창을 열고, `1` 또는 `2`를 눌러 엔딩 대화를 시작한다.

## 기대 결과
- 플레이어 체력이 0이 되어 사망(`health.IsDead == true`)한 순간부터는 `PauseMenu`와 마찬가지로 정지 버튼(`StopButton`)이나 방송 해킹(`BroadcastHackUi`) 상호작용이 차단되어야 하며, 반대로 엔딩 선택/대화가 이미 시작되었다면 사망 타이머로 인해 엔딩 도중 씬이 리로드되지 않아야 한다.

## 실제 결과
1. `StopButton.Update()`와 `BroadcastHackUi.Update()`는 `PauseMenu.CanOpen`(`health == null || !health.IsDead`)과 달리 플레이어의 사망 여부(`health.IsDead` 또는 `player.CurrentState == PlayerController.State.Dead`)를 전혀 확인하지 않는다.
2. 플레이어가 5번째 피격을 당해 `health.IsDead == true`가 되어도 `State.Hurt`(0.25초) + `State.Dead` 리스폰 대기(`respawnDelay = 1.5초`) 동안 총 **1.75초간** `GameFlow.InputLocked`는 `false`이므로, 사망한 플레이어가 `E`를 눌러 정지 버튼의 엔딩 선택창(`Phase.Choosing`)을 열거나 엔딩 대사(`Phase.Playing`)를 재생할 수 있다(전광판 앞이라면 사망 상태에서 전광판 해킹도 가능).
3. 한편 `PlayerController.Update()`와 `RespawnManager.Update()`는 `GameFlow.InputLocked`를 확인하지 않고 `Time.unscaledTime` 기준으로 사망 후 1.5초 타이머를 진행하므로, **플레이어가 엔딩을 고르거나 엔딩 대사를 읽는 도중에 `RespawnManager`가 `STG05` 씬을 강제로 다시 불러와 엔딩이 끊기고 1웨이브로 초기화된다.**

## 코드 근거 (file:line)
- `Assets/Scripts/Level/StopButton.cs:39-44` — `near` 판정에서 `player != null && !GameFlow.InputLocked && Vector2.Distance(...) <= interactRange`만 검사하고 `PlayerHealth.IsDead` 및 `PlayerController.CurrentState`를 검사하지 않음
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:55-66` — 해킹 다이얼 열기 및 유지 조건에서 플레이어 사망 여부를 검사하지 않음
- `Assets/Scripts/UI/PauseMenu.cs:63` — 대조군: `PauseMenu.CanOpen`은 `(health == null || !health.IsDead)`를 검사함
- `Assets/Scripts/Player/PlayerController.cs:101-148, 276-277` — `GameFlow.InputLocked` 중에도 `State.Hurt` → `State.Dead` 전이가 그대로 진행됨
- `Assets/Scripts/Checkpoint/RespawnManager.cs:60-69` — `GameFlow.InputLocked`를 확인하지 않고 `Time.unscaledTime - deadSince >= respawnDelay` 경과 시 즉시 `SceneManager.LoadScene` 실행
