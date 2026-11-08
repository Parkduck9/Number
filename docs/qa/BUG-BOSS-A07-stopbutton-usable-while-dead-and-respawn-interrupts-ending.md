검증: CONFIRMED (중복: BUG-BOSS-C01-dead-player-opens-stop-button.md. StopButton.cs:40-44에 사망 확인 없음, RespawnManager.cs:62-68이 1.5초 후 씬 리로드)

# BUG-BOSS-A07 — 보스전(STG-05) 정지 버튼 해금 후에도 남아 있는 적과 탄환이 계속 플레이어를 공격하여 엔딩 선택 직전 사망 및 진행 꼬임 가능

## 심각도
중간

## 재현 단계
1. STG-05 보스전 3웨이브가 시작되어 안드로이드 2명과 시민 3명이 소환된다.
2. 적들을 모두 쓰러뜨리지 않은 상태에서(규칙상 허용됨: [BossFight.cs:59](../../Assets/Scripts/Boss/BossFight.cs#L59), [docs/design/impl/boss.md:17](../../docs/design/impl/boss.md#L17)) 전광판을 해킹하여 정지 버튼(`StopButton`)을 활성화한다.
3. 안드로이드가 공격 또는 황금 탄 3발(`FireFan`)을 발사하는 도중 정지 버튼(`x = 26`)으로 달려가 `E` 키를 누르거나, 정지 버튼 앞에서 피격당해 체력이 0이 되는 순간 `E` 키를 누른다.

## 기대 결과
- 정지 버튼이 열렸거나 정지 버튼을 조작해 엔딩을 선택하는 동안에는 플레이어가 사망하여 씬이 강제 리로드되는 일이 없어야 하며, 사망 상태(`health.IsDead`)에서는 정지 버튼 선택 창을 열 수 없어야 한다.

## 실제 결과
1. **`StopButton`에서 사망 체크 누락:** `StopButton.Update()`의 `Phase.Idle` 판정은 `player != null && !GameFlow.InputLocked && Vector2.Distance(...) <= interactRange`만 확인할 뿐, **`player.GetComponent<PlayerHealth>().IsDead`(또는 `player.CurrentState == PlayerController.State.Dead`)를 확인하지 않는다** ([StopButton.cs:40-44](../../Assets/Scripts/Level/StopButton.cs#L40-L44)).
2. 따라서 플레이어가 정지 버튼 근처에서 적의 공격이나 날아오는 탄환(`EnemyProjectile`)에 맞아 사망(`State.Dead`)한 뒤 `RespawnManager`가 1.5초(`respawnDelay`)를 기다리는 동안 ([RespawnManager.cs:61-68](../../Assets/Scripts/Checkpoint/RespawnManager.cs#L61-L68)), 플레이어가 `E` 키를 누르면 **죽은 상태에서 엔딩 선택 창이 열리고 엔딩 대화가 시작되다가, 1.5초 타이머가 만료되는 순간 `RespawnManager`가 씬을 강제로 다시 로드하여 엔딩 대화가 중간에 끊기고 1웨이브로 초기화**된다! (`RespawnManager.Update()`는 `GameFlow.InputLocked`를 확인하지 않고 `Time.unscaledTime`으로 1.5초 뒤 무조건 씬을 리로드함 — [RespawnManager.cs:60-69](../../Assets/Scripts/Checkpoint/RespawnManager.cs#L60-L69).)
3. 반대로 엔딩 선택 창에서 `Esc`로 취소를 누르는 순간(`GameFlow.InputLocked = false`), 버튼 바로 옆까지 쫓아온 안드로이드들의 근접 공격이나 탄환에 즉시 피격당해 사망할 수 있다.

## 근거 파일 및 위치
- [StopButton.cs:40-45](../../Assets/Scripts/Level/StopButton.cs#L40-L45) (`player` 사망 여부 미확인)
- [RespawnManager.cs:60-69](../../Assets/Scripts/Checkpoint/RespawnManager.cs#L60-L69) (`GameFlow.InputLocked` 중에도 `Time.unscaledTime`으로 씬 리로드 강행)
- [BossFight.cs:145-150](../../Assets/Scripts/Boss/BossFight.cs#L145-L150) (`Unlock()` 시 스폰 중단 외에 잔존 적/탄환 정리 없음)
