# BUG-BOSS-01: 적 간 물리 충돌 및 기절/정지 적 콜라이더 유지로 인한 수직 스태킹·무적 적(웨이브 클리어 불가 소프트락)

## 심각도
**높음**

## 재현 단계
1. `Assets/Scenes/STG05.unity`에서 보스전을 시작하여 1웨이브~3웨이브를 진행한다.
2. **케이스 A (기절한 적 위에 소환·안착 시 웨이브 클리어 불가 소프트락):**
   - 1웨이브 또는 2웨이브에서 적을 소환 지점(`Spawn-L` `x = 2` 또는 `Spawn-R` `x = 28`) 근처에서 기절(`TestEnemy.State.Stunned`)시킨다.
   - 다음 적 또는 다음 웨이브의 적(특히 2웨이브의 원거리 벌레 대역 `Template-Bug`, `attackRangeX = 9`)이 같은 `x` 위치(`(2, 14)` 또는 `(28, 14)`)에서 떨어져 바닥(`y = 3`)에 누워 있는 기절한 적의 `BoxCollider2D`(`0.9 × 2.0`) 머리 위(`y = 5`)에 착지한다.
   - 플레이어가 바닥(`y = 3`)에서 해당 적에게 접근해 공격을 시도하거나 적의 공격을 기다린다.
3. **케이스 B (3웨이브 시민 자동 수직 스태킹):**
   - 3웨이브(`broadcastNumber = 0`, 안드로이드 2 + 시민 3)가 시작되면 `SpawnNext()`가 번갈아 가며 적을 소환한다.
   - `spawned = 2`(시민 1)가 `Spawn-L (2, 14)`에서 떨어져 `State.Frozen`(`vx = 0`) 상태로 `(2, 3)`에 멈춘다.
   - `spawned = 4`(시민 3)가 동일한 `Spawn-L (2, 14)`에서 떨어져 `State.Frozen`(`vx = 0`) 상태로 시민 1의 콜라이더 위에 충돌해 `(2, 5)` 높이에 2층으로 쌓인다.
4. **케이스 C (기절·정지 적에 의한 지상 진로 막힘):**
   - 바닥에 기절한 적(`State.Stunned`) 또는 굳은 시민(`State.Frozen`)이 플레이어와 활동 중인 근접 적 사이에 위치할 때, 근접 적이 플레이어 쪽으로 이동을 시도한다.

## 기대 결과
- 이미 기절한 적(`State.Stunned`)이나 다른 적과 물리적으로 겹치거나 막히지 않아야 하며, 천장에서 소환된 적이 다른 적 머리 위(`y = 5`)에 쌓여 공격 불능·무적 상태에 빠지지 않고 모든 웨이브가 정상적으로 클리어 가능해야 한다.

## 실제 결과
1. **적끼리 물리 충돌 발생 및 기절 적의 벽화:**
   - `Stg05Builder.SetupLayers()`는 `PlayerLayer(7)`와 `EnemyLayer(8)` 간 충돌만 무시(`Physics2D.IgnoreLayerCollision(PlayerLayer, EnemyLayer, true)`)하며, `EnemyLayer(8)`끼리의 충돌은 켜져 있다.
   - `TestEnemy`는 `State.Stunned` 진입 후에도 `BoxCollider2D(0.9 × 2.0)`가 활성화된 채 매 `FixedUpdate`마다 `rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y)`(`vx = 0`)로 고정되며, `BossFight.StartWave()`도 `alive.Clear()`만 호출할 뿐 이전 웨이브의 기절한 적 오브젝트를 제거하지 않는다.
   - 이로 인해 기절한 적은 바닥에서 움직이지 않는 높이 2u짜리 물리 장벽이 되어 다른 지상 적의 이동을 가로막는다.
2. **수직 스태킹(`y = 5`) 시 공격·피격 불가 소프트락 (웨이브 영구 미클리어):**
   - 기절한 적(또는 굳은 시민) 위(`y = 5.0`)에 올라탄 적은 바닥의 플레이어(`y = 3.0`)와의 수직 거리가 `Mathf.Abs(d.y) = 2.0 > attackRangeY (1.5)`가 된다.
   - `TestEnemy.FixedUpdate()`는 수평 거리만 확인(`(player.x - transform.x) * facing > attackRangeX`)하므로, 플레이어가 `attackRangeX`(근접 `1.8u`, 2웨이브 원거리 벌레 `9.0u`) 안에 있으면 `vx = 0`으로 멈춰 아래로 내려오지 않는다. 특히 원거리 벌레는 `IsBackingOff()`에서도 `Mathf.Abs(d.y) > attackRangeY`일 때 `false`를 반환하므로 후퇴도 하지 않고 `y = 5`에 완전히 고정된다.
   - 동시에 `TestEnemy.UpdateState()`의 `State.Approach`는 `Mathf.Abs(d.y) <= attackRangeY (1.5f)`를 요구하므로 영원히 `State.Windup`으로 진입하지 못하고 `State.Approach`(`strikesLeft > 0`)에 갇힌다.
   - `TestEnemy.ReceivePlayerHit()`는 `strikesLeft > 0 && state != State.Turn && state != State.Frozen`일 때 모든 플레이어 공격을 튕겨내므로(`return false`), 해당 적은 영원히 공격도 하지 않고 일반 공격으로 처치할 수도 없어 `AllStunned()`가 달성되지 않는 소프트락이 발생한다.

## 코드 근거 (file:line)
- `Assets/Editor/Stg05Builder.cs:91-92` — 천장 소환 지점 `Spawn-L (2, 14)`, `Spawn-R (28, 14)` 고정 배치
- `Assets/Editor/Stg05Builder.cs:161` / `ProjectSettings/Physics2DSettings.asset:56` — `Physics2D.IgnoreLayerCollision(PlayerLayer, EnemyLayer, true)`만 설정되어 `Enemy(8) × Enemy(8)` 충돌 활성 상태
- `Assets/Prefabs/Prototype/TestEnemy.prefab:197, 249-293` — Layer 8(`Enemy`), 비트리거 `BoxCollider2D` 크기 `(0.9, 2.0)`
- `Assets/Scripts/Boss/BossFight.cs:34, 98, 108-123` — `StartWave()`에서 이전 웨이브 기절 적 미제거(`alive.Clear()`만 수행), 3웨이브 시민 1·3이 동일하게 `Spawn-L`에 배정됨
- `Assets/Scripts/Enemy/TestEnemy.cs:16, 125-144, 190, 212, 372-376, 391`:
  - `:16` `attackRangeY = 1.5f`
  - `:132-143` `State.Stunned`/`State.Frozen` 및 `attackRangeX` 이내일 때 `vx = 0f` 강제
  - `:190` `IsBackingOff()`에서 `Mathf.Abs(d.y) > attackRangeY`이면 후퇴 안 함
  - `:212` `Mathf.Abs(d.y) <= attackRangeY` 미충족 시 `State.Windup` 진입 불가
  - `:372-376` `State.Stunned`에서 콜라이더를 끄지 않고 자식 `visual`만 90° 회전
  - `:391` `strikesLeft > 0`인 `State.Approach` 상태에서 플레이어 공격 무효화(`return false`)
