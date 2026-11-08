검증: CONFIRMED (BroadcastHackUi.cs:48-66 Update에 플레이어 사망 검사 없음, 코드 확인. 사망 후 재시작 대기 1.5초 동안만 영향이라 낮음~중간)

# BUG-ENEMY-A17: 플레이어 사망(State.Dead) 후 재시작 대기 시간 동안 전광판 해킹 다이얼 조작이 가능하고 적 AI가 시체에 돌격(Charge)하는 문제

## 재현 단계
1. 전광판(`BroadcastBoard`) 반경 `3u` 안에서 해킹 다이얼을 열어 둔 채(`dialBoard != null`) 다가온 적의 공격에 맞아 체력이 0이 되어 사망(`PlayerController.State.Dead`)하거나, 전광판 반경 `3u` 안에서 사망한 뒤 재시작 대기 시간(`RespawnManager.respawnDelay = 1.5초`) 동안 `E` 키와 숫자 키를 눌러 본다.
2. 또한 플레이어가 적의 연타 첫 타격에 맞아 사망하면서 피격 넉백(`knockback.x = 5f`, `Assets/Scripts/Player/PlayerController.cs:280`)으로 적의 공격 사거리(`attackRangeX = 1.8f`) 밖으로 밀려났을 때 적의 다음 행동을 관찰한다.
3. 플레이어 오브젝트가 파괴되어 `player == null`이 되는 순간 적이 `State.Charge` 중일 때 적의 이동 상태를 확인한다.

## 기대 결과
- 플레이어가 사망(`State.Dead` / `health.IsDead`)하면 모든 플레이어 상호작용 UI(해킹 다이얼 및 `[E] 해킹` 프롬프트)가 즉시 닫히고 입력이 차단되어야 하며, 적 AI도 사망한 플레이어를 상대로 추격·돌격 패턴을 이어가지 않아야 한다.

## 실제 결과
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:50-66`의 `Update()`는 `GameFlow.InputLocked`만 검사하고 `player.CurrentState == PlayerController.State.Dead` 여부를 전혀 확인하지 않는다(사망 시 `GameFlow.InputLocked`는 `false` 유지). 그 결과 사망 후 재시작되기 전 1.5초 동안 열린 해킹 다이얼로 숫자를 돌려 해킹을 확정(`UpdateDial`, `126-130줄`)할 수 있고, 다이얼이 닫혀 있었더라도 시체 위에 `[E] 해킹` 프롬프트(`65줄`)가 표시되어 죽은 상태에서 `E` 키로 해킹 창을 열 수 있다.
- `Assets/Scripts/Enemy/TestEnemy.cs:121`의 `Update()`도 플레이어 사망 상태를 확인하지 않으며, `PlayerController.ReceiveEnemyHit`(`Assets/Scripts/Player/PlayerController.cs:256`)는 `state == State.Dead`일 때 `HitResult.Dodged`를 반환한다. 이로 인해 플레이어가 연타 도중 사망해 사거리 밖으로 밀려나면 적이 다음 타격(`DoStrike`, `TestEnemy.cs:332`)에서 이를 "플레이어가 회피하여 사거리 밖으로 도망침"으로 인식해 시체를 향해 `State.Charge` 고속 돌격을 시전한다.
- 아울러 `TestEnemy.cs:121`에서 `player == null`이면 `UpdateState()` 호출을 건너뛰지만, `FixedUpdate()`(`TestEnemy.cs:141`: `if (state == State.Charge) { vx = facing * chargeSpeed; chasing = true; }`)는 `player != null`을 확인하지 않아 `State.Charge` 도중 `player`가 `null`이 되면 `chargeMaxTime`이 지나도 영구히 멈추지 않고 돌진한다.

## 심각도
중간
