# 공격 자동 접근 · 대시 뒤잡기 구현 계획

> 2026-11-08 Fable 설계. 근거: [DECISIONS.md](../DECISIONS.md) 11, [GDD.md](../GDD.md) 6장 (대시·공격 자동 접근 `[확정]`).
> **2026-11-08 밤 구현 완료 (Fable).** 아래 설계대로 `PlayerController.cs`에 넣었고, ① 공중 접근 = 끔, ② 뒤잡기 뒤 돌아서기 = 켬(`dashBehindFaceBack`)으로 정했다. 추가로 리뷰 06 요구에 따라 `TestEnemy.DoStrike`에 **"대시가 판정 안에서 시작했으면 타격 순간 밖에 있어도 dashedThrough"** 분기를 넣었다 (`PlayerController.DashStart` 사용) — 이게 없으면 뒤잡기가 마지막 일격 경직·안드로이드 7번째 과부하를 못 낸다. 모든 수치는 (T) 시작값. Play 모드 확인은 아직 안 했다 (6절).
> `PlayerInputReader.cs`·`EnemyProjectile.cs`는 바꾸지 않았다.

## 0. 이미 있어서 그대로 쓰는 것
- **Player×Enemy 레이어 충돌은 모든 빌더에서 꺼져 있다** (`IgnoreLayerCollision(7, 8)`). 대시로 적을 통과하는 데 레이어 전환·충돌 무시 코드가 **필요 없다**.
- 플레이어 `Rigidbody2D`는 Continuous + Interpolate (PrototypeSceneBuilder:131). 빠른 미끄러짐도 벽을 뚫지 않는다 → 벽은 **물리에 맡기고 사전 캐스트를 안 한다**. 낭떠러지만 레이캐스트로 미리 본다.
- `TryDash`는 이미 "누른 방향이 있으면 그쪽, 없으면 바라보는 방향"으로 `Facing`을 정한다. 대시 뒤잡기도 **이 Facing 기준**으로 적을 찾는다 (추천: 유지). 적에게서 먼 쪽을 누르고 대시하면 뒤잡기 없이 정거리 대시 → 기존 돌격(Charge) 규칙이 그대로 나온다.
- 대시 중 무적(`ReceiveEnemyHit` 1순위), 2초 쿨다운, 히트스톱 중 `pendingDash`, 공격 회복 중 대시 취소 — 전부 `TryDash`/`EnterState(Dash)` 경로라 바뀌지 않는다.

## 1. 공통: 대상 찾기
`PlayerController.cs`에 추가:
```csharp
// Facing 쪽 maxDx 안, 높이차 maxDy 이내, 쓰러지지 않은 가장 가까운 적. 없으면 null.
TestEnemy FindTargetAhead(float maxDx, float maxDy, out Collider2D col)
```
- `Physics2D.OverlapBoxAll(center = pos + (Facing * maxDx/2, 0), size = (maxDx + 1, maxDy*2 + 2), 0, enemyMask)` 로 후보를 넓게 받고, `transform.position` 기준으로 걸러낸다: `dx = (enemy.x - player.x) * Facing`, 조건 `dx > 0 && dx <= maxDx && |dy| <= maxDy`.
- 제외: `enemy.Current == TestEnemy.State.Stunned`(쓰러짐·사라지는 중), `!gameObject.activeInHierarchy`. Frozen(숫자 0 시민)·Open·Overload 등은 **포함** (때릴 수 있는 적).
- 가장 작은 `dx` 하나. `col`은 뒤잡기 자리 계산에 쓴다 (`col.bounds.extents.x` = 적 반폭 0.45).
- 할당은 `OverlapBoxAll` 한 번 (`DoAttackHit`도 같은 방식이라 허용).

## 2. A) 공격 자동 접근 — **DECISIONS 12로 대체 (2026-11-08 밤)**
> 이 절은 더 이상 쓰지 않는다. 좌클릭은 마우스 조준 관통 돌진 + 패링으로 바뀌었고, 자동 접근 코드(`approach*`, `sliding`, `TryStartApproach`)는 지웠다. 근거: [DECISIONS.md](../DECISIONS.md) 12. 아래는 기록용이다. 6절 1~8번 확인 항목과 23번 힌트 문구도 옛 내용이다 (힌트는 이제 "LMB attack · parry").

### 동작
좌클릭 → `EnterState(Attack)` 시점에 대상을 찾고, 있으면 **공격 판정이 나가는 `attackHitDelay`(0.08초) 동안** 적 앞 `approachStopDist`까지 미끄러진다. 판정은 기존대로 0.08초에 한 번 나간다. 대상이 없거나 조건이 안 맞으면 지금과 같은 제자리 공격.

### 추가 필드 (Header "Attack approach (T)")
| 이름 | 시작값 | 뜻 |
|---|---|---|
| `approachRange` | 3.0 | 수평 탐색 거리 (u) |
| `approachMaxDy` | 1.0 | 높이차 허용 |
| `approachStopDist` | 1.4 | 멈추는 중심 간 거리. 공격 박스가 1.7까지 닿으니 0.3u 여유 (뒤로 물러나는 원거리 적 대비) |
| `approachMaxSpeed` | 40 | 미끄러짐 속도 상한 (3u / 0.08s = 37.5) |
| `ledgeProbeDepth` | 1.5 | 목적지 발밑 땅 확인 깊이 (TestEnemy.EdgeAhead와 같은 값) |

상태 변수: `bool sliding; float slideTargetX; float slideSpeed;`

### 함수
- `void TryStartApproach()` — `EnterState(Attack)`의 `case State.Attack`에서 호출.
  1. `!grounded` → 접근 안 함 (공중 공격은 제자리. 낭떠러지 규칙을 공짜로 지키고 공중 미끄러짐의 어색함을 피한다. **결정 필요 ①**: 공중에서도 원하면 이 줄만 지운다).
  2. `FindTargetAhead(approachRange, approachMaxDy, out col)`; null → 안 함.
  3. `slideTargetX = enemy.x - Facing * approachStopDist`. `dist = (slideTargetX - pos.x) * Facing`; `dist <= 0.05` → 이미 닿는 거리, 안 함.
  4. **낭떠러지**: `Physics2D.Raycast((slideTargetX, feetY), down, ledgeProbeDepth, groundMask)` 가 없으면 안 함 (`feetY = pos.y + feetOffset.y`). 가운데 틈은 0.08초 안에 지나가므로 (낙하 ≈ 0.03u) 따로 안 본다.
  5. `slideSpeed = Mathf.Min(dist / attackHitDelay, approachMaxSpeed)`, `sliding = true`.
- `FixedUpdate`에 `case State.Attack:` 추가:
  ```csharp
  if (sliding) {
      float remain = (slideTargetX - rb.position.x) * Facing;
      if (remain <= 0.02f) { sliding = false; rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); }
      else rb.linearVelocity = new Vector2(Facing * Mathf.Min(slideSpeed, remain / Time.fixedDeltaTime), rb.linearVelocity.y);
  }
  ```
  마지막 스텝은 `remain / dt`로 속도를 줄여 **정확히 목표에서 멈춘다** (overshoot 없음). 벽에 막히면 물리가 멈추고 다음 스텝에도 `remain > 0`이지만 0.08초 뒤 `DoAttackHit`에서 끝난다.
- `DoAttackHit()` 첫 줄에 `if (sliding) { sliding = false; if (grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); }` — 판정 이후엔 절대 더 안 미끄러진다.
- `EnterState()` 맨 앞에 `sliding = false;` — 대시 취소·피격(Hurt)·사망으로 공격이 끊기면 즉시 멈춤. 무적 없음(Hurt 전이가 평소대로 들어옴).
- `UpdateVisual`: 미끄러지는 동안 `walking` 애니메이션이 돌지 않도록 조건에 `state == Locomotion`이 이미 있다 → 추가 변경 없음.

### 엣지 케이스
- 대상이 미끄러지는 사이 움직여도 목표 x는 시작 시 고정. 접근 적(3.75u/s)이면 0.3u 더 가까워져 겹칠 수 있다 — 충돌이 꺼져 있어 문제 없고 판정 박스(0.1~1.7u)에 보통 든다. **기존 한계**: 적 중심이 0.1u 안으로 들어오면 공격 박스 밖 — 지금도 같다, 이번엔 안 고친다.
- 히트스톱으로 `Time.timeScale = 0`이면 물리도 멈추므로 안전.
- `GameFlow.InputLocked` 중에는 `stateEnteredAt`을 밀어 주니 미끄러짐도 그만큼 연장된다. FixedUpdate는 Locked 중 Attack에서 velocity를 쓰지 않도록 `if (GameFlow.InputLocked) return;`을 FixedUpdate 첫 줄에 둔다 (현재 Locomotion도 Locked 중 MoveX=0으로 쓰고 있어 동작은 같다).
- 안드로이드·연타 중 적도 접근 대상이다 (막힘 판정은 `ReceivePlayerHit` 그대로).

## 3. B) Shift 대시: 뒤잡기 / 정거리 — **DECISIONS 13으로 바뀜 (2026-11-08 밤): 회피 + 반격**
> 지금 동작 (`PlayerController.TryPlanDashBehind` / `PlanBehind` / `DoShiftCounter`, 수치는 `DashRules.cs`):
> - **대상 찾기 (보정):** 수평 `ShiftAssistRange` 4u · 높이차 `ShiftAssistDy` 1.5u 안의 가장 가까운 적 (Stunned·Frozen·비활성 제외). **누른 방향 → (안 누르면) 바라보는 쪽 → 반대쪽** 순서. 한쪽 대상의 뒤 자리가 막히면 다른 쪽을 본다.
> - **대상 있음:** 무적 대시로 적 뒤 자리(적 반폭 + 0.45 + 0.3)까지. 사이에 벽(수평 BoxCast, 일방 발판 무시)·자리가 벽 안·땅에서 시작했는데 발밑이 허공이면 그 대상은 못 씀. 발 높이 차가 `ShiftStepDy` 0.5u 이하면 적의 땅 높이로 살짝 올라가거나 내려가고, 그보다 크면 수평 그대로. 대시 시간 = 거리/24 (0.08~0.25초), 끝점에 정확히 닿게 속도를 맞춘다.
> - **대시 끝:** 적 쪽으로 돌아서 바로 **반격 1타** (`HitEnemy` — 좌클릭과 같은 경로: 일반 적 한 방, 히트스톱·흔들림·Hit 소리. 안드로이드는 과부하가 아니면 흰 번쩍 + 패링 소리 작게 = 막힘). 같은 순간 공격 상자 안의 다른 적도 맞는다. Swing 소리 + 베기 이펙트.
> - **회피 판정 그대로:** 적의 Strike는 대시 중에 처리되므로(`TestEnemy.DoStrike`의 `DashStart` 분기 그대로) 마지막 일격 경직·안드로이드 7번째 과부하가 먼저 성립하고, 그 뒤 대시 끝 반격이 과부하된 안드로이드를 쓰러뜨릴 수 있다.
> - **대상 없음:** 정거리 대시(0.15초, 3.6u) 후 **뒤돌아본다** (`Facing` 반전, 이동키를 누르고 있으면 다음 프레임에 그쪽으로).
> - **쿨다운 `ShiftCooldown` 3.5초** (전 2초, 프리팹 `dashCooldown` 필드는 지움). 탄 피하기 무적 = 대시 상태, 그대로. 반격 뒤 상태는 Locomotion이라 대시 중 누른 좌클릭 버퍼는 대시가 끝나자마자 나간다.
> - 지운 필드: `dashCooldown`, `dashBehindRange`, `dashBehindMaxDy`, `dashBehindFaceBack`. 아래는 DECISIONS 11 때의 기록이다 (6절 9·10·16·18번도 옛 내용).

### 동작 (DECISIONS 11, 기록)
`TryDash` → `EnterState(Dash)` 때 `FindTargetAhead(dashBehindRange, dashBehindMaxDy, …)`. 적이 있고 **뒤 자리가 비어 있으면** 그 자리까지 대시(속도 24 유지, **시간이 거리에 맞게 달라짐**). 아니면 지금처럼 `dashDuration` 0.15초 정거리(3.6u). 무적·잔상·쿨다운은 같다.

### 추가 필드 (Header "Dash behind (T)")
| 이름 | 시작값 | 뜻 |
|---|---|---|
| `dashBehindRange` | 2.0 | 뒤잡기 발동 수평 거리 = 1.5 + 여유 0.5 (적 공격 사거리 1.8을 덮음) |
| `dashBehindMaxDy` | 1.0 | 높이차 허용 |
| `dashBehindClearance` | 0.3 | 적 뒤 자리 = 적 반폭 + 플레이어 반폭(0.45) + 이 값 → 중심 간 1.2u |
| `dashMinDuration` | 0.08 | 뒤잡기 최소 시간 (Strike 프레임을 Dash 상태로 지나갈 여유) |

상태 변수: `float dashEndsAfter;` (이번 대시의 길이. 정거리면 `dashDuration`). **결정 필요 ②** `bool dashBehindFaceBack` (아래).

### 함수
- `bool TryPlanDashBehind(out float distance)` — `EnterState(Dash)`에서 호출.
  1. `FindTargetAhead(dashBehindRange, dashBehindMaxDy, out col)`; null → false.
  2. `targetX = enemy.x + Facing * (col.bounds.extents.x + 0.45f + dashBehindClearance)`; `distance = (targetX - pos.x) * Facing` (항상 > 0).
  3. **벽**: `Physics2D.OverlapBox((targetX, pos.y), 플레이어 박스 × 0.9 = (0.8, 1.8), 0, groundMask) != null` → false. (사이에 벽이 있으면 물리가 멈춰 세워 정면에 남는다 — 그건 적이 벽에 붙은 경우뿐이고, 그때 뒤 자리는 벽 안이라 이 검사가 잡는다.)
  4. **낭떠러지**: `grounded`일 때만, 목적지 발밑 `Raycast(down, ledgeProbeDepth, groundMask)` 없으면 false. 공중 대시는 검사 안 함 (정거리 대시도 공중에서 떨어질 자리를 안 본다, 같은 규칙).
  5. true.
- `EnterState(Dash)`:
  ```csharp
  dashEndsAfter = TryPlanDashBehind(out float d) ? Mathf.Clamp(d / dashSpeed, dashMinDuration, 0.25f) : dashDuration;
  ```
  뒤잡기면 `dashBehindActive = true` 저장 (끝날 때 방향 결정용).
- `Update` `case State.Dash`: `elapsed >= dashDuration` → `elapsed >= dashEndsAfter`.
- **대시 끝 방향 (결정 필요 ②, 추천 켬)**: 뒤잡기로 끝날 때 `Facing = -Facing` 한 줄. 지금은 등 뒤로 넘어가도 적 반대쪽을 보고 있어서, 등을 때리려면 방향키를 반대로 눌러야 한다. 켜면 쿨다운 중 버퍼된 공격이 바로 등을 때린다. 이동키를 계속 누르고 있으면 다음 Locomotion 프레임에 `MoveX`가 Facing을 되돌리므로 조작을 방해하지 않는다. GDD 6장 "피한 직후 바로 공격 가능"과 맞는다. 끄려면 한 줄 삭제.

### 기존 규칙과의 결합 (확인만, 코드 변경 없음)
- **뒤잡기 → 적 Turn 0.35초**: `TestEnemy.DoStrike`가 Strike 순간 `playerBehind`를 보고 Turn으로 간다. Windup 중 뒤잡기면 그 Strike에서, Recover 중이면 다음 Strike(헛스윙)에서 돈다. 플레이어 대시는 쿨다운이라 다음 공격은 패링으로만 — 설계 의도 유지.
- **마지막 일격 대시 통과 경직 · 안드로이드 7번째**: 조건은 "Strike 프레임에 플레이어가 적 공격 박스(0.1~1.9u) 안 + `State.Dash`". 뒤잡기는 ≤2.0u 앞에서 시작해 박스를 전부 지나므로 **지금보다 성공하기 쉽다**. 끝 자리(뒤 1.2u)는 박스 밖이라 Strike가 늦게 와도 맞지 않는다(무적이기도 함).
- **탄 피하기**: `EnemyProjectile`은 `State.Dash`면 Dodged → 통과. 뒤잡기도 정거리도 같다. 짧은 뒤잡기(0.08~0.13초)는 무적 시간이 조금 짧다 — 의도된 교환.
- **대시로 멀어지기 → Charge**: 반대쪽을 누르고 대시하면 Facing이 그쪽이라 뒤잡기 대상이 없다 → 정거리 → 기존 Charge.

### 엣지 케이스
- 적 두 명이 앞뒤로 있을 때(협공): 앞쪽 적 뒤 자리에 뒤쪽 적이 있어도 Player×Enemy 충돌이 꺼져 있어 겹친 채 끝난다. 뒤쪽 적의 공격 박스 안에 서게 될 수 있다 — 플레이어 선택의 결과라 그대로 둔다.
- 적이 뒤잡기 중 Charge로 반대로 달리면 서로 지나친다 — 목표 x는 시작 시 고정, 결과는 "적 뒤"가 아닐 수 있지만 무적 통과라 손해 없음.
- InputLocked가 대시 중 켜지면 기존 코드가 Dash를 끊는다 (변경 없음).

## 4. 검수 지적 같이 고치기 (같은 파일, 2단계에서)
1. **히트스톱 중 공격 입력 유실 (중간)** — `PlayerController.cs` 156·191줄의 `attackBufferedUntil = now + attackBuffer`를 **상태가 끝나는 시각 기준**으로: 헬퍼 `float StateEndsAt()` = Attack→`stateEnteredAt + attackDuration`, Parry→`+ parryDuration`, Dash→`+ dashEndsAfter`, 그 외 `Time.time`. 두 곳 모두 `attackBufferedUntil = Mathf.Max(Time.time, StateEndsAt()) + attackBuffer`. 자동 접근과의 결합: 버퍼된 공격은 Locomotion 복귀 프레임에 `EnterState(Attack)` → `TryStartApproach`가 **그 시점의 적 위치**로 다시 계산한다. 뒤잡기 뒤 버퍼 공격도 같은 경로(②를 켜면 등을 때림).
2. **패링 가드가 진행 중 공격을 끊음 (낮~중)** — `ReceiveEnemyHit` 패링 성공 분기의 `EnterState(State.Locomotion)` → `if (state == State.Parry) EnterState(State.Locomotion);`. 미끄러짐 중 가드 패링이 나도 공격이 이어진다.
3. **Shockwave 쿨다운이 대화 중에도 흐름 (낮)** — `Shockwave.cs` `Update`: `if (GameFlow.InputLocked) { pendingPress = false; readyAt += Time.deltaTime; UpdateHud(); return; }`.
4. **DialoguePlayer 언어 전환 시 현재 줄 갱신 안 됨 (낮)** — `OnEnable/OnDisable`에서 `Loc.Changed` 구독, 핸들러는 `radioLine`/`talkLine`이 있으면 `maxVisibleCharacters`를 저장 → `Show(box, line)` → 복원.
5. **STG-01 첫 적 조작 힌트 없음 (P1-6)** — `Stg01Builder.Build`에 `StageBuildKit.BuildDialoguePlayer()` 추가(지금 STG-01엔 DialoguePlayer가 없다), E-A1이 깨어나는 지점(SEG-A x 8~9, y 2.25~5) 앞에 `BuildTrigger` + `DialogueTrigger`(`lines` = 새 파일). 파일 `Assets/Data/Dialogue/stg01_hint_combat.txt`:
   ```
   @mode radio
   yudo | 좌클릭 공격 · 패링 | LMB attack · parry | 4
   ```
   (화자는 `yudo`; 표지판 그림 없이 라디오 줄만 — 새 에셋 0. 문구는 사용자가 바꿀 수 있음.)

## 5. 작업 순서 (Sonnet)
1. `FindTargetAhead` + A(접근) → 확인: 컴파일 0 오류, 프로토타입 씬에서 3u 앞 적에게 좌클릭 → 붙어서 때림.
2. B(뒤잡기) + `dashEndsAfter` → 확인: 1.5u 앞 적에게 Shift → 적 뒤 1.2u에 멈춤.
3. 4절 1·2 (같은 파일) → 3·4·5 (다른 파일).
4. Opus 검수 → `[code]` 커밋. Play 모드 확인은 사용자(에디터 점유 금지).

## 6. Play 모드 확인 목록
A 접근
1. 적 2.5u 앞에서 좌클릭 → 미끄러져 1.4u에서 멈추고 바로 맞는다 (빨강 플래시/쓰러짐). 공격 뒤 더 밀리지 않는다.
2. 적 3.5u 앞 → 제자리 헛스윙. 적이 뒤에 있을 때 → 제자리.
3. 적과 나 사이에 벽(또는 적이 발판 위 dy > 1) → 접근 안 함 / 벽에서 멈춤, 뚫지 않음.
4. 적이 낭떠러지 건너(틈 1u 이상, 목적지가 허공) → 제자리 공격, 떨어지지 않음.
5. 공중에서 좌클릭 → 제자리 공중 공격 (①이 "끄기"일 때).
6. 접근 중 적 Strike에 맞으면 평소처럼 피격(무적 없음), 미끄러짐 즉시 중단.
7. 접근 중 Shift → 대시로 취소되고 미끄러짐 멈춤.
8. 숫자 연타 중 안드로이드에게 접근 → 붙되 흰색 막힘.

B 뒤잡기
9. 적 1.5u 앞, Shift → 적 뒤 약 1.2u에 멈춤. 잔상·흰색 유지. 2초 쿨다운 그대로 (Debug 표시).
10. 적 3u 앞 → 정거리 3.6u 대시 (바뀌지 않음).
11. 적이 벽을 등지고 있을 때 → 정거리 대시(벽에서 멈춤).
12. 적이 낭떠러지 끝에 서 있을 때 (뒤가 허공) → 정거리 대시.
13. Windup 중 뒤잡기 → 적이 헛치고 0.35초 돈다 → 그 사이 등 때리면 피해.
14. 마지막 일격(빨간 불꽃) 순간 뒤잡기 → 적 1초 경직. 숫자 7 안드로이드 7번째 → 과부하.
15. 황금 탄이 날아올 때 뒤잡기/정거리 모두 → 탄 통과(HP 유지).
16. 반대 방향키를 누르고 Shift → 적에게서 멀어지는 정거리 대시 → 적 Charge.
17. 공격 회복 중 Shift → 즉시 대시 (취소), 뒤잡기도 적용.
18. ② 켬: 뒤잡기 뒤 키를 떼고 있으면 적을 바라봄 → 좌클릭이 등을 때림.

검수 수정
19. 적을 쓰러뜨린 히트스톱(0.08초) 중 좌클릭 → 공격 끝나자마자 다음 공격이 나간다.
20. 패링 가드 0.2초 안에 공격을 시작하고 두 번째 타를 가드로 막아도 공격 애니메이션이 끊기지 않는다.
21. 대화(InputLocked) 중 Shockwave HUD 쿨다운이 멈춘다.
22. 라디오 줄 표시 중 L(개발 빌드) → 같은 줄이 다른 언어로 바뀐다.
23. STG-01 시작 후 오른쪽으로 가면 첫 원거리 적이 깨기 전 "LMB attack · parry" 라디오가 한 번 뜬다.

## 7. 위험
- 미끄러짐 속도 37u/s는 Continuous 모드라 벽을 뚫지 않지만, **one-way 발판의 옆면**은 PlatformEffector2D가 통과시켜 발판 위로 올라탈 수 있다 — 목적지 발밑 검사만 있으므로 가끔 발판 모서리에 걸릴 수 있다. 플레이에서 보이면 `approachMaxSpeed`를 낮추거나 목적지를 `BoxCast`로 한 번 더 자른다.
- 뒤잡기 시간이 거리별로 달라 무적 시간이 0.08~0.13초로 짧아진다. 탄 피하기가 어렵다고 느끼면 `dashMinDuration`을 0.12로.
- `approachStopDist` 1.4는 적 사거리(1.8) 안이다 — 접근 = 적 공격을 받을 거리로 스스로 들어가는 것. 의도한 교환(먼저 때리기 vs 안전)이며, 한 방 처치 규칙 덕에 유리하다.

## 8. 벽 매달림 수정 (2026-11-09)
- 플레이어 콜라이더에 마찰 0 PhysicsMaterial2D를 Awake에서 런타임 생성 (이미 머티리얼이 있으면 그대로 둠).
- Locomotion FixedUpdate: 공중이고 이동 방향 0.06u 앞에 일반(비 one-way) 지면 벽이 있으면 vx=0 -> 중력으로 미끄러짐. 모서리 보정은 생략.
