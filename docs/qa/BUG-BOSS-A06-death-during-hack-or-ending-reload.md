검증: CONFIRMED (중복: C01, C04, A07-stopbutton-usable-while-dead. 사망 확인 누락 BroadcastHackUi.cs:56-63, StopButton.cs:40-44. 번호 A06이 두 파일에 겹침: A06-shockwave-locked와 별개)

# BUG-BOSS-A06: 보스전 중 플레이어 사망(피격/사망 대기) 상태에서 전광판 해킹 및 정지 버튼 엔딩 진입이 가능하고, 엔딩 도중 씬이 강제 리로드됨

## 심각도
높음 (High)

## 재현 단계
1. `STG05` 보스전 3웨이브에서 체력이 1 남은 상태로 전광판(`BossBoard`, `x = 15`) 해킹 다이얼을 열거나, 전광판 해킹 후 해금된 정지 버튼(`StopButton`, `x = 26`) 근처로 이동한다.
2. **시나리오 A (사망 중 해킹):** 해킹 다이얼이 열린 상태에서(또는 전광판 반경 `3u` 안에서) 적의 공격이나 투사체에 맞아 체력이 `0`(`PlayerHealth.IsDead == true`)이 된 직후, 리스폰 대기 시간(`Hurt` 0.25초 + `Dead` 1.5초 = 총 1.75초) 동안 `E` 키를 누른다.
3. **시나리오 B (사망 중 정지 버튼 및 엔딩 강제 중단):** 정지 버튼이 열려도 3웨이브 적(안드로이드 2기 + 시민 3명)과 날아오는 황금 탄(`EnemyProjectile`)은 사라지지 않는다. 정지 버튼 반경(`2.5u`) 안에서 마지막 5번째 피격을 당해 `State.Hurt`(0.25초) 또는 `State.Dead`(1.5초) 상태가 되었을 때(혹은 피격과 같은 프레임에) `E` 키를 눌러 엔딩 선택 창을 열고 `1` 또는 `2`를 눌러 엔딩 대화를 시작한다.

## 기대 결과
- 플레이어의 체력이 `0`(`PlayerHealth.IsDead == true` 또는 `PlayerController.State.Dead`)이 되면 열려 있던 해킹 다이얼이 즉시 닫히고, 전광판 해킹(`BroadcastHackUi`)과 정지 버튼(`StopButton`) 상호작용이 차단되어야 한다.
- 반대로 플레이어가 정지 버튼을 눌러 이미 엔딩 선택(`Phase.Choosing`) 또는 엔딩 대화(`Phase.Playing`)에 진입했다면, 사망 리스폰 타이머(`RespawnManager`)가 엔딩 도중 씬을 다시 불러와 엔딩을 끊지 않아야 한다.

## 실제 결과
- **시나리오 A:** `BroadcastHackUi.Update()`(`Assets/Scripts/Broadcast/BroadcastHackUi.cs:56-65`)와 `UpdateDial()`(`BroadcastHackUi.cs:121-131`)은 `GameFlow.InputLocked`만 확인하고 `PlayerHealth.IsDead`나 `PlayerController.CurrentState == PlayerController.State.Dead`를 전혀 확인하지 않는다. 따라서 사망 상태에서도 해킹 다이얼이 닫히지 않고(또는 시체 상태에서 새로 열 수 있고) `E` 키로 해킹을 확정해 `BossFight.Unlock()`(`Assets/Scripts/Boss/BossFight.cs:59`)을 발동시킬 수 있다.
- **시나리오 B:** `StopButton.Update()`(`Assets/Scripts/Level/StopButton.cs:40-44`) 역시 `player != null && !GameFlow.InputLocked`만 확인할 뿐 플레이어의 사망 여부(`IsDead`)를 확인하지 않는다. 플레이어가 5번째 피격을 당한 직후(`State.Hurt` 0.25초 또는 `State.Dead` 1.5초 동안) `E`를 누르면 `OpenChoice()`가 호출되어 엔딩 선택 창이 열리고 `1`/`2`로 엔딩 대화가 재생된다. 그러나 `PlayerController.Update()`(`Assets/Scripts/Player/PlayerController.cs:108-148`)와 `RespawnManager.Update()`(`Assets/Scripts/Checkpoint/RespawnManager.cs:60-69`)는 `GameFlow.InputLocked`를 확인하지 않으며 `Time.unscaledTime`으로 1.5초를 세기 때문에, **엔딩 선택 중이거나 엔딩 대화가 나오는 도중에 `SceneManager.LoadScene("STG05")`가 호출되어 엔딩이 강제로 끊기고 1웨이브부터 다시 시작된다.**

## 근거 (file:line)
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:56-65, 121-131` — `GameFlow.InputLocked`만 검사하고 플레이어 사망(`PlayerHealth.IsDead` / `PlayerController.State.Dead`) 미검사
- `Assets/Scripts/Level/StopButton.cs:39-45` — `Phase.Idle`에서 플레이어 사망 상태 미검사
- `Assets/Scripts/Player/PlayerController.cs:108-148, 276-277` — 5번째 피격 시 `hp = 0`(`IsDead = true`)이 되지만 0.25초 동안 `State.Hurt`를 거쳐 `State.Dead`로 전이하며, `GameFlow.InputLocked` 중에도 그대로 진행됨
- `Assets/Scripts/Checkpoint/RespawnManager.cs:60-69` — `GameFlow.InputLocked`를 확인하지 않고 `Time.unscaledTime - deadSince >= respawnDelay`(1.5초) 경과 시 무조건 씬을 리로드함
