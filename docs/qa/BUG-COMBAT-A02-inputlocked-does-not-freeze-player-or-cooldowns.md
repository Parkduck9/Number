검증: CONFIRMED (PlayerController/PlayerHealth/Shockwave 어디에도 GameFlow.InputLocked 확인 없음; 심각도는 '높음'보다 낮은 '중간' 권장: 대시 0.15초·쿨다운 소진 정도, 이동은 입력 차단됨)

# BUG-COMBAT-A02: 블록 대화(`GameFlow.InputLocked`) 중에도 플레이어 물리·상태 타이머·충격파·대시 쿨다운이 멈추지 않고 진행됨

## 심각도
**높음**

## 위반 규칙
- **Core/GameFlow.cs:3**: `// Global gameplay lock. Set by blocking dialogue (talk mode). Respected by PlayerInputReader and TestEnemy.`
- **PauseMenu.cs:7**: `// Pausing = Time.timeScale 0 + GameFlow.InputLocked (freezes input, enemy AI, projectiles).`
- **ARCHITECTURE_PATTERNS.md 1.3절**: "타이머 규칙 (일시정지·히트스톱 호환): 게임플레이 타이머는 `Time.time`... `InputLocked` 동안은 `deltaTime`만큼 밀어서 멈춘 것처럼 만든다 (`TestEnemy.cs:110-119`, `EnemyProjectile.cs:41`)."

## 코드 근거
- `Assets/Scripts/Player/PlayerInputReader.cs:38-43`: `GameFlow.InputLocked`일 때 입력 값만 0/`false`로 비움.
- `Assets/Scripts/Player/PlayerController.cs:101-152`, `220-231`: `GameFlow.InputLocked`를 전혀 확인하지 않음.
  - 대시(`State.Dash`) 상태에서 대화/전환 트리거(`DialogueTrigger`, `LudditeMeeting`, `LevelTransition`)에 진입하면 `InputLocked`가 켜지더라도 `FixedUpdate`에서 `rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f)`로 계속 이동하며, `Update`에서 `elapsed >= dashDuration`으로 상태가 끝나고 잔상이 생성됨.
  - 점프/낙하/피격(`Hurt`) 중 대화에 진입하면 플레이어가 대화 중 그대로 중력을 받아 떨어지거나 넉백 속도로 미끄러짐.
  - 공격 버퍼(`attackBufferedUntil`), 점프 버퍼(`jumpBufferedUntil`), 코요테 타임(`lastGroundedAt`), 대시 쿨다운(`dashCooldownUntil`), 피격 무적 시간(`PlayerHealth.invulnerableUntil`, `PlayerHealth.cs:14`)이 `Time.deltaTime`만큼 밀리지 않아 대화 중에 모두 만료됨.
- `Assets/Scripts/Player/Shockwave.cs:33, 47-60`:
  - `if (Time.timeScale == 0f) return;`만 확인하고 `GameFlow.InputLocked`를 확인하지 않으며, `readyAt` 타이머를 `Time.deltaTime`만큼 밀어주지 않아 대화 중에 충격파 쿨다운 5초가 그대로 흘러감. (또한 에디터/개발 빌드에서 대화 중 F9 디버그 토글도 작동함.)

## 재현 단계
1. 대시(Shift) 또는 충격파(Space)를 사용하여 쿨다운이 남은 상태에서 즉시 `LudditeMeeting`(STG-03) 또는 `DialogueTrigger`(Talk 모드)에 진입하거나, 대시/점프 중 트리거에 닿는다.
2. 대화가 진행되는 동안(`GameFlow.InputLocked == true`, `Time.timeScale == 1`) 플레이어의 움직임과 대시/충격파 쿨다운을 확인한다.

## 기대 결과
`TestEnemy` 및 `EnemyProjectile`과 동일하게, 블록 대화나 스테이지 전환(`GameFlow.InputLocked == true`) 중에는 플레이어의 물리 이동과 상태 진행이 멈추고, 대시 쿨다운(`dashCooldownUntil`), 무적 타이머(`invulnerableUntil`), 충격파 쿨다운(`readyAt`) 등 게임플레이 타이머가 `Time.deltaTime`만큼 연장되어 동결되어야 한다.

## 실제 결과
입력만 차단될 뿐 `PlayerController`, `PlayerHealth`, `Shockwave`는 `GameFlow.InputLocked`를 무시하고 계속 돌아간다. 대시/점프/넉백 중 대화 트리거에 들어가면 대화창이 뜬 상태에서 캐릭터가 계속 이동·낙하하며, 대시 쿨다운(2초)·피격 무적(0.8초)·충격파 쿨다운(5초)이 대화를 읽는 동안 전부 소진된다.
